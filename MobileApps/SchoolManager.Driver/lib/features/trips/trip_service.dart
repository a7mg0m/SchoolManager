import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';
import '../profile/profile_service.dart' show SessionExpiredException;

/// Direction of a trip (backend TripType: ToSchool = 0, ToHome = 1)
enum TripType {
  toSchool,
  toHome;

  String get label => this == toSchool ? tr('رحلة الذهاب إلى المدرسة', 'To school') : tr('رحلة العودة إلى المنزل', 'To home');
  String get shortLabel => this == toSchool ? tr('ذهاب', 'To school') : tr('عودة', 'To home');

  static TripType fromJson(Object? v) => v == 1 || v == 'ToHome' ? toHome : toSchool;
}

/// Backend TripStatus: Planned = 0, InProgress = 1, Completed = 2, Cancelled = 3
enum TripStatus {
  planned,
  inProgress,
  completed,
  cancelled;

  String get label => switch (this) {
        planned => tr('مخططة', 'Planned'),
        inProgress => tr('في الطريق', 'On the road'),
        completed => tr('مكتملة', 'Completed'),
        cancelled => tr('ملغاة', 'Cancelled'),
      };

  static TripStatus fromJson(Object? v) {
    if (v is int && v >= 0 && v < values.length) return values[v];
    return switch ('$v') {
      'InProgress' => inProgress,
      'Completed' => completed,
      'Cancelled' => cancelled,
      _ => planned,
    };
  }
}

/// A student's status on a trip (backend TripStudentStatus: Pending, Boarded, DroppedOff, Absent)
enum TripStudentStatus { pending, boarded, droppedOff, absent }

class RouteStop {
  const RouteStop({required this.nameAr, required this.nameEn, required this.order});

  final String nameAr;
  final String nameEn;
  final int order;
  String get name => pickLang(nameAr, nameEn);

  factory RouteStop.fromJson(Map<String, dynamic> json) => RouteStop(
        nameAr: '${json['nameAr'] ?? ''}',
        nameEn: '${json['nameEn'] ?? ''}',
        order: (json['stopOrder'] ?? json['order'] ?? 0) is num ? ((json['stopOrder'] ?? json['order'] ?? 0) as num).toInt() : 0,
      );
}

/// The driver's bus and its route (GET /api/trip/my-bus)
class DriverBus {
  const DriverBus({
    required this.id,
    required this.number,
    required this.plateNumber,
    required this.capacity,
    this.isActive = true,
    this.routeNameAr = '',
    this.routeNameEn = '',
    this.stops = const [],
  });

  final String id;
  final String number;
  final String plateNumber;
  final int capacity;
  final bool isActive;
  final String routeNameAr;
  final String routeNameEn;
  final List<RouteStop> stops;

  String get routeName => pickLang(routeNameAr, routeNameEn);

  factory DriverBus.fromJson(Map<String, dynamic> json) {
    final bus = json['bus'] is Map<String, dynamic> ? json['bus'] as Map<String, dynamic> : const <String, dynamic>{};
    final route = json['route'] is Map<String, dynamic> ? json['route'] as Map<String, dynamic> : const <String, dynamic>{};
    final stops = (route['stops'] is List ? route['stops'] as List : const [])
        .whereType<Map<String, dynamic>>()
        .map(RouteStop.fromJson)
        .toList()
      ..sort((a, b) => a.order.compareTo(b.order));
    return DriverBus(
      id: '${bus['id'] ?? ''}',
      number: '${bus['number'] ?? ''}',
      plateNumber: '${bus['plateNumber'] ?? ''}',
      capacity: (bus['capacity'] as num?)?.toInt() ?? 0,
      isActive: bus['isActive'] != false,
      routeNameAr: '${route['nameAr'] ?? ''}',
      routeNameEn: '${route['nameEn'] ?? ''}',
      stops: stops,
    );
  }
}

