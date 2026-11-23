import 'dart:async';
import 'dart:convert';

import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// SchoolManager.Attendance.Domain.TripType (sent as its number)
enum TripDirection {
  toSchool(0),
  toHome(1);

  const TripDirection(this.code);
  final int code;

  static TripDirection fromCode(Object? v) =>
      TripDirection.values.firstWhere((t) => '${t.code}' == '$v' || t.name.toLowerCase() == '$v'.toLowerCase(), orElse: () => TripDirection.toSchool);

  String get label => switch (this) {
        TripDirection.toSchool => tr('إلى المدرسة', 'To school'),
        TripDirection.toHome => tr('إلى المنزل', 'To home'),
      };
}

/// SchoolManager.Attendance.Domain.TripStatus
enum TripState {
  planned(0),
  inProgress(1),
  completed(2),
  cancelled(3);

  const TripState(this.code);
  final int code;

  static TripState fromCode(Object? v) =>
      TripState.values.firstWhere((t) => '${t.code}' == '$v' || t.name.toLowerCase() == '$v'.toLowerCase(), orElse: () => TripState.planned);

  String get label => switch (this) {
        TripState.planned => tr('مخططة', 'Planned'),
        TripState.inProgress => tr('جارية', 'In progress'),
        TripState.completed => tr('انتهت', 'Completed'),
        TripState.cancelled => tr('ملغاة', 'Cancelled'),
      };
}

/// SchoolManager.Attendance.Domain.TripStudentStatus: the child on the trip
enum RiderStatus {
  waiting(0),
  boarded(1),
  droppedOff(2),
  absent(3);

  const RiderStatus(this.code);
  final int code;

  static RiderStatus fromCode(Object? v) {
    final code = v is num ? v.toInt() : int.tryParse('$v');
    if (code != null) return RiderStatus.values.firstWhere((s) => s.code == code, orElse: () => RiderStatus.waiting);
    return switch ('$v'.toLowerCase()) {
      'boarded' => RiderStatus.boarded,
      'droppedoff' => RiderStatus.droppedOff,
      'absent' => RiderStatus.absent,
      _ => RiderStatus.waiting,
    };
  }

  String get label => switch (this) {
        RiderStatus.waiting => tr('في الانتظار', 'Waiting'),
        RiderStatus.boarded => tr('في الباص', 'On the bus'),
        RiderStatus.droppedOff => tr('نزل', 'Dropped off'),
        RiderStatus.absent => tr('غائب', 'Absent'),
      };
}

/// The API sends UTC times without a zone
DateTime? _utc(Object? value) {
  if (value == null || '$value'.isEmpty) return null;
  final text = '$value';
  final hasZone = text.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(text);
  return DateTime.tryParse(hasZone ? text : '${text}Z')?.toLocal();
}

double? _num(Object? v) => v is num ? v.toDouble() : double.tryParse('${v ?? ''}');

/// The child's running trip today (GET /api/trip/my-child/{studentId}): bus, driver, last position, the child's status
class CurrentTrip {
  const CurrentTrip({
    required this.tripId,
    required this.direction,
    required this.busNumber,
    required this.plateNumber,
    required this.driverName,
    required this.driverPhone,
    required this.riderStatus,
    this.startedAt,
    this.latitude,
    this.longitude,
    this.locationAt,
    this.boardedAt,
    this.droppedOffAt,
  });

  final String tripId;
  final TripDirection direction;
  final DateTime? startedAt;
  final String busNumber;
  final String plateNumber;
  final String driverName;
  final String driverPhone;

  /// Last known bus position
  final double? latitude;
  final double? longitude;
  final DateTime? locationAt;

  final RiderStatus riderStatus;
  final DateTime? boardedAt;
  final DateTime? droppedOffAt;

  bool get hasPosition => latitude != null && longitude != null;

  factory CurrentTrip.fromJson(Map<String, dynamic> json) => CurrentTrip(
        tripId: '${json['tripId'] ?? ''}',
        direction: TripDirection.fromCode(json['type']),
        startedAt: _utc(json['startedAt']),
        busNumber: (json['busNumber'] as String?)?.trim() ?? '',
        plateNumber: (json['plateNumber'] as String?)?.trim() ?? '',
        driverName: (json['driverName'] as String?)?.trim() ?? '',
        driverPhone: (json['driverPhone'] as String?)?.trim() ?? '',
        latitude: _num(json['currentLatitude']),
        longitude: _num(json['currentLongitude']),
        locationAt: _utc(json['lastLocationAt']),
        riderStatus: RiderStatus.fromCode(json['studentStatus']),
        boardedAt: _utc(json['boardedAt']),
        droppedOffAt: _utc(json['droppedOffAt']),
      );
}

/// One trip of the child (GET /api/trip/my-child/{studentId}/trips)
class ChildTrip {
  const ChildTrip({
    required this.tripId,
    required this.date,
    required this.direction,
    required this.state,
    required this.busNumber,
    required this.riderStatus,
    this.plateNumber = '',
    this.driverName = '',
    this.startedAt,
    this.endedAt,
    this.boardedAt,
    this.droppedOffAt,
  });

  final String tripId;
  final DateTime date;
  final TripDirection direction;
  final TripState state;
  final String busNumber;
  final String plateNumber;
  final String driverName;
  final DateTime? startedAt;
  final DateTime? endedAt;
  final RiderStatus riderStatus;
  final DateTime? boardedAt;
  final DateTime? droppedOffAt;

