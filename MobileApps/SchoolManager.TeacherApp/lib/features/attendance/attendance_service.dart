import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';
import '../timetable/teacher_timetable_service.dart';

/// A student's status in one lesson (SchoolManager.Attendance.Domain.PeriodAttendanceStatus, sent as its number)
enum LessonAttendanceStatus {
  present(0),
  absent(1),
  late(2),
  excused(3);

  const LessonAttendanceStatus(this.code);
  final int code;

  static LessonAttendanceStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return LessonAttendanceStatus.values.firstWhere((s) => s.code == code, orElse: () => LessonAttendanceStatus.present);
  }

  String get label => switch (this) {
        LessonAttendanceStatus.present => tr('حاضر', 'Present'),
        LessonAttendanceStatus.absent => tr('غائب', 'Absent'),
        LessonAttendanceStatus.late => tr('متأخر', 'Late'),
        LessonAttendanceStatus.excused => tr('بعذر', 'Excused'),
      };

  /// Absent and excused students can have a reason
  bool get hasReason => this == LessonAttendanceStatus.absent || this == LessonAttendanceStatus.excused;
}

/// What was saved for one student in a lesson (GET /api/daily-attendance/get-by-period/{slotId}?date=)
class RecordedAttendance {
  const RecordedAttendance({required this.studentId, required this.status, this.absenceReason});

  final String studentId;
  final LessonAttendanceStatus status;
  final String? absenceReason;

  factory RecordedAttendance.fromJson(Map<String, dynamic> json) => RecordedAttendance(
        studentId: '${json['studentId'] ?? ''}',
        status: LessonAttendanceStatus.fromCode(json['status']),
        absenceReason: (json['absenceReason'] as String?)?.trim(),
      );
}

/// One student's status to save
class StudentAttendanceInput {
  const StudentAttendanceInput({required this.studentId, required this.status, this.absenceReason});

  final String studentId;
  final LessonAttendanceStatus status;
  final String? absenceReason;

  Map<String, dynamic> toJson() => {
        'studentId': studentId,
        'status': status.code,
        'absenceReason': status.hasReason && (absenceReason ?? '').trim().isNotEmpty ? absenceReason!.trim() : null,
      };
}

/// A student's whole school day (SchoolManager.Attendance.Domain.DailyAttendanceStatus, sent as its number)
enum DayAttendanceStatus {
  present(0),
  absent(1),
  partiallyPresent(2),
  excused(3);

  const DayAttendanceStatus(this.code);
  final int code;

  static DayAttendanceStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return DayAttendanceStatus.values.firstWhere((s) => s.code == code, orElse: () => DayAttendanceStatus.present);
  }

  String get label => switch (this) {
        DayAttendanceStatus.present => tr('حاضر', 'Present'),
        DayAttendanceStatus.absent => tr('غائب', 'Absent'),
        DayAttendanceStatus.partiallyPresent => tr('حاضر جزئياً', 'Partly present'),
        DayAttendanceStatus.excused => tr('غياب بعذر', 'Excused'),
      };
}

/// One lesson of a student's day (by any teacher of the class)
class DayLesson {
  const DayLesson({
    required this.timetableSlotId,
    required this.subjectId,
    required this.teacherId,
    required this.start,
    required this.status,
    this.absenceReason,
  });

  final String timetableSlotId;
  final String subjectId;
  final String teacherId;

  /// Minutes since midnight
  final int start;
  final LessonAttendanceStatus status;
  final String? absenceReason;

  factory DayLesson.fromJson(Map<String, dynamic> json) => DayLesson(
        timetableSlotId: '${json['timetableSlotId'] ?? ''}',
        subjectId: '${json['subjectId'] ?? ''}',
        teacherId: '${json['teacherId'] ?? ''}',
        start: timeToMinutes(json['startTime']),
        status: LessonAttendanceStatus.fromCode(json['status']),
        absenceReason: (json['absenceReason'] as String?)?.trim(),
      );
}

/// A student's day in the class (GET /api/daily-attendance/get-by-section/{sectionId}?date=)
class StudentDay {
  const StudentDay({
    required this.studentId,
    required this.status,
    this.checkIn,
    this.checkOut,
    this.notes,
    this.attendedLessons = 0,
    this.absentLessons = 0,
    this.lessons = const [],
  });

  final String studentId;
  final DayAttendanceStatus status;

  /// Minutes since midnight (null: not recorded)
  final int? checkIn;
  final int? checkOut;
  final String? notes;
  final int attendedLessons;
  final int absentLessons;

  /// By start time
  final List<DayLesson> lessons;

