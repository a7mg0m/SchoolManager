import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// Teacher app "student activity" of one student the teacher teaches: profile, class timetable, the week's
/// attendance with the teachers' reviews, and exams. Every call is checked by the server (a student the teacher
/// does not teach: 404).

// ---------------- Helpers ----------------

/// "08:30:00" -> 510
int _minutes(Object? value) {
  final parts = '$value'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}

int? _minutesOrNull(Object? value) => value == null ? null : _minutes(value);

double? _num(Object? value) => value is num ? value.toDouble() : (value == null ? null : double.tryParse('$value'));

/// .NET DayOfWeek as a number or a name -> 0 = Sunday … 6 = Saturday
int _dayOfWeek(Object? value) {
  if (value is num) return value.toInt() % 7;
  const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
  final index = names.indexOf('$value'.toLowerCase());
  return index < 0 ? 0 : index;
}

String _pick(Map<String, dynamic> json, String ar, String en) =>
    pickLang((json[ar] as String?)?.trim() ?? '', (json[en] as String?)?.trim() ?? '');

DateTime _day(Object? value) {
  final d = DateTime.tryParse('$value') ?? DateTime(2000);
  return DateTime(d.year, d.month, d.day);
}

/// "yyyy-MM-dd"
String activityDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

const _months = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
const _monthsEn = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

// ---------------- Profile, timetable, teachers ----------------

/// The student (GET /api/teacher/my-student/{id})
class ActivityStudent {
  const ActivityStudent({
    required this.id,
    required this.nameAr,
    required this.nameEn,
    this.gender = '',
    this.stageName = '',
    this.gradeName = '',
    this.sectionName = '',
  });

  final String id;
  final String nameAr;
  final String nameEn;
  final String gender;
  final String stageName;
  final String gradeName;
  final String sectionName;

  String get name => pickLang(nameAr, nameEn);
  bool get isFemale => gender.toLowerCase().startsWith('f');

  factory ActivityStudent.fromJson(Map<String, dynamic> json) => ActivityStudent(
        id: '${json['id'] ?? ''}',
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        gender: '${json['gender'] ?? ''}',
        stageName: _pick(json, 'stageNameAr', 'stageNameEn'),
        gradeName: _pick(json, 'gradeNameAr', 'gradeNameEn'),
        sectionName: _pick(json, 'sectionNameAr', 'sectionNameEn'),
      );
}

/// One lesson of the class timetable
class ActivitySlot {
  const ActivitySlot({required this.id, required this.start, required this.end, this.subject = '', this.teacher = ''});

  final String id;

  /// Minutes since midnight
  final int start;
  final int end;
  final String subject;
  final String teacher;

  factory ActivitySlot.fromJson(Map<String, dynamic> json) => ActivitySlot(
        id: '${json['id'] ?? ''}',
        start: _minutes(json['startTime']),
        end: _minutes(json['endTime']),
        subject: _pick(json, 'subjectNameAr', 'subjectNameEn'),
        teacher: _pick(json, 'teacherNameAr', 'teacherNameEn'),
      );
}

/// Lessons of one day of the week (0 = Sunday … 6 = Saturday)
class ActivityTimetableDay {
  const ActivityTimetableDay({required this.dayOfWeek, required this.slots});
  final int dayOfWeek;
  final List<ActivitySlot> slots;
}

/// A teacher of the student's grade, with the subjects they teach it
class ActivityTeacher {
  const ActivityTeacher({required this.id, required this.name, this.subjects = const {}});
  final String id;
  final String name;

  /// Subject id -> name
  final Map<String, String> subjects;
}

/// Profile + class timetable + teachers
class StudentActivityInfo {
  const StudentActivityInfo({required this.student, this.days = const [], this.teachers = const []});

  final ActivityStudent student;
  final List<ActivityTimetableDay> days;
  final List<ActivityTeacher> teachers;

  ActivityTimetableDay? dayOf(int dayOfWeek) => days.where((d) => d.dayOfWeek == dayOfWeek).firstOrNull;

