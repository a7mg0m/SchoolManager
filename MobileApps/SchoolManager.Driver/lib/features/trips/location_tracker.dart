import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:geolocator/geolocator.dart';

import '../../core/i18n/app_language.dart';
import 'trip_service.dart';

/// Sends the bus's position automatically while a trip is on the road, so the school and the parents can follow
/// the bus: on every screen of the app and in the background (Android: a foreground service with a
/// "sharing the bus's location" notification; iOS: background location). Positions come from the phone's GPS;
/// one is sent at most every [minInterval], and the last one is sent again every [heartbeat] when the bus stands still.
class LocationTracker extends ChangeNotifier with WidgetsBindingObserver {
  LocationTracker({
    Stream<(double, double)> Function()? positions,
    Future<void> Function()? ensurePermission,
    Future<void> Function(String tripId, double latitude, double longitude)? send,
    this.minInterval = const Duration(seconds: 15),
    // null: no resend
    this.heartbeat = const Duration(seconds: 60),
  })  : _positions = positions ?? _gpsPositions,
        _ensurePermission = ensurePermission ?? _askPermission,
        _send = send ?? TripService.instance.updateLocation;

  static final instance = LocationTracker();

  final Stream<(double, double)> Function() _positions;
  final Future<void> Function() _ensurePermission;
  final Future<void> Function(String tripId, double latitude, double longitude) _send;
  final Duration minInterval;
  final Duration? heartbeat;

  StreamSubscription<(double, double)>? _subscription;
  Timer? _heartbeatTimer;
  bool _observing = false;

  /// The trip whose position is being shared (null when none)
  String? tripId;

  /// Last position of the phone
  (double, double)? last;

  /// When the last position reached the server
  DateTime? lastSentAt;

  /// Why sharing does not work right now (permission, GPS off, server), or null
  String? error;

  bool _sending = false;

  bool get isTracking => _subscription != null;

  /// Starts sharing the position of a running trip (nothing happens when it is already shared)
  Future<void> start(String id) async {
    if (tripId == id && (isTracking || error != null)) return;
    stop(notify: false);
    tripId = id;
    error = null;
    notifyListeners();
    try {
      await _ensurePermission();
    } catch (e) {
      error = '$e';
      notifyListeners();
      return;
    }
    if (tripId != id) return; // stopped meanwhile
    _watchAppLifecycle();
    // Standing still (no new GPS position): the last one is sent again so "last updated" stays fresh
    final every = heartbeat;
    if (every != null) {
      _heartbeatTimer = Timer.periodic(every, (_) {
        final p = last;
        if (p != null && tripId == id) _onPosition(id, p);
      });
    }
    _subscription = _positions().listen(
      (p) => _onPosition(id, p),
      onError: (Object e) {
        error = tr('تعذر تحديد موقع الحافلة. تأكد من تشغيل خدمة الموقع.',
            "Could not get the bus's position. Make sure location is on.");
        notifyListeners();
      },
    );
    notifyListeners();
  }

  /// Shares the position of the running trip among [trips] (one runs at a time), or stops when none runs
  Future<void> follow(Iterable<Trip> trips) async {
    final running = trips.where((t) => t.status == TripStatus.inProgress);
    if (running.isEmpty) {
      if (tripId != null) stop();
      return;
    }
    await start(running.first.id);
  }

  /// Retries after a permission / GPS problem
  Future<void> retry() async {
    final id = tripId;
    if (id == null) return;
    stop(notify: false);
    await start(id);
  }

  void stop({bool notify = true}) {
    _subscription?.cancel();
    _subscription = null;
    _heartbeatTimer?.cancel();
    _heartbeatTimer = null;
    tripId = null;
    last = null;
    lastSentAt = null;
    error = null;
    if (notify) notifyListeners();
  }

  Future<void> _onPosition(String id, (double, double) position) async {
    last = position;
    final now = DateTime.now();
    final due = lastSentAt == null || now.difference(lastSentAt!) >= minInterval;
    if (!due || _sending) {
      notifyListeners();
      return;
    }
    _sending = true;
    try {
      await _send(id, position.$1, position.$2);
      if (tripId == id) {
        lastSentAt = now;
        error = null;
      }
    } on TripNotRunningException {
      // Finished or cancelled elsewhere: nothing more to share
      if (tripId == id) {
        stop();
        return;
      }
    } catch (e) {
      if (tripId == id) error = '$e';
    } finally {
      _sending = false;
      notifyListeners();
    }
  }

  /// Back in the app after a problem (GPS off, permission refused, stream ended): try again
  void _watchAppLifecycle() {
    if (_observing) return;
    final binding = WidgetsBinding.instance;
    binding.addObserver(this);
    _observing = true;
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && tripId != null && (error != null || !isTracking)) {
      retry();
    }
  }

  static Stream<(double, double)> _gpsPositions() =>
      Geolocator.getPositionStream(locationSettings: _settings()).map((p) => (p.latitude, p.longitude));

  /// Keeps the GPS on when the app is in the background or the screen is off
  static LocationSettings _settings() {
    if (kIsWeb) return const LocationSettings(accuracy: LocationAccuracy.high, distanceFilter: 10);
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return AndroidSettings(
          accuracy: LocationAccuracy.high,
          distanceFilter: 10,
          intervalDuration: const Duration(seconds: 10),
          // A foreground service: Android keeps sending positions while the app is in the background
          foregroundNotificationConfig: ForegroundNotificationConfig(
            notificationTitle: tr('رحلة الحافلة جارية', 'Bus trip on the road'),
            notificationText: tr('يتم مشاركة موقع الحافلة مع المدرسة وأولياء الأمور.',
                "The bus's position is shared with the school and the parents."),
            notificationChannelName: tr('موقع الحافلة', 'Bus location'),
            enableWakeLock: true,
            setOngoing: true,
          ),
        );
      case TargetPlatform.iOS:
      case TargetPlatform.macOS:
        return AppleSettings(
          accuracy: LocationAccuracy.high,
          distanceFilter: 10,
          activityType: ActivityType.automotiveNavigation,
          pauseLocationUpdatesAutomatically: false,
          allowBackgroundLocationUpdates: true,
          showBackgroundLocationIndicator: true,
        );
      default:
        return const LocationSettings(accuracy: LocationAccuracy.high, distanceFilter: 10);
    }
  }

  static Future<void> _askPermission() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw TripException(tr('خدمة الموقع مغلقة في الهاتف. افتحها لمشاركة موقع الحافلة.',
          "Location is turned off on the phone. Turn it on to share the bus's position."));
    }
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
    if (permission == LocationPermission.denied) {
      throw TripException(tr('لم يتم السماح للتطبيق بمعرفة الموقع.', 'The app was not allowed to use the location.'));
    }
    if (permission == LocationPermission.deniedForever) {
      throw TripException(tr('الموقع مرفوض للتطبيق. اسمح به من إعدادات الهاتف.',
          'Location is blocked for the app. Allow it in the phone settings.'));
    }
  }
}
