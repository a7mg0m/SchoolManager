import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../school/school_location_map.dart' show MapLinks;
import 'bus_service.dart';
import '../../core/live/live_refresh.dart';
import '../../core/widgets/paged.dart';

const _amber = Color(0xFFB7791F);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _blue = Color(0xFF1677E8);
const _grey = Color(0xFF8A94A6);

/// Where the map starts when nothing is known yet (Cairo)
const _defaultCenter = LatLng(30.0444, 31.2357);

Color riderColor(RiderStatus s) => switch (s) {
      RiderStatus.waiting => _grey,
      RiderStatus.boarded => _blue,
      RiderStatus.droppedOff => _green,
      RiderStatus.absent => _red,
    };

Color tripStateColor(TripState s) => switch (s) {
      TripState.planned => _grey,
      TripState.inProgress => _blue,
      TripState.completed => _green,
      TripState.cancelled => _red,
    };

List<String> get _dayNames => AppLanguage.instance.isEnglish
    ? const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
    : const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

List<String> get _monthNames => AppLanguage.instance.isEnglish
    ? const ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
    : const ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

String _day(DateTime d) => '${_dayNames[d.weekday % 7]} ${d.day} ${_monthNames[d.month - 1]}';

String _time(DateTime? t) {
  if (t == null) return '-';
  final h12 = t.hour % 12 == 0 ? 12 : t.hour % 12;
  return '$h12:${t.minute.toString().padLeft(2, '0')} ${t.hour < 12 ? tr('ص', 'AM') : tr('م', 'PM')}';
}

/// The API calls of the bus page (tests pass their own)
class BusApi {
  const BusApi({this.loadCurrentTrip, this.loadTrips, this.saveLocation, this.loadPhonePosition});

  final Future<CurrentTrip?> Function(String studentId)? loadCurrentTrip;
  final Future<List<ChildTrip>> Function(String studentId, DateTime from, DateTime to)? loadTrips;
  final Future<void> Function(String studentId, double? latitude, double? longitude)? saveLocation;

  /// Where the phone is now (GPS)
  final Future<(double, double)> Function()? loadPhonePosition;

  Future<(double, double)> phonePosition() => (loadPhonePosition ?? BusService.instance.getPhonePosition)();

  Future<CurrentTrip?> currentTrip(String studentId) => (loadCurrentTrip ?? BusService.instance.getCurrentTrip)(studentId);

  Future<List<ChildTrip>> trips(String studentId, DateTime from, DateTime to) =>
      (loadTrips ?? BusService.instance.getTrips)(studentId, from, to);

  Future<void> save(String studentId, double? latitude, double? longitude) =>
      (saveLocation ?? BusService.instance.saveHomeLocation)(studentId, latitude, longitude);
}

/// The school bus of the child selected on home: follow the running trip on the map, the trips of the last
/// 30 days, and the child's home location (pin on the map, saved for the bus)
class BusScreen extends StatefulWidget {
  const BusScreen({
    super.key,
    required this.child,
    this.api = const BusApi(),
    this.onSessionExpired,
    this.onLocationSaved,
    this.refreshEvery = const Duration(seconds: 15),
    this.showTiles = true,
    this.now,
  });

  final Child child;
  final BusApi api;
  final VoidCallback? onSessionExpired;

  /// The child with the new home location (home keeps it for the next visit)
  final ValueChanged<Child>? onLocationSaved;

  /// How often a running trip is reloaded (null: never, for tests)
  final Duration? refreshEvery;

  /// false skips the map images (widget tests have no network)
  final bool showTiles;
  final DateTime? now;

  @override
  State<BusScreen> createState() => _BusScreenState();
}

class _BusScreenState extends State<BusScreen> with WidgetsBindingObserver, LiveRefreshMixin {
  /// Long lists: the first items, then "Show more"
  final _pages = PageWindows();

  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => Future.wait([_loadTrip(silent: true), _loadTrips()]);

  late Child _child = widget.child;