/// One trip of the bus
class Trip {
  const Trip({
    required this.id,
    required this.date,
    required this.type,
    required this.status,
    this.startedAt,
    this.endedAt,
    this.studentStatuses = const [],
    this.currentLatitude,
    this.currentLongitude,
    this.lastLocationAt,
  });

  final String id;

  /// Date only (local)
  final DateTime date;
  final TripType type;
  final TripStatus status;

  /// Local time
  final DateTime? startedAt;
  final DateTime? endedAt;
  final List<TripStudentStatus> studentStatuses;

  /// Last position sent while the trip ran (local time for [lastLocationAt])
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime? lastLocationAt;

  int get students => studentStatuses.length;
  int count(TripStudentStatus s) => studentStatuses.where((x) => x == s).length;

  bool get canStart => status == TripStatus.planned;
  bool get canComplete => status == TripStatus.inProgress;
  bool get canCancel => status == TripStatus.planned || status == TripStatus.inProgress;

  bool isOn(DateTime day) => date.year == day.year && date.month == day.month && date.day == day.day;

  factory Trip.fromJson(Map<String, dynamic> json) {
    final students = (json['students'] is List ? json['students'] as List : const [])
        .whereType<Map<String, dynamic>>()
        .map((s) {
          final v = s['status'];
          final i = v is int ? v : ['Pending', 'Boarded', 'DroppedOff', 'Absent'].indexOf('$v');
          return TripStudentStatus.values[(i >= 0 && i < 4) ? i : 0];
        })
        .toList();
    return Trip(
      id: '${json['id'] ?? ''}',
      date: _date(json['date']) ?? DateTime.now(),
      type: TripType.fromJson(json['type']),
      status: TripStatus.fromJson(json['status']),
      startedAt: _utc(json['startedAt']),
      endedAt: _utc(json['endedAt']),
      studentStatuses: students,
      currentLatitude: (json['currentLatitude'] as num?)?.toDouble(),
      currentLongitude: (json['currentLongitude'] as num?)?.toDouble(),
      lastLocationAt: _utc(json['lastLocationAt']),
    );
  }

  static DateTime? _date(Object? v) {
    final s = '${v ?? ''}';
    if (s.length < 10) return null;
    return DateTime.tryParse(s.substring(0, 10));
  }

  /// The API sends UTC times (sometimes without a zone): shown in local time
  static DateTime? _utc(Object? v) {
    if (v is! String || v.isEmpty) return null;
    final hasZone = v.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(v);
    return DateTime.tryParse(hasZone ? v : '${v}Z')?.toLocal();
  }
}

/// A student on a trip, with what the driver needs (GET /api/trip/my-bus/trips/{id}/students)
class TripRider {
  const TripRider({
    required this.studentId,
    required this.status,
    this.fullNameAr = '',
    this.fullNameEn = '',
    this.gender = '',
    this.gradeNameAr = '',
    this.gradeNameEn = '',
    this.sectionNameAr = '',
    this.sectionNameEn = '',
    this.parentNameAr = '',
    this.parentNameEn = '',
    this.parentPhone = '',
    this.address = '',
    this.stopNameAr,
    this.stopNameEn,
    this.boardedAt,
    this.droppedOffAt,
    this.latitude,
    this.longitude,
  });

  final String studentId;

  /// Home location set by the parent (null when not set)
  final double? latitude;
  final double? longitude;
  bool get hasLocation => latitude != null && longitude != null;
  final TripStudentStatus status;
  final String fullNameAr;
  final String fullNameEn;
  final String gender;
  final String gradeNameAr;
  final String gradeNameEn;
  final String sectionNameAr;
  final String sectionNameEn;
  final String parentNameAr;
  final String parentNameEn;
  final String parentPhone;
  final String address;
  final String? stopNameAr;
  final String? stopNameEn;

  /// Local time
  final DateTime? boardedAt;
  final DateTime? droppedOffAt;

  String get name {
    final n = pickLang(fullNameAr, fullNameEn);
    return n.isNotEmpty ? n : tr('طالب', 'Student');
  }

