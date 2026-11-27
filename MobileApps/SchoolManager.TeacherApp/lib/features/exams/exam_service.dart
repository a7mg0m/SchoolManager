import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// The exam types of the teacher app: quizzes and monthly exams only (ExamType on the API)
enum TeacherExamType {
  quiz(1),
  monthly(2);

  const TeacherExamType(this.value);
  final int value;

  /// The types a teacher can add
  static const creatable = [TeacherExamType.quiz, TeacherExamType.monthly];

  /// A quiz or monthly exam (other types, e.g. mid-term / final, are not shown)
  static bool isShown(Object? value) => TeacherExamType.values.any((t) => '${t.value}' == '$value' || t.name == '$value'.toLowerCase());

  String get label => switch (this) {
        TeacherExamType.quiz => tr('اختبار قصير', 'Quiz'),
        TeacherExamType.monthly => tr('امتحان شهري', 'Monthly exam'),
      };

  static TeacherExamType from(Object? value) =>
      TeacherExamType.values.firstWhere((t) => '${t.value}' == '$value' || t.name == '$value'.toLowerCase(),
          orElse: () => TeacherExamType.quiz);
}

/// Month name 1..12
String examMonthName(int month) {
  const ar = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
  const en = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  return (AppLanguage.instance.isEnglish ? en : ar)[(month - 1).clamp(0, 11)];
}

/// Minutes since midnight from "HH:mm[:ss]"
int _minutes(Object? value) {
  final parts = '${value ?? ''}'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}

/// "HH:mm:ss" from minutes since midnight
String examTime(int minutes) =>
    '${(minutes ~/ 60).toString().padLeft(2, '0')}:${(minutes % 60).toString().padLeft(2, '0')}:00';

/// "09:30" / "9:30 ص"
String examClock(int minutes) {
  final h = minutes ~/ 60, m = (minutes % 60).toString().padLeft(2, '0');
  if (AppLanguage.instance.isEnglish) return '${h % 12 == 0 ? 12 : h % 12}:$m ${h < 12 ? 'AM' : 'PM'}';
  return '${h % 12 == 0 ? 12 : h % 12}:$m ${h < 12 ? 'ص' : 'م'}';
}

/// "15" / "12.5"
String examMark(num value) => value.toStringAsFixed(2).replaceFirst(RegExp(r'\.?0+$'), '');

/// An exam of the teacher's subjects and classes (GET /api/exam/my): one they added, or one by the school office /
/// another teacher (read-only; scores can still be entered)
class TeacherExam {
  const TeacherExam({
    required this.id,
    required this.subjectId,
    required this.subjectNameAr,
    required this.subjectNameEn,
    required this.gradeId,
    required this.gradeNameAr,
    required this.gradeNameEn,
    required this.sectionId,
    required this.sectionNameAr,
    required this.sectionNameEn,
    required this.type,
    this.month,
    required this.date,
    required this.startMinutes,
    required this.endMinutes,
    required this.weight,
    required this.successScore,
    this.resultCount = 0,
    this.averageScore,
    this.answerModelUrl,
    this.isMine = true,
    this.addedByTeacher = true,
  });

  final String id;

  /// The logged-in teacher added it (only then it can be edited / deleted)
  final bool isMine;

  /// Added by a teacher (another one when not [isMine]) or by the school office
  final bool addedByTeacher;
  final String subjectId;
  final String subjectNameAr;
  final String subjectNameEn;
  final String gradeId;
  final String gradeNameAr;
  final String gradeNameEn;
  final String sectionId;
  final String sectionNameAr;
  final String sectionNameEn;
  final TeacherExamType type;
  final int? month;
  final DateTime date;
  final int startMinutes;
  final int endMinutes;

  /// Full mark
  final double weight;
  final double successScore;

  /// Students with a score
  final int resultCount;
  final double? averageScore;

  /// The answer model (file path or link) set by the school; null when none
  final String? answerModelUrl;

  String get subjectName => pickLang(subjectNameAr, subjectNameEn);
  String get gradeName => pickLang(gradeNameAr, gradeNameEn);
  String get sectionName => pickLang(sectionNameAr, sectionNameEn);

  /// An exam for the whole grade (no class)
  bool get isWholeGrade => sectionId.isEmpty;

  /// The class, or "all classes" for a whole-grade exam
  String get classText => isWholeGrade ? tr('كل الفصول', 'All classes') : sectionName;

  /// Who added an exam that is not the teacher's own
  String get addedByText => addedByTeacher ? tr('أضافه معلم آخر', 'By another teacher') : tr('من إدارة المدرسة', 'By the school');

  /// "اختبار قصير" / "امتحان شهر نوفمبر"
  String get title => type == TeacherExamType.monthly && month != null
      ? tr('امتحان شهر ${examMonthName(month!)}', '${examMonthName(month!)} monthly exam')
      : type.label;

  bool isUpcoming(DateTime now) => !DateTime(date.year, date.month, date.day).isBefore(DateTime(now.year, now.month, now.day));

