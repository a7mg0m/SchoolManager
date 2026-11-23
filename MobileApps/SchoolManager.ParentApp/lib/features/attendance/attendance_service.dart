import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// The child's whole school day (SchoolManager.Attendance.Domain.DailyAttendanceStatus, sent as its number)
enum DayStatus {
  present(0),
  absent(1),
  partiallyPresent(2),
  excused(3);

  const DayStatus(this.code);
  final int code;

  static DayStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return DayStatus.values.firstWhere((s) => s.code == code, orElse: () => DayStatus.present);
  }

  String get label => switch (this) {
        DayStatus.present => tr('حاضر', 'Present'),
        DayStatus.absent => tr('غائب', 'Absent'),
        DayStatus.partiallyPresent => tr('حاضر جزئياً', 'Partly present'),
        DayStatus.excused => tr('غياب بعذر', 'Excused'),
      };
}

/// The child's status in one lesson (SchoolManager.Attendance.Domain.PeriodAttendanceStatus)
enum LessonStatus {
  present(0),
  absent(1),
  late(2),
  excused(3);

  const LessonStatus(this.code);
  final int code;

  static LessonStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return LessonStatus.values.firstWhere((s) => s.code == code, orElse: () => LessonStatus.present);
  }

  String get label => switch (this) {
        LessonStatus.present => tr('حاضر', 'Present'),
        LessonStatus.absent => tr('غائب', 'Absent'),
        LessonStatus.late => tr('متأخر', 'Late'),
        LessonStatus.excused => tr('بعذر', 'Excused'),
      };
}

/// "08:30:00" -> 510 (null when missing)
int? _minutes(Object? value) {
  if (value == null) return null;
  final parts = '$value'.split(':');
  if (parts.length < 2) return null;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}

/// One lesson of the child's day
class AttendanceLesson {
  const AttendanceLesson({
    required this.timetableSlotId,
    required this.subjectId,
    required this.start,
    required this.status,
    this.absenceReason,
  });

  final String timetableSlotId;
  final String subjectId;

  /// Minutes since midnight
  final int start;
  final LessonStatus status;
  final String? absenceReason;

  factory AttendanceLesson.fromJson(Map<String, dynamic> json) {
    final reason = (json['absenceReason'] as String?)?.trim();
    return AttendanceLesson(
      timetableSlotId: '${json['timetableSlotId'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      start: _minutes(json['startTime']) ?? 0,
      status: LessonStatus.fromCode(json['status']),
      absenceReason: reason == null || reason.isEmpty ? null : reason,
    );
  }
}

/// One school day of the child (GET /api/daily-attendance/my-child/{studentId}?from=&to=)
class AttendanceDay {
  const AttendanceDay({
    required this.date,
    required this.status,
    this.checkIn,
    this.checkOut,
    this.notes,
    this.attendedLessons = 0,
    this.absentLessons = 0,
    this.lessons = const [],
  });

  final DateTime date;
  final DayStatus status;

  /// Minutes since midnight (null: not recorded)
  final int? checkIn;
  final int? checkOut;
  final String? notes;
  final int attendedLessons;
  final int absentLessons;

  /// By start time
  final List<AttendanceLesson> lessons;

  int get lateLessons => lessons.where((l) => l.status == LessonStatus.late).length;

  factory AttendanceDay.fromJson(Map<String, dynamic> json) {
    final d = DateTime.tryParse('${json['date']}') ?? DateTime(2000);
    final notes = (json['notes'] as String?)?.trim();
    return AttendanceDay(
      date: DateTime(d.year, d.month, d.day),
      status: DayStatus.fromCode(json['status']),
      checkIn: _minutes(json['checkInTime']),
      checkOut: _minutes(json['checkOutTime']),
      notes: notes == null || notes.isEmpty ? null : notes,
      attendedLessons: (json['attendedPeriodsCount'] as num?)?.toInt() ?? 0,
      absentLessons: (json['absentPeriodsCount'] as num?)?.toInt() ?? 0,
      lessons: (json['periods'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(AttendanceLesson.fromJson)
          .toList()
        ..sort((a, b) => a.start.compareTo(b.start)),
    );
  }
}

/// "yyyy-MM-dd"
String attendanceDate(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

/// Loading failed; [message] is ready to show
class AttendanceException implements Exception {
  const AttendanceException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class AttendanceService {
  AttendanceService._();
  static final instance = AttendanceService._();

  /// The child's school days between [from] and [to] (both included); days with nothing recorded are missing
  Future<List<AttendanceDay>> getChildAttendance(String studentId, DateTime from, DateTime to) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw AttendanceException(_sessionEnded, sessionExpired: true);

    final uri = Uri.parse('${AppConfig.apiBaseUrl}/api/daily-attendance/my-child/${Uri.encodeComponent(studentId)}')
        .replace(queryParameters: {'from': attendanceDate(from), 'to': attendanceDate(to)});

    final http.Response response;
    try {
      response = await http.get(uri, headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'})
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw AttendanceException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw AttendanceException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw AttendanceException(_sessionEnded, sessionExpired: true);
      case 403:
        throw AttendanceException(tr('هذا الحساب غير مرتبط بولي أمر.', 'This account is not linked to a parent.'));
      case 404:
        throw AttendanceException(tr('هذا الطالب ليس من أبنائك.', 'This student is not one of your children.'));
      case 503:
        throw AttendanceException(tr('خدمة الحضور غير متاحة حالياً، حاول لاحقاً.', 'The attendance service is not available right now. Please try later.'));
      default:
        throw AttendanceException(tr('تعذر تحميل الحضور.', 'Could not load the attendance.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(AttendanceDay.fromJson).toList();
    } catch (_) {
      throw AttendanceException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