  String get classLabel =>
      [pickLang(gradeNameAr, gradeNameEn), pickLang(sectionNameAr, sectionNameEn)].where((s) => s.isNotEmpty).join(' · ');
  String get parentName => pickLang(parentNameAr, parentNameEn);
  String get stopName => pickLang(stopNameAr, stopNameEn);
  bool get isFemale => gender.toLowerCase() == 'female';

  TripRider copyWith({TripStudentStatus? status, DateTime? boardedAt, DateTime? droppedOffAt}) => TripRider(
        studentId: studentId,
        status: status ?? this.status,
        fullNameAr: fullNameAr,
        fullNameEn: fullNameEn,
        gender: gender,
        gradeNameAr: gradeNameAr,
        gradeNameEn: gradeNameEn,
        sectionNameAr: sectionNameAr,
        sectionNameEn: sectionNameEn,
        parentNameAr: parentNameAr,
        parentNameEn: parentNameEn,
        parentPhone: parentPhone,
        address: address,
        stopNameAr: stopNameAr,
        stopNameEn: stopNameEn,
        boardedAt: boardedAt ?? this.boardedAt,
        droppedOffAt: droppedOffAt ?? this.droppedOffAt,
        latitude: latitude,
        longitude: longitude,
      );

  factory TripRider.fromJson(Map<String, dynamic> json) {
    final v = json['status'];
    final i = v is int ? v : ['Pending', 'Boarded', 'DroppedOff', 'Absent'].indexOf('$v');
    String s(String key) => (json[key] as String?)?.trim() ?? '';
    return TripRider(
      studentId: '${json['studentId'] ?? ''}',
      status: TripStudentStatus.values[(i >= 0 && i < 4) ? i : 0],
      fullNameAr: s('fullNameAr'),
      fullNameEn: s('fullNameEn'),
      gender: s('gender'),
      gradeNameAr: s('gradeNameAr'),
      gradeNameEn: s('gradeNameEn'),
      sectionNameAr: s('sectionNameAr'),
      sectionNameEn: s('sectionNameEn'),
      parentNameAr: s('parentNameAr'),
      parentNameEn: s('parentNameEn'),
      parentPhone: s('parentPhone'),
      address: s('address'),
      stopNameAr: json['stopNameAr'] as String?,
      stopNameEn: json['stopNameEn'] as String?,
      boardedAt: Trip._utc(json['boardedAt']),
      droppedOffAt: Trip._utc(json['droppedOffAt']),
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
    );
  }
}

/// The school's name and location (GET /api/school-info/get)
class SchoolPlace {
  const SchoolPlace({this.nameAr = '', this.nameEn = '', this.latitude, this.longitude});

  final String nameAr;
  final String nameEn;
  final double? latitude;
  final double? longitude;
  String get name {
    final n = pickLang(nameAr, nameEn);
    return n.isNotEmpty ? n : tr('المدرسة', 'School');
  }

  bool get hasLocation => latitude != null && longitude != null;

  factory SchoolPlace.fromJson(Map<String, dynamic> json) => SchoolPlace(
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        latitude: (json['latitude'] as num?)?.toDouble(),
        longitude: (json['longitude'] as num?)?.toDouble(),
      );
}

/// A request failed; [message] is ready to show
class TripException implements Exception {
  const TripException(this.message);
  final String message;

  @override
  String toString() => message;
}

/// The trip is no longer on the road (finished or cancelled, e.g. by the school office)
class TripNotRunningException extends TripException {
  const TripNotRunningException() : super('The trip is not running.');
}

/// "yyyy-MM-dd"
String isoDate(DateTime d) => '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// The driver's bus and trips (Attendance service, through the gateway: /api/trip/my-bus)
class TripService {
  TripService._();
  static final instance = TripService._();

  static const _base = '/api/trip/my-bus';

  Future<DriverBus> getMyBus() async {
    final response = await _send('GET', _base);
    final data = _okData(response, tr('تعذر تحميل بيانات الحافلة.', 'Could not load your bus.'));
    if (data is Map<String, dynamic>) return DriverBus.fromJson(data);
    throw TripException(tr('تعذر تحميل بيانات الحافلة.', 'Could not load your bus.'));
  }