  factory ChildTrip.fromJson(Map<String, dynamic> json) {
    final d = DateTime.tryParse('${json['date']}') ?? DateTime(2000);
    return ChildTrip(
      tripId: '${json['tripId'] ?? ''}',
      date: DateTime(d.year, d.month, d.day),
      direction: TripDirection.fromCode(json['type']),
      state: TripState.fromCode(json['status']),
      busNumber: (json['busNumber'] as String?)?.trim() ?? '',
      plateNumber: (json['plateNumber'] as String?)?.trim() ?? '',
      driverName: (json['driverName'] as String?)?.trim() ?? '',
      startedAt: _utc(json['startedAt']),
      endedAt: _utc(json['endedAt']),
      riderStatus: RiderStatus.fromCode(json['studentStatus']),
      boardedAt: _utc(json['boardedAt']),
      droppedOffAt: _utc(json['droppedOffAt']),
    );
  }
}

/// "yyyy-MM-dd"
String busDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// A request failed; [message] is ready to show
class BusException implements Exception {
  const BusException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

/// The child's bus trips (Attendance service) and home location (Management service), through the gateway
class BusService {
  BusService._();
  static final instance = BusService._();

  String get _api => '${AppConfig.apiBaseUrl}/api';

  /// The child's running trip now, or null when there is none
  Future<CurrentTrip?> getCurrentTrip(String studentId) async {
    final body = await _send((h) => http.get(Uri.parse('$_api/trip/my-child/${Uri.encodeComponent(studentId)}'), headers: h),
        failed: tr('تعذر تحميل الرحلة الحالية.', 'Could not load the current trip.'));
    final data = body['data'];
    return data is Map<String, dynamic> ? CurrentTrip.fromJson(data) : null;
  }

  /// The child's trips between two dates, newest first
  Future<List<ChildTrip>> getTrips(String studentId, DateTime from, DateTime to) async {
    final uri = Uri.parse('$_api/trip/my-child/${Uri.encodeComponent(studentId)}/trips')
        .replace(queryParameters: {'from': busDate(from), 'to': busDate(to)});
    final body = await _send((h) => http.get(uri, headers: h), failed: tr('تعذر تحميل الرحلات.', 'Could not load the trips.'));
    final data = body['data'];
    if (data is! List) throw BusException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(ChildTrip.fromJson).toList();
  }

  /// Where the phone is now (asks for the location permission first); a ready-to-show message when it cannot tell
  Future<(double, double)> getPhonePosition() async {
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        throw BusException(tr('خدمة الموقع مغلقة في الهاتف. افتحها ثم حاول مرة أخرى.',
            'Location is turned off on the phone. Turn it on and try again.'));
      }
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        throw BusException(tr('لم يتم السماح للتطبيق بمعرفة الموقع.', 'The app was not allowed to use the location.'));
      }
      if (permission == LocationPermission.deniedForever) {
        throw BusException(tr('الموقع مرفوض للتطبيق. اسمح به من إعدادات الهاتف.',
            'Location is blocked for the app. Allow it in the phone settings.'));
      }
      final p = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.high, timeLimit: Duration(seconds: 20)),
      );
      return (p.latitude, p.longitude);
    } on BusException {
      rethrow;
    } on TimeoutException {
      throw BusException(tr('تعذر تحديد الموقع الآن. حاول في مكان مفتوح.', 'Could not get the location right now. Try outdoors.'));
    } catch (_) {
      throw BusException(tr('تعذر تحديد موقعك الحالي.', 'Could not get your current location.'));
    }
  }

  /// Saves the child's home location (both values, or both null to clear it)
  Future<void> saveHomeLocation(String studentId, double? latitude, double? longitude) async {
    await _send(
      (h) => http.put(Uri.parse('$_api/student/my-children/${Uri.encodeComponent(studentId)}/location'),
          headers: {...h, 'Content-Type': 'application/json'}, body: jsonEncode({'latitude': latitude, 'longitude': longitude})),
      failed: tr('تعذر حفظ موقع المنزل.', 'Could not save the home location.'),
    );
  }

  Future<Map<String, dynamic>> _send(Future<http.Response> Function(Map<String, String> headers) request, {required String failed}) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw BusException(_sessionEnded, sessionExpired: true);

    final http.Response response;
    try {
      response = await request({'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'})
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw BusException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw BusException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    Map<String, dynamic>? body;
    try {
      final decoded = jsonDecode(utf8.decode(response.bodyBytes));
      if (decoded is Map<String, dynamic>) body = decoded;
    } catch (_) {}

    switch (response.statusCode) {
      case 200:
        break;
      case 400:
        throw BusException(_message('${body?['message'] ?? ''}', failed));
      case 401:
        throw BusException(_sessionEnded, sessionExpired: true);
      case 403:
        throw BusException(tr('هذا الحساب غير مرتبط بولي أمر.', 'This account is not linked to a parent.'));
      case 404:
        throw BusException(tr('هذا الطالب ليس من أبنائك.', 'This student is not one of your children.'));
      case 503:
        throw BusException(tr('الخدمة غير متاحة حالياً، حاول لاحقاً.', 'The service is not available right now. Please try later.'));
      default:
        throw BusException(failed);
    }

    if (body == null) throw BusException(_unexpected);
    final status = body['status'];
    if (status is num && status != 200) throw BusException(_message('${body['message'] ?? ''}', failed));
    return body;
  }

  static String _message(String message, String fallback) => switch (message) {
        'LatitudeInvalid' => tr('خط العرض يجب أن يكون بين -90 و 90.', 'The latitude must be between -90 and 90.'),
        'LongitudeInvalid' => tr('خط الطول يجب أن يكون بين -180 و 180.', 'The longitude must be between -180 and 180.'),
        'LocationIncomplete' => tr('حدد خط العرض وخط الطول معاً.', 'Set both latitude and longitude.'),
        _ => fallback,
      };

  static String get _unexpected => tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.');

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