  factory StudentActivityInfo.fromJson(Map<String, dynamic> json) {
    final timetable = json['timetable'] is Map ? Map<String, dynamic>.from(json['timetable'] as Map) : const <String, dynamic>{};
    return StudentActivityInfo(
      student: ActivityStudent.fromJson(json['student'] is Map ? Map<String, dynamic>.from(json['student'] as Map) : const {}),
      days: (timetable['days'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map((d) => ActivityTimetableDay(
                dayOfWeek: _dayOfWeek(d['dayOfWeek']),
                slots: (d['slots'] as List? ?? const []).whereType<Map<String, dynamic>>().map(ActivitySlot.fromJson).toList(),
              ))
          .where((d) => d.slots.isNotEmpty)
          .toList(),
      teachers: (json['teachers'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map((t) => ActivityTeacher(
                id: '${t['teacherId'] ?? ''}',
                name: _pick(t, 'nameAr', 'nameEn'),
                subjects: {
                  for (final s in (t['subjects'] as List? ?? const []).whereType<Map<String, dynamic>>())
                    '${s['subjectId'] ?? ''}': _pick(s, 'nameAr', 'nameEn'),
                },
              ))
          .toList(),
    );
  }
}

// ---------------- Attendance ----------------

/// The student's whole school day (Attendance DailyAttendanceStatus)
enum ActivityDayStatus {
  present,
  absent,
  partiallyPresent,
  excused;

  static ActivityDayStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value') ?? 0;
    return ActivityDayStatus.values[code.clamp(0, 3)];
  }

  String get label => switch (this) {
        ActivityDayStatus.present => tr('حاضر', 'Present'),
        ActivityDayStatus.absent => tr('غائب', 'Absent'),
        ActivityDayStatus.partiallyPresent => tr('حاضر جزئياً', 'Partly present'),
        ActivityDayStatus.excused => tr('غياب بعذر', 'Excused'),
      };
}

/// The student's status in one lesson (Attendance PeriodAttendanceStatus)
enum ActivityLessonStatus {
  present,
  absent,
  late,
  excused;

  static ActivityLessonStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value') ?? 0;
    return ActivityLessonStatus.values[code.clamp(0, 3)];
  }

  String get label => switch (this) {
        ActivityLessonStatus.present => tr('حاضر', 'Present'),
        ActivityLessonStatus.absent => tr('غائب', 'Absent'),
        ActivityLessonStatus.late => tr('متأخر', 'Late'),
        ActivityLessonStatus.excused => tr('بعذر', 'Excused'),
      };
}

class ActivityLesson {
  const ActivityLesson({required this.timetableSlotId, required this.subjectId, required this.start, required this.status, this.absenceReason});

  final String timetableSlotId;
  final String subjectId;
  final int start;
  final ActivityLessonStatus status;
  final String? absenceReason;

  factory ActivityLesson.fromJson(Map<String, dynamic> json) {
    final reason = (json['absenceReason'] as String?)?.trim();
    return ActivityLesson(
      timetableSlotId: '${json['timetableSlotId'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      start: _minutes(json['startTime']),
      status: ActivityLessonStatus.fromCode(json['status']),
      absenceReason: reason == null || reason.isEmpty ? null : reason,
    );
  }
}

/// One school day (GET /api/daily-attendance/my-student/{id}?from=&to=)
class ActivityDay {
  const ActivityDay({
    required this.date,
    required this.status,
    this.checkIn,
    this.attendedLessons = 0,
    this.absentLessons = 0,
    this.lessons = const [],
  });

  final DateTime date;
  final ActivityDayStatus status;
  final int? checkIn;
  final int attendedLessons;
  final int absentLessons;
  final List<ActivityLesson> lessons;

  int get lateLessons => lessons.where((l) => l.status == ActivityLessonStatus.late).length;

  factory ActivityDay.fromJson(Map<String, dynamic> json) => ActivityDay(
        date: _day(json['date']),
        status: ActivityDayStatus.fromCode(json['status']),
        checkIn: _minutesOrNull(json['checkInTime']),
        attendedLessons: (json['attendedPeriodsCount'] as num?)?.toInt() ?? 0,
        absentLessons: (json['absentPeriodsCount'] as num?)?.toInt() ?? 0,
        lessons: (json['periods'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(ActivityLesson.fromJson)
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start)),
      );
}

// ---------------- Reviews ----------------

enum ActivityHealth {
  good,
  tired,
  sick;

  static ActivityHealth fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value') ?? 0;
    return ActivityHealth.values[code.clamp(0, 2)];
  }

  String get label => switch (this) {
        ActivityHealth.good => tr('جيدة', 'Good'),
        ActivityHealth.tired => tr('متعب', 'Tired'),
        ActivityHealth.sick => tr('مريض', 'Sick'),
      };
}

/// Rating 1..5
String activityRatingLabel(int rating) => switch (rating) {
      1 => tr('ضعيف', 'Weak'),
      2 => tr('مقبول', 'Acceptable'),
      3 => tr('جيد', 'Good'),
      4 => tr('جيد جداً', 'Very good'),
      _ => tr('ممتاز', 'Excellent'),
    };

/// A teacher's review of the student in a lesson (GET /api/student-evaluation/my-student/{id}?from=&to=)
class ActivityReview {
  const ActivityReview({
    required this.id,
    required this.teacherId,
    required this.subjectId,
    required this.date,
    required this.focus,
    required this.interaction,
    required this.behavior,
    required this.health,
    this.comment,
  });