  /// Trips between two dates (both included), newest first
  Future<List<Trip>> getTrips({DateTime? from, DateTime? to}) async {
    final query = <String, String>{
      if (from != null) 'from': isoDate(from),
      if (to != null) 'to': isoDate(to),
    };
    final path = query.isEmpty ? '$_base/trips' : '$_base/trips?${Uri(queryParameters: query).query}';
    final response = await _send('GET', path);
    final data = _okData(response, tr('تعذر تحميل الرحلات.', 'Could not load the trips.'));
    return (data is List ? data : const []).whereType<Map<String, dynamic>>().map(Trip.fromJson).toList();
  }

  Future<Trip> getTrip(String id) async {
    final response = await _send('GET', '$_base/trips/$id');
    final data = _okData(response, tr('تعذر تحميل الرحلة.', 'Could not load the trip.'));
    if (data is Map<String, dynamic>) return Trip.fromJson(data);
    throw TripException(tr('تعذر تحميل الرحلة.', 'Could not load the trip.'));
  }

  /// Plans a trip; every student riding the bus that day is added. Returns the new trip id.
  Future<String> createTrip(DateTime date, TripType type) async {
    final response = await _send('POST', '$_base/trips', {'date': isoDate(date), 'type': type.index});
    final body = _body(response);
    if (response.statusCode == 200 && (body?['status'] ?? 200) == 200) {
      return '${body?['insertedId'] ?? ''}';
    }
    throw TripException(_errorMessage(response, body, tr('تعذر إنشاء الرحلة.', 'Could not create the trip.')));
  }

  /// The trip's students with their status, stop, class and parent's phone, in stop order
  Future<List<TripRider>> getTripStudents(String tripId) async {
    final response = await _send('GET', '$_base/trips/$tripId/students');
    final data = _okData(response, tr('تعذر تحميل طلاب الرحلة.', 'Could not load the students of the trip.'));
    return (data is List ? data : const []).whereType<Map<String, dynamic>>().map(TripRider.fromJson).toList();
  }

  /// A student boarded, arrived (dropped off) or is absent; the trip must be on the road
  Future<void> markStudent(String tripId, String studentId, TripStudentStatus status) async {
    final response = await _send('PUT', '$_base/trips/$tripId/students/$studentId', {'status': status.index});
    final body = _body(response);
    if (response.statusCode == 200 && (body?['status'] ?? 200) == 200) return;
    throw TripException(_errorMessage(response, body, tr('تعذر حفظ حالة الطالب.', "Could not save the student's status.")));
  }

  /// The bus's current position while the trip runs
  Future<void> updateLocation(String tripId, double latitude, double longitude) async {
    final response = await _send('PUT', '$_base/trips/$tripId/location', {'latitude': latitude, 'longitude': longitude});
    final body = _body(response);
    if (response.statusCode == 200 && (body?['status'] ?? 200) == 200) return;
    if (body?['message'] == 'The trip is not running.') throw const TripNotRunningException();
    throw TripException(_errorMessage(response, body, tr('تعذر إرسال موقع الحافلة.', "Could not send the bus's location.")));
  }

  /// The school's location (null when the school has not entered it)
  Future<SchoolPlace?> getSchool() async {
    final response = await _send('GET', '/api/school-info/get');
    if (response.statusCode != 200) return null;
    final data = _body(response)?['data'];
    return data is Map<String, dynamic> ? SchoolPlace.fromJson(data) : null;
  }

  Future<void> start(String id) => _change(id, 'start', tr('تعذر بدء الرحلة.', 'Could not start the trip.'));
  Future<void> complete(String id) => _change(id, 'complete', tr('تعذر إنهاء الرحلة.', 'Could not finish the trip.'));
  Future<void> cancel(String id) => _change(id, 'cancel', tr('تعذر إلغاء الرحلة.', 'Could not cancel the trip.'));