  factory TeacherExam.fromJson(Map<String, dynamic> json) {
    final d = DateTime.tryParse('${json['examDate']}') ?? DateTime.now();
    return TeacherExam(
      id: '${json['id'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      subjectNameAr: (json['subjectNameAr'] as String?) ?? '',
      subjectNameEn: (json['subjectNameEn'] as String?) ?? '',
      gradeId: '${json['gradeId'] ?? ''}',
      gradeNameAr: (json['gradeNameAr'] as String?) ?? '',
      gradeNameEn: (json['gradeNameEn'] as String?) ?? '',
      sectionId: json['sectionId'] == null ? '' : '${json['sectionId']}',
      sectionNameAr: (json['sectionNameAr'] as String?) ?? '',
      sectionNameEn: (json['sectionNameEn'] as String?) ?? '',
      type: TeacherExamType.from(json['examType']),
      month: (json['month'] as num?)?.toInt(),
      date: DateTime(d.year, d.month, d.day),
      startMinutes: _minutes(json['startTime']),
      endMinutes: _minutes(json['endTime']),
      weight: (json['weight'] as num?)?.toDouble() ?? 0,
      successScore: (json['successScore'] as num?)?.toDouble() ?? 0,
      resultCount: (json['resultCount'] as num?)?.toInt() ?? 0,
      averageScore: (json['averageScore'] as num?)?.toDouble(),
      answerModelUrl: (json['answerModelUrl'] as String?)?.trim().isNotEmpty == true ? (json['answerModelUrl'] as String).trim() : null,
      isMine: json['isMine'] != false,
      addedByTeacher: json['addedByTeacher'] != false,
    );
  }
}

/// What the teacher enters in the exam form
class ExamInput {
  const ExamInput({
    required this.subjectId,
    required this.gradeId,
    required this.sectionId,
    required this.type,
    this.month,
    required this.date,
    required this.startMinutes,
    required this.endMinutes,
    required this.weight,
    required this.successScore,
  });

  final String subjectId;
  final String gradeId;
  final String sectionId;
  final TeacherExamType type;
  final int? month;
  final DateTime date;
  final int startMinutes;
  final int endMinutes;
  final double weight;
  final double successScore;

  Map<String, Object?> toJson() => {
        'subjectId': subjectId,
        'gradeId': gradeId,
        'sectionId': sectionId,
        'examType': type.value,
        'month': type == TeacherExamType.monthly ? month : null,
        'examDate': '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}',
        'startTime': examTime(startMinutes),
        'endTime': examTime(endMinutes),
        'weight': weight,
        'successScore': successScore,
      };
}

/// A student of the exam's class and their score (null = none yet)
class ExamStudentScore {
  const ExamStudentScore({required this.studentId, required this.nameAr, required this.nameEn, this.score});

  final String studentId;
  final String nameAr;
  final String nameEn;
  final double? score;

  String get name => pickLang(nameAr, nameEn);

  factory ExamStudentScore.fromJson(Map<String, dynamic> json) => ExamStudentScore(
        studentId: '${json['studentId'] ?? ''}',
        nameAr: (json['nameAr'] as String?) ?? '',
        nameEn: (json['nameEn'] as String?) ?? '',
        score: (json['score'] as num?)?.toDouble(),
      );
}

/// GET /api/exam/my/{id}/scores
class ExamScores {
  const ExamScores({required this.weight, required this.successScore, this.students = const []});

  final double weight;
  final double successScore;
  final List<ExamStudentScore> students;