  factory StudentDay.fromJson(Map<String, dynamic> json) {
    int? time(Object? v) => v == null || '$v'.isEmpty ? null : timeToMinutes(v);
    final notes = (json['notes'] as String?)?.trim();
    return StudentDay(
      studentId: '${json['studentId'] ?? ''}',
      status: DayAttendanceStatus.fromCode(json['status']),
      checkIn: time(json['checkInTime']),
      checkOut: time(json['checkOutTime']),
      notes: notes == null || notes.isEmpty ? null : notes,
      attendedLessons: (json['attendedPeriodsCount'] as num?)?.toInt() ?? 0,
      absentLessons: (json['absentPeriodsCount'] as num?)?.toInt() ?? 0,
      lessons: (json['periods'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(DayLesson.fromJson)
          .toList()
        ..sort((a, b) => a.start.compareTo(b.start)),
    );
  }
}

/// Longest absence reason the API accepts (PeriodAttendance.MaxAbsenceReasonLength)
const maxAbsenceReasonLength = 500;

/// "yyyy-MM-dd"
String attendanceDate(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

/// 510 -> "08:30:00"
String minutesToTime(int minutes) =>
    '${(minutes ~/ 60).toString().padLeft(2, '0')}:${(minutes % 60).toString().padLeft(2, '0')}:00';

/// A request failed; [message] is ready to show
class AttendanceException implements Exception {
  const AttendanceException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

/// Lesson attendance of the logged-in teacher (Attendance service through the gateway)
class AttendanceService {
  AttendanceService._();
  static final instance = AttendanceService._();

  String get _base => '${AppConfig.apiBaseUrl}/api/daily-attendance';

  /// What was already saved for [lesson] on [date] (empty when nothing yet)
  Future<List<RecordedAttendance>> getLessonAttendance(TeacherLesson lesson, DateTime date) async {
    final uri = Uri.parse('$_base/get-by-period/${Uri.encodeComponent(lesson.id)}')
        .replace(queryParameters: {'date': attendanceDate(date)});
    final body = await _send((headers) => http.get(uri, headers: headers),
        failed: tr('تعذر تحميل الحضور.', 'Could not load the attendance.'));
    final data = body['data'];
    if (data is! List) throw AttendanceException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(RecordedAttendance.fromJson).toList();
  }

  /// The days of the class's students on [date] (students with nothing recorded are missing)
  Future<List<StudentDay>> getClassAttendance(String sectionId, DateTime date) async {
    final uri = Uri.parse('$_base/get-by-section/${Uri.encodeComponent(sectionId)}')
        .replace(queryParameters: {'date': attendanceDate(date)});
    final body = await _send((headers) => http.get(uri, headers: headers),
        failed: tr('تعذر تحميل حضور الفصل.', 'Could not load the class attendance.'));
    final data = body['data'];
    if (data is! List) throw AttendanceException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(StudentDay.fromJson).toList();
  }

  /// Saves the students' status in [lesson] on [date] (sending the same lesson again updates it)
  Future<void> recordLesson({
    required TeacherLesson lesson,
    required DateTime date,
    required String academicYearId,
    required List<StudentAttendanceInput> students,
  }) async {
    final payload = jsonEncode({
      'timetableSlotId': lesson.id,
      'subjectId': lesson.subjectId,
      'sectionId': lesson.sectionId,
      'gradeId': lesson.gradeId,
      'academicYearId': academicYearId,
      'date': attendanceDate(date),
      'startTime': minutesToTime(lesson.start),
      'students': students.map((s) => s.toJson()).toList(),
    });
    await _send(
      (headers) => http.post(Uri.parse('$_base/record-period'),
          headers: {...headers, 'Content-Type': 'application/json'}, body: payload),
      failed: tr('تعذر حفظ الحضور.', 'Could not save the attendance.'),
    );
  }

  /// Sends a request with the session token; returns the decoded body of a successful response
  Future<Map<String, dynamic>> _send(
    Future<http.Response> Function(Map<String, String> headers) request, {
    required String failed,
  }) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw AttendanceException(_sessionEnded, sessionExpired: true);

    final http.Response response;
    try {
      response = await request({'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'})
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw AttendanceException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw AttendanceException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
      case 201:
        break;
      case 401:
        throw AttendanceException(_sessionEnded, sessionExpired: true);
      case 403:
        throw AttendanceException(tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'));
      case 503:
        throw AttendanceException(tr('خدمة الحضور غير متاحة الآن، حاول لاحقاً.', 'The attendance service is not available right now. Try again later.'));
      default:
        throw AttendanceException(failed);
    }

    final Object? body;
    try {
      body = jsonDecode(utf8.decode(response.bodyBytes));
    } catch (_) {
      throw AttendanceException(_unexpected);
    }
    if (body is! Map<String, dynamic>) throw AttendanceException(_unexpected);
    // Rules broken (e.g. no students) come back as 200 with status 400 and a message
    final status = body['status'];
    if (status is num && status != 200) throw AttendanceException(_message('${body['message'] ?? ''}', failed));
    return body;
  }

  /// Arabic / English text of an API message
  static String _message(String message, String fallback) => switch (message) {
        'Select at least one student.' => tr('لا يوجد طلاب للحفظ.', 'There are no students to save.'),
        'Only a teacher can record lesson attendance.' || 'NotATeacher' =>
          tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'),
        'Academic year is required.' => tr('لا توجد سنة دراسية.', 'There is no academic year.'),
        'Attendance status is not valid.' => tr('حالة الحضور غير صحيحة.', 'The attendance status is not valid.'),
        _ when message.startsWith('Absence reason cannot exceed') =>
          tr('سبب الغياب طويل جداً (الحد $maxAbsenceReasonLength حرف).', 'The absence reason is too long (max $maxAbsenceReasonLength characters).'),
        _ => fallback,
      };

  static String get _unexpected => tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.');

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