  CurrentTrip? _trip;
  bool _loadingTrip = true;
  String? _tripError;
  DateTime? _tripUpdatedAt;
  Timer? _timer;

  List<ChildTrip>? _trips;
  String? _tripsError;

  /// Home pin being placed (not saved yet)
  LatLng? _pin;
  bool _saving = false;
  bool _locating = false;
  final _homeMap = MapController();

  DateTime get _today {
    final n = widget.now ?? DateTime.now();
    return DateTime(n.year, n.month, n.day);
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _loadTrip();
    _loadTrips();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _timer?.cancel();
    super.dispose();
  }

  /// Battery: no bus refresh while the app is in the background; a fresh position on return
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused || state == AppLifecycleState.hidden) {
      _timer?.cancel();
      _timer = null;
    } else if (state == AppLifecycleState.resumed && _timer == null && mounted) {
      _loadTrip(silent: true);
    }
  }

  void _expired() => widget.onSessionExpired?.call();

  Future<void> _loadTrip({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _loadingTrip = _trip == null;
        _tripError = null;
      });
    }
    try {
      final trip = await widget.api.currentTrip(_child.id);
      if (!mounted) return;
      setState(() {
        _trip = trip;
        _tripError = null;
        _loadingTrip = false;
        _tripUpdatedAt = DateTime.now();
      });
      _scheduleRefresh();
    } on BusException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        _expired();
        return;
      }
      setState(() {
        if (!silent) _tripError = e.message;
        _loadingTrip = false;
      });
    }
  }

  /// While the child is on a running trip, its bus position is reloaded regularly
  void _scheduleRefresh() {
    _timer?.cancel();
    final every = widget.refreshEvery;
    if (every == null || _trip == null) return;
    _timer = Timer.periodic(every, (_) => _loadTrip(silent: true));
  }

  Future<void> _loadTrips() async {
    setState(() => _tripsError = null);
    try {
      final list = await widget.api.trips(_child.id, _today.subtract(const Duration(days: 30)), _today);
      if (!mounted) return;
      setState(() => _trips = [...list]..sort((a, b) => b.date.compareTo(a.date) != 0 ? b.date.compareTo(a.date) : b.direction.code - a.direction.code));
    } on BusException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        _expired();
        return;
      }
      setState(() {
        _trips = const [];
        _tripsError = e.message;
      });
    }
  }

  Future<void> _call(String phone) async {
    final messenger = ScaffoldMessenger.of(context);
    var opened = false;
    try {
      opened = await launchUrl(Uri(scheme: 'tel', path: phone));
    } catch (_) {}
    if (!opened) {
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(tr('تعذر الاتصال بالرقم.', 'Could not call the number.'))));
    }
  }

  Future<void> _saveLocation({bool clear = false}) async {
    final pin = clear ? null : (_pin ?? (_child.hasLocation ? LatLng(_child.latitude!, _child.longitude!) : null));
    if (!clear && pin == null) {
      _show(tr('اضغط على الخريطة لتحديد موقع المنزل.', 'Tap the map to place the home pin.'));
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.api.save(_child.id, pin?.latitude, pin?.longitude);
      if (!mounted) return;
      setState(() {
        _child = _child.withLocation(pin?.latitude, pin?.longitude);
        _pin = null;
      });
      widget.onLocationSaved?.call(_child);
      _show(clear ? tr('تم حذف موقع المنزل.', 'The home location was removed.') : tr('تم حفظ موقع المنزل.', 'The home location was saved.'));
    } on BusException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        _expired();
        return;
      }
      _show(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  /// The pin goes where the phone is now (the parent at home); saved with "Save location"
  Future<void> _useMyLocation() async {
    setState(() => _locating = true);
    try {
      final (lat, lng) = await widget.api.phonePosition();
      if (!mounted) return;
      final point = LatLng(lat, lng);
      setState(() => _pin = point);
      try {
        _homeMap.move(point, 17);
      } catch (_) {
        // The map is not shown yet
      }
      _show(tr('تم تحديد موقعك الحالي. اضغط "حفظ الموقع" لحفظه.', 'Your current location is set. Tap "Save location" to keep it.'));
    } on BusException catch (e) {
      if (mounted) _show(e.message);
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 3,
      child: Scaffold(
        appBar: AppBar(
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(tr('الباص المدرسي', 'School bus'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              Text([_child.name, _child.classInfo].where((s) => s.isNotEmpty).join(' · '),
                  style: const TextStyle(fontSize: 12, color: Colors.white70)),
            ],
          ),
          bottom: TabBar(
            labelColor: Colors.white,
            unselectedLabelColor: Colors.white70,
            indicatorColor: Colors.white,
            indicatorWeight: 3,
            labelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13.5),
            tabs: [
              Tab(key: const ValueKey('bus-tab-current'), text: tr('الرحلة الحالية', 'Current trip')),
              Tab(key: const ValueKey('bus-tab-trips'), text: tr('الرحلات', 'Trips')),
              Tab(key: const ValueKey('bus-tab-home'), text: tr('موقع المنزل', 'Home location')),
            ],
          ),
        ),
        body: TabBarView(
          physics: const NeverScrollableScrollPhysics(), // the maps take the drag gestures
          children: [_currentTab(), _tripsTab(), _homeTab()],
        ),
      ),
    );
  }

  // ---------------------------------------------------------------------------
  // Current trip
  // ---------------------------------------------------------------------------

  Widget _currentTab() {
    if (_loadingTrip) return const Center(child: CircularProgressIndicator());
    if (_tripError != null) return _message(Icons.error_outline_rounded, _tripError!, retry: _loadTrip);

    final palette = context.palette;
    final trip = _trip;
    return RefreshIndicator(
      onRefresh: _loadTrip,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
        children: [
          if (trip == null) ...[
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(20), border: Border.all(color: palette.line)),
              child: Column(
                children: [
                  Icon(Icons.directions_bus_filled_rounded, size: 52, color: _amber.withValues(alpha: .6)),
                  const SizedBox(height: 10),
                  Text(tr('لا توجد رحلة جارية الآن لـ ${_child.name}.', 'There is no running trip for ${_child.name} right now.'),
                      key: const ValueKey('bus-no-trip'), textAlign: TextAlign.center, style: TextStyle(color: palette.ink, fontWeight: FontWeight.w600)),
                  const SizedBox(height: 4),
                  Text(tr('ستظهر الرحلة هنا عندما يبدأ الباص.', 'The trip shows here when the bus starts.'),
                      textAlign: TextAlign.center, style: TextStyle(color: palette.muted, fontSize: 12.5)),
                  const SizedBox(height: 10),
                  OutlinedButton.icon(onPressed: _loadTrip, icon: const Icon(Icons.refresh_rounded), label: Text(tr('تحديث', 'Refresh'))),
                ],
              ),
            ),
          ] else ...[
            _tripCard(trip),
            const SizedBox(height: 12),
            SizedBox(
              height: 300,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(20),
                child: _trackingMap(trip),
              ),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(Icons.update_rounded, size: 16, color: palette.muted),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    trip.hasPosition
                        ? tr('آخر موقع للباص: ${_time(trip.locationAt)}', 'Bus position at ${_time(trip.locationAt)}')
                        : tr('لم يرسل الباص موقعه بعد.', 'The bus has not sent its position yet.'),
                    key: const ValueKey('bus-position-time'),
                    style: TextStyle(fontSize: 12, color: palette.muted),
                  ),
                ),
                if (trip.hasPosition)
                  TextButton.icon(
                    onPressed: () => MapLinks.open(context, MapLinks.view(trip.latitude!, trip.longitude!)),
                    icon: const Icon(Icons.map_rounded, size: 18),
                    label: Text(tr('خرائط جوجل', 'Google Maps')),
                  ),
              ],
            ),
            if (_tripUpdatedAt != null)
              Text(tr('يتم التحديث تلقائياً · آخر تحديث ${_time(_tripUpdatedAt)}', 'Updates automatically · last at ${_time(_tripUpdatedAt)}'),
                  style: TextStyle(fontSize: 11.5, color: palette.faint)),
          ],
        ],
      ),
    );
  }

  Widget _tripCard(CurrentTrip trip) {
    final color = riderColor(trip.riderStatus);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: const LinearGradient(colors: [Color(0xFFF2B53B), _amber]),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.directions_bus_rounded, color: Colors.white, size: 28),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('${tr('باص', 'Bus')} ${trip.busNumber} · ${trip.direction.label}',
                        style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
                    if (trip.plateNumber.isNotEmpty)
                      Text(trip.plateNumber, style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5)),
                  ],
                ),
              ),
              Container(
                key: const ValueKey('bus-rider-status'),
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(20)),
                child: Text(trip.riderStatus.label, style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 12)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.fromLTRB(12, 6, 6, 6),
            decoration: BoxDecoration(color: Colors.white.withValues(alpha: .18), borderRadius: BorderRadius.circular(14)),
            child: Row(
              children: [
                const Icon(Icons.person_rounded, color: Colors.white, size: 20),
                const SizedBox(width: 6),
                Expanded(
                  child: Text('${tr('السائق', 'Driver')}: ${trip.driverName.isEmpty ? '-' : trip.driverName}',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
                ),
                if (trip.driverPhone.isNotEmpty)
                  FilledButton.icon(
                    key: const ValueKey('bus-call-driver'),
                    onPressed: () => _call(trip.driverPhone),
                    style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: _amber, visualDensity: VisualDensity.compact),
                    icon: const Icon(Icons.call_rounded, size: 18),
                    label: Text(tr('اتصال', 'Call')),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 14,
            runSpacing: 4,
            children: [
              _whiteInfo(Icons.play_arrow_rounded, '${tr('بدأت', 'Started')} ${_time(trip.startedAt)}'),
              if (trip.boardedAt != null) _whiteInfo(Icons.login_rounded, '${tr('ركب', 'Boarded')} ${_time(trip.boardedAt)}'),
              if (trip.droppedOffAt != null) _whiteInfo(Icons.logout_rounded, '${tr('نزل', 'Dropped off')} ${_time(trip.droppedOffAt)}'),
            ],
          ),
          if (!_child.hasLocation) ...[
            const SizedBox(height: 10),
            Text(tr('حدد موقع المنزل من تبويب "موقع المنزل" ليظهر على الخريطة.', 'Set the home location in the "Home location" tab to see it on the map.'),
                style: const TextStyle(color: Colors.white, fontSize: 12)),
          ],
        ],
      ),
    );
  }

  Widget _whiteInfo(IconData icon, String text) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: Colors.white),
          const SizedBox(width: 3),
          Text(text, style: const TextStyle(color: Colors.white, fontSize: 12.5)),
        ],
      );

  Widget _trackingMap(CurrentTrip trip) {
    final bus = trip.hasPosition ? LatLng(trip.latitude!, trip.longitude!) : null;
    final home = _child.hasLocation ? LatLng(_child.latitude!, _child.longitude!) : null;
    final center = bus ?? home ?? _defaultCenter;

    return FlutterMap(
      // A new key when the bus moves: the map follows it
      key: ValueKey('bus-map-${bus?.latitude}-${bus?.longitude}'),
      options: MapOptions(
        initialCenter: center,
        initialZoom: bus != null || home != null ? 15 : 11,
        minZoom: 3,
        maxZoom: 19,
        backgroundColor: context.palette.placeholder,
        interactionOptions: const InteractionOptions(flags: InteractiveFlag.all & ~InteractiveFlag.rotate),
      ),
      children: [
        if (widget.showTiles)
          TileLayer(
            urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            userAgentPackageName: 'com.schoolmanager.smart_school_parent',
            maxZoom: 19,
          ),
        MarkerLayer(
          markers: [
            if (home != null) Marker(point: home, width: 42, height: 42, child: _pinIcon(Icons.home_rounded, _green, 'home-pin')),
            if (bus != null) Marker(point: bus, width: 46, height: 46, child: _pinIcon(Icons.directions_bus_rounded, _amber, 'bus-pin')),
          ],
        ),
        if (widget.showTiles) const SimpleAttributionWidget(source: Text('OpenStreetMap contributors')),
      ],
    );
  }

  Widget _pinIcon(IconData icon, Color color, String key) => Container(
        key: ValueKey(key),
        decoration: BoxDecoration(
          color: color,
          shape: BoxShape.circle,
          border: Border.all(color: Colors.white, width: 3),
          boxShadow: const [BoxShadow(color: Color(0x55000000), blurRadius: 6, offset: Offset(0, 2))],
        ),
        child: Icon(icon, color: Colors.white, size: 22),
      );

  // ---------------------------------------------------------------------------
  // Trips
  // ---------------------------------------------------------------------------

  Widget _tripsTab() {
    final palette = context.palette;
    final trips = _trips;
    if (trips == null) return const Center(child: CircularProgressIndicator());

    return RefreshIndicator(
      onRefresh: _loadTrips,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
        children: [
          Text(tr('رحلات آخر 30 يوماً', 'Trips of the last 30 days'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
          const SizedBox(height: 10),
          if (_tripsError != null)
            _message(Icons.error_outline_rounded, _tripsError!, retry: _loadTrips)
          else if (trips.isEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 40),
              child: _message(Icons.directions_bus_outlined, tr('لا توجد رحلات في آخر 30 يوماً.', 'No trips in the last 30 days.')),
            )
          else
            for (final t in _pages.take('trips', trips)) ...[
              _tripTile(t),
              const SizedBox(height: 8),
            ],
            _pages.more(context, 'trips', trips.length, setState),
        ],
      ),
    );
  }

  Widget _tripTile(ChildTrip t) {
    final palette = context.palette;
    final rider = riderColor(t.riderStatus);
    final details = [
      if (t.busNumber.isNotEmpty) '${tr('باص', 'Bus')} ${t.busNumber}',
      if (t.boardedAt != null) '${tr('ركب', 'Boarded')} ${_time(t.boardedAt)}',
      if (t.droppedOffAt != null) '${tr('نزل', 'Dropped off')} ${_time(t.droppedOffAt)}',
    ];

    return Container(
      key: ValueKey('bus-trip-${t.tripId}'),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(16), border: Border.all(color: palette.line)),
      child: Row(
        children: [
          CircleAvatar(
            backgroundColor: _amber.withValues(alpha: .12),
            child: Icon(t.direction == TripDirection.toSchool ? Icons.school_rounded : Icons.home_rounded, color: _amber),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${_day(t.date)} · ${t.direction.label}', style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                if (details.isNotEmpty) Text(details.join(' · '), style: TextStyle(fontSize: 12, color: palette.muted)),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              _pill(t.riderStatus.label, rider),
              const SizedBox(height: 4),
              _pill(t.state.label, tripStateColor(t.state)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _pill(String text, Color color) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
        child: Text(text, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold)),
      );

  // ---------------------------------------------------------------------------
  // Home location
  // ---------------------------------------------------------------------------

  Widget _homeTab() {
    final palette = context.palette;
    final saved = _child.hasLocation ? LatLng(_child.latitude!, _child.longitude!) : null;
    final shown = _pin ?? saved;
    final changed = _pin != null;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
          child: Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(color: palette.infoTint, borderRadius: BorderRadius.circular(14)),
            child: Row(
              children: [
                const Icon(Icons.touch_app_rounded, color: AppTheme.blue),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    tr('اضغط على الخريطة في مكان منزل ${_child.name}، أو استخدم موقعك الحالي إن كنت في المنزل، ثم احفظ. يستخدمه الباص المدرسي.',
                        'Tap the map where ${_child.name}\'s home is, or use your current location if you are at home, then save. The school bus uses it.'),
                    style: TextStyle(fontSize: 12.5, color: palette.ink),
                  ),
                ),
              ],
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              key: const ValueKey('home-location-mine'),
              onPressed: _saving || _locating ? null : _useMyLocation,
              style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(46)),
              icon: _locating
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.my_location_rounded),
              label: Text(_locating ? tr('جارٍ تحديد موقعك…', 'Finding your location…') : tr('استخدم موقعي الحالي', 'Use my current location')),
            ),
          ),
        ),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(20),
              child: FlutterMap(
                mapController: _homeMap,
                options: MapOptions(
                  initialCenter: shown ?? _defaultCenter,
                  initialZoom: shown != null ? 16 : 11,
                  minZoom: 3,
                  maxZoom: 19,
                  backgroundColor: palette.placeholder,
                  interactionOptions: const InteractionOptions(flags: InteractiveFlag.all & ~InteractiveFlag.rotate),
                  onTap: _saving ? null : (_, point) => setState(() => _pin = point),
                ),
                children: [
                  if (widget.showTiles)
                    TileLayer(
                      urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                      userAgentPackageName: 'com.schoolmanager.smart_school_parent',
                      maxZoom: 19,
                    ),
                  if (shown != null)
                    MarkerLayer(markers: [
                      Marker(point: shown, width: 46, height: 46, alignment: Alignment.topCenter, child: Icon(Icons.location_on_rounded,
                          key: const ValueKey('home-location-pin'), size: 46, color: changed ? _red : _green,
                          shadows: const [Shadow(color: Color(0x55000000), blurRadius: 6, offset: Offset(0, 2))])),
                    ]),
                  if (widget.showTiles) const SimpleAttributionWidget(source: Text('OpenStreetMap contributors')),
                ],
              ),
            ),
          ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  shown == null
                      ? tr('لم يتم تحديد موقع المنزل بعد.', 'The home location is not set yet.')
                      : '${changed ? tr('الموقع الجديد', 'New location') : tr('الموقع المحفوظ', 'Saved location')}: '
                          '${shown.latitude.toStringAsFixed(6)}, ${shown.longitude.toStringAsFixed(6)}',
                  key: const ValueKey('home-location-text'),
                  textAlign: TextAlign.center,
                  textDirection: TextDirection.ltr,
                  style: TextStyle(fontSize: 12.5, color: palette.muted, fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    if (saved != null && !changed) ...[
                      OutlinedButton.icon(
                        key: const ValueKey('home-location-clear'),
                        onPressed: _saving ? null : () => _saveLocation(clear: true),
                        style: OutlinedButton.styleFrom(foregroundColor: _red, minimumSize: const Size(0, 48)),
                        icon: const Icon(Icons.delete_outline_rounded),
                        label: Text(tr('حذف', 'Remove')),
                      ),
                      const SizedBox(width: 8),
                    ],
                    if (changed) ...[
                      OutlinedButton(
                        key: const ValueKey('home-location-undo'),
                        onPressed: _saving ? null : () => setState(() => _pin = null),
                        style: OutlinedButton.styleFrom(minimumSize: const Size(0, 48)),
                        child: Text(tr('تراجع', 'Undo')),
                      ),
                      const SizedBox(width: 8),
                    ],
                    Expanded(
                      child: FilledButton.icon(
                        key: const ValueKey('home-location-save'),
                        onPressed: _saving || !changed ? null : _saveLocation,
                        style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                        icon: _saving
                            ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                            : const Icon(Icons.save_rounded),
                        label: Text(tr('حفظ الموقع', 'Save location')),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _message(IconData icon, String text, {Future<void> Function()? retry}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry != null ? _red : _amber.withValues(alpha: .6)),
              const SizedBox(height: 12),
              Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.ink)),
              if (retry != null) ...[
                const SizedBox(height: 12),
                FilledButton(onPressed: retry, child: Text(tr('إعادة المحاولة', 'Try again'))),
              ],
            ],
          ),
        ),
      );
}