  final String id;
  final String teacherId;
  final String subjectId;
  final DateTime date;
  final int focus;
  final int interaction;
  final int behavior;
  final ActivityHealth health;
  final String? comment;

  double get score => (focus + interaction + behavior) / 3;

  factory ActivityReview.fromJson(Map<String, dynamic> json) {
    int rating(Object? v) => ((v is num ? v.toInt() : int.tryParse('$v')) ?? 3).clamp(1, 5);
    final comment = (json['comment'] as String?)?.trim();
    return ActivityReview(
      id: '${json['id'] ?? ''}',
      teacherId: '${json['teacherId'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      date: _day(json['date']),
      focus: rating(json['focus']),
      interaction: rating(json['interaction']),
      behavior: rating(json['behavior']),
      health: ActivityHealth.fromCode(json['healthStatus']),
      comment: comment == null || comment.isEmpty ? null : comment,
    );
  }
}

// ---------------- Exams ----------------

enum ActivityExamKind {
  quiz('اختبار قصير', 'Quiz'),
  monthly('امتحان شهري', 'Monthly exam'),
  midTerm('منتصف الفصل', 'Mid-term'),
  finalExam('نهائي', 'Final');

  const ActivityExamKind(this._ar, this._en);
  final String _ar;
  final String _en;
  String get label => tr(_ar, _en);

  static ActivityExamKind from(Object? value) {
    final v = value is num ? value.toInt() : int.tryParse('$value');
    if (v != null) return ActivityExamKind.values[(v - 1).clamp(0, 3)];
    return switch ('$value'.toLowerCase()) {
      'monthly' => ActivityExamKind.monthly,
      'midterm' => ActivityExamKind.midTerm,
      'final' => ActivityExamKind.finalExam,
      _ => ActivityExamKind.quiz,
    };
  }
}

/// An exam of the student with their result (GET /api/exam/my-student/{id})
class ActivityExam {
  const ActivityExam({
    required this.id,
    required this.kind,
    required this.subject,
    required this.date,
    required this.start,
    required this.end,
    required this.fullMark,
    this.month,
    this.score,
    this.isPassed,
    this.percentage,
  });

  final String id;
  final ActivityExamKind kind;
  final String subject;
  final DateTime date;
  final int start;
  final int end;
  final double fullMark;
  final int? month;
  final double? score;
  final bool? isPassed;
  final double? percentage;

  bool get hasResult => score != null;

  String? get monthName =>
      month != null && month! >= 1 && month! <= 12 ? (AppLanguage.instance.isEnglish ? _monthsEn : _months)[month! - 1] : null;

  /// Score as a percentage of the full mark
  double? get percent => !hasResult ? null : (percentage ?? (fullMark > 0 ? score! * 100 / fullMark : 0));