  factory ExamScores.fromJson(Map<String, dynamic> json) => ExamScores(
        weight: (json['weight'] as num?)?.toDouble() ?? 0,
        successScore: (json['successScore'] as num?)?.toDouble() ?? 0,
        students: (json['students'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(ExamStudentScore.fromJson)
            .toList(),
      );
}

/// An exam call failed; [message] is ready to show
class ExamException implements Exception {
  const ExamException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ExamService {
  ExamService._();
  static final instance = ExamService._();

  Future<List<TeacherExam>> getMyExams() async {
    final data = await _send('GET', _withYear('/api/exam/my'), fallback: tr('تعذر تحميل الاختبارات.', 'Could not load the exams.'));
    // Quizzes and monthly exams only
    return data is List
        ? data.whereType<Map<String, dynamic>>().where((e) => TeacherExamType.isShown(e['examType'])).map(TeacherExam.fromJson).toList()
        : const [];
  }

  /// Returns the new exam id
  Future<String> createExam(ExamInput input) async {
    final body = await _send('POST', '/api/exam/my',
        body: {
          ...input.toJson(),
          // The academic year and semester chosen in the app (null: the server uses the current ones)
          'academicYearId': AcademicYearController.instance.selectedId,
          'semesterId': AcademicYearController.instance.selectedSemesterId,
        },
        fallback: tr('تعذر حفظ الاختبار.', 'Could not save the exam.'),
        wholeBody: true);
    return '${(body as Map<String, dynamic>?)?['insertedId'] ?? ''}';
  }

  Future<void> updateExam(String examId, ExamInput input) async {
    await _send('PUT', '/api/exam/my/${Uri.encodeComponent(examId)}',
        body: input.toJson(), fallback: tr('تعذر حفظ التعديلات.', 'Could not save the changes.'));
  }

  Future<void> deleteExam(String examId) async {
    await _send('DELETE', '/api/exam/my/${Uri.encodeComponent(examId)}',
        fallback: tr('تعذر حذف الاختبار.', 'Could not delete the exam.'));
  }

  Future<ExamScores> getScores(String examId) async {
    final data = await _send('GET', '/api/exam/my/${Uri.encodeComponent(examId)}/scores',
        fallback: tr('تعذر تحميل الطلاب.', 'Could not load the students.'));
    return data is Map<String, dynamic> ? ExamScores.fromJson(data) : const ExamScores(weight: 0, successScore: 0);
  }

  /// [scores]: student id -> score (null removes the student's score)
  Future<void> saveScores(String examId, Map<String, double?> scores) async {
    await _send('POST', '/api/exam/my/${Uri.encodeComponent(examId)}/scores',
        body: {
          'results': [
            for (final e in scores.entries) {'studentId': e.key, 'score': e.value},
          ],
        },
        fallback: tr('تعذر حفظ الدرجات.', 'Could not save the scores.'));
  }

  /// [path] with the academic year / semester chosen in the app
  static String _withYear(String path) {
    final uri = AcademicYearController.instance.withYear(Uri.parse(path));
    return uri.hasQuery ? '${uri.path}?${uri.query}' : uri.path;
  }

  /// Calls the API with the teacher's token; returns `data` (or the whole body)
  Future<Object?> _send(String method, String path,
      {Map<String, Object?>? body, required String fallback, bool wholeBody = false}) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw ExamException(_sessionEnded, sessionExpired: true);
    }

    final uri = Uri.parse('${AppConfig.apiBaseUrl}$path');
    final headers = {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ${session.token}',
    };

    final http.Response response;
    try {
      final request = switch (method) {
        'POST' => http.post(uri, headers: headers, body: jsonEncode(body ?? const {})),
        'PUT' => http.put(uri, headers: headers, body: jsonEncode(body ?? const {})),
        'DELETE' => http.delete(uri, headers: headers),
        _ => http.get(uri, headers: headers),
      };
      response = await request.timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ExamException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ExamException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    Map<String, dynamic>? decoded;
    try {
      final json = jsonDecode(utf8.decode(response.bodyBytes));
      if (json is Map<String, dynamic>) decoded = json;
    } catch (_) {}

    if (response.statusCode == 401) throw ExamException(_sessionEnded, sessionExpired: true);
    if (response.statusCode == 503) {
      throw ExamException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ExamException(_errorText('${decoded?['message'] ?? ''}') ?? fallback);
    }
    return wholeBody ? decoded : decoded?['data'];
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');

  /// API error keys (TeacherExamsController)
  static String? _errorText(String key) => switch (key) {
        'NotATeacher' => tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'),
        'ExamTypeNotAllowed' => tr('يمكنك إضافة اختبار قصير أو امتحان شهري فقط.', 'You can only add a quiz or a monthly exam.'),
        'SectionRequired' => tr('اختر الفصل.', 'Choose the class.'),
        'MonthRequired' => tr('اختر شهر الامتحان.', 'Choose the month of the exam.'),
        'MonthInvalid' => tr('الشهر غير صحيح.', 'The month is not valid.'),
        'EndTimeBeforeStart' => tr('وقت النهاية يجب أن يكون بعد وقت البداية.', 'The end time must be after the start time.'),
        'WeightInvalid' => tr('الدرجة الكاملة يجب أن تكون أكبر من صفر.', 'The full mark must be more than zero.'),
        'SuccessScoreInvalid' => tr('درجة النجاح يجب أن تكون بين صفر والدرجة الكاملة.', 'The pass mark must be between zero and the full mark.'),
        'WeightBelowExistingScores' => tr('توجد درجات أعلى من الدرجة الكاملة الجديدة.', 'Some scores are higher than the new full mark.'),
        'SectionHasScores' => tr('لا يمكن تغيير الفصل بعد رصد الدرجات.', 'The class cannot change after scores were entered.'),
        'SubjectNotTaught' => tr('هذه المادة ليست من موادك.', 'You do not teach this subject.'),
        'SectionNotTaught' => tr('هذا الفصل ليس من فصولك.', 'This class is not one of yours.'),
        'NoActiveSemester' => tr('لا يوجد فصل دراسي حالي، تواصل مع الإدارة.', 'There is no current semester. Please contact the school office.'),
        'ScoreInvalid' => tr('إحدى الدرجات خارج المدى المسموح.', 'One of the scores is out of range.'),
        'StudentNotInClass' => tr('أحد الطلاب ليس في هذا الفصل.', 'One of the students is not in this class.'),
        'DuplicateStudent' => tr('طالب مكرر في القائمة.', 'A student is listed twice.'),
        'ExamNotFound' => tr('الاختبار غير موجود.', 'The exam was not found.'),
        _ => null,
      };
}