  Future<void> _change(String id, String action, String fallback) async {
    final response = await _send('PUT', '$_base/trips/$id/$action');
    final body = _body(response);
    if (response.statusCode == 200 && (body?['status'] ?? 200) == 200) return;
    throw TripException(_errorMessage(response, body, fallback));
  }

  Object? _okData(http.Response response, String fallback) {
    final body = _body(response);
    if (response.statusCode == 200 && (body?['status'] ?? 200) == 200) return body?['data'];
    throw TripException(_errorMessage(response, body, fallback));
  }

  Future<http.Response> _send(String method, String path, [Map<String, dynamic>? body]) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw const SessionExpiredException();

    final uri = Uri.parse('${AppConfig.apiBaseUrl}$path');
    final headers = {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ${session.token}',
    };

    final http.Response response;
    try {
      response = await switch (method) {
        'POST' => http.post(uri, headers: headers, body: jsonEncode(body ?? const {})),
        'PUT' => http.put(uri, headers: headers, body: jsonEncode(body ?? const {})),
        _ => http.get(uri, headers: headers),
      }
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw TripException(tr('انتهت مهلة الاتصال بالخادم، حاول مرة أخرى.', 'The server took too long to respond. Please try again.'));
    } catch (_) {
      throw TripException(tr('تعذر الاتصال بالخادم، تأكد من اتصالك بالإنترنت.', 'Could not reach the server. Check your internet connection.'));
    }

    if (response.statusCode == 401) throw const SessionExpiredException();
    return response;
  }

  static Map<String, dynamic>? _body(http.Response response) {
    try {
      final decoded = jsonDecode(utf8.decode(response.bodyBytes));
      return decoded is Map<String, dynamic> ? decoded : null;
    } catch (_) {
      return null;
    }
  }

  /// The API sends an error key or an English sentence in `message`
  static String _errorMessage(http.Response response, Map<String, dynamic>? body, String fallback) {
    final key = body?['message'] as String?;
    return switch (key) {
      'NotADriver' => tr('هذا الحساب غير مرتبط بحافلة.', 'This account is not linked to a bus.'),
      'BusNotFound' => tr('لم يتم العثور على الحافلة.', 'Your bus was not found.'),
      'TripNotFound' => tr('لم يتم العثور على الرحلة.', 'The trip was not found.'),
      'The bus already has this trip on this date.' =>
        tr('هذه الرحلة موجودة بالفعل لهذا اليوم.', 'This trip already exists for that day.'),
      'The bus is not active.' => tr('الحافلة غير مفعّلة، تواصل مع إدارة المدرسة.', 'The bus is not active. Please contact the school office.'),
      'Only a planned trip can start.' => tr('يمكن بدء الرحلة المخططة فقط.', 'Only a planned trip can start.'),
      'Only a running trip can be completed.' => tr('يمكن إنهاء الرحلة الجارية فقط.', 'Only a running trip can be finished.'),
      'A completed trip cannot be cancelled.' => tr('لا يمكن إلغاء رحلة مكتملة.', 'A completed trip cannot be cancelled.'),
      'Trip type is not valid.' => tr('نوع الرحلة غير صحيح.', 'The trip type is not valid.'),
      'The trip is not running.' => tr('ابدأ الرحلة أولاً لتسجيل الطلاب.', 'Start the trip first to mark the students.'),
      'Only a waiting student can board.' => tr('هذا الطالب سُجّل من قبل.', 'This student was already marked.'),
      'Only a student on the bus can be dropped off.' =>
        tr('سجّل ركوب الطالب أولاً.', 'Mark the student as on the bus first.'),
      'Only a waiting student can be marked absent.' => tr('هذا الطالب سُجّل من قبل.', 'This student was already marked.'),
      'The student is not on this trip.' => tr('الطالب ليس في هذه الرحلة.', 'The student is not on this trip.'),
      _ => response.statusCode >= 500 ? tr('حدث خطأ في الخادم، حاول لاحقاً.', 'A server error occurred. Please try again later.') : fallback,
    };
  }
}