  bool isUpcoming(DateTime now) {
    final today = DateTime(now.year, now.month, now.day);
    if (date.isAfter(today)) return true;
    if (date.isBefore(today)) return false;
    return now.hour * 60 + now.minute < end;
  }

  factory ActivityExam.fromJson(Map<String, dynamic> json) {
    final subject = _pick(json, 'subjectNameAr', 'subjectNameEn');
    return ActivityExam(
      id: '${json['id'] ?? ''}',
      kind: ActivityExamKind.from(json['examType']),
      subject: subject.isNotEmpty ? subject : tr('مادة', 'Subject'),
      date: _day(json['examDate']),
      start: _minutes(json['startTime']),
      end: _minutes(json['endTime']),
      fullMark: _num(json['weight']) ?? 0,
      month: (json['month'] as num?)?.toInt(),
      score: _num(json['score']),
      isPassed: json['isPassed'] as bool?,
      percentage: _num(json['percentage']),
    );
  }
}

// ---------------- Service ----------------

/// A call failed; [message] is ready to show
class ActivityException implements Exception {
  const ActivityException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class StudentActivityService {
  StudentActivityService._();
  static final instance = StudentActivityService._();

  /// Profile, class timetable and teachers (academic year / semester chosen in the app)
  Future<StudentActivityInfo> getInfo(String studentId) async {
    final data = await _get(AcademicYearController.instance.withYear(_uri('/api/teacher/my-student/${Uri.encodeComponent(studentId)}')));
    if (data is! Map) throw ActivityException(_failed);
    return StudentActivityInfo.fromJson(Map<String, dynamic>.from(data));
  }

  Future<List<ActivityDay>> getAttendance(String studentId, DateTime from, DateTime to) async {
    final data = await _get(_uri('/api/daily-attendance/my-student/${Uri.encodeComponent(studentId)}',
        {'from': activityDate(from), 'to': activityDate(to)}));
    return data is List ? data.whereType<Map<String, dynamic>>().map(ActivityDay.fromJson).toList() : const [];
  }

  Future<List<ActivityReview>> getReviews(String studentId, DateTime from, DateTime to) async {
    final data = await _get(_uri('/api/student-evaluation/my-student/${Uri.encodeComponent(studentId)}',
        {'from': activityDate(from), 'to': activityDate(to)}));
    return data is List ? data.whereType<Map<String, dynamic>>().map(ActivityReview.fromJson).toList() : const [];
  }

  /// Exams of the academic year / semester chosen in the app, with the student's results
  Future<List<ActivityExam>> getExams(String studentId) async {
    final data = await _get(AcademicYearController.instance.withYear(_uri('/api/exam/my-student/${Uri.encodeComponent(studentId)}')));
    return data is List ? data.whereType<Map<String, dynamic>>().map(ActivityExam.fromJson).toList() : const [];
  }

  static Uri _uri(String path, [Map<String, String>? query]) {
    final uri = Uri.parse('${AppConfig.apiBaseUrl}$path');
    return query == null ? uri : uri.replace(queryParameters: query);
  }

  static String get _failed => tr('تعذر تحميل بيانات الطالب.', "Could not load the student's data.");

  /// GET with the teacher's token; returns `data` of the ApiResponse
  Future<Object?> _get(Uri uri) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw ActivityException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'),
          sessionExpired: true);
    }
    final http.Response response;
    try {
      response = await http
          .get(uri, headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'})
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ActivityException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ActivityException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    if (response.statusCode == 401) {
      throw ActivityException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'),
          sessionExpired: true);
    }
    if (response.statusCode == 404) {
      throw ActivityException(tr('لا تُدرّس هذا الطالب.', "You don't teach this student."));
    }
    if (response.statusCode == 503) {
      throw ActivityException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
    }
    if (response.statusCode < 200 || response.statusCode >= 300) throw ActivityException(_failed);
    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      return body is Map ? body['data'] : null;
    } catch (_) {
      throw ActivityException(_failed);
    }
  }
}
