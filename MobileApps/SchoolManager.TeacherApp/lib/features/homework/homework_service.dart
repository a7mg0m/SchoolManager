import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// A class (section) the teacher can publish homework to
class TeacherSection {
  const TeacherSection({required this.id, required this.nameAr, required this.nameEn});

  final String id;
  final String nameAr;
  final String nameEn;

  String get name => pickLang(nameAr, nameEn);

  factory TeacherSection.fromJson(Map<String, dynamic> json) => TeacherSection(
        id: '${json['sectionId'] ?? ''}',
        nameAr: (json['nameAr'] as String?) ?? '',
        nameEn: (json['nameEn'] as String?) ?? '',
      );
}

/// A subject the teacher teaches, with its grade and the grade's classes
class TeacherSubject {
  const TeacherSubject({
    required this.subjectId,
    required this.subjectNameAr,
    required this.subjectNameEn,
    required this.gradeId,
    required this.gradeNameAr,
    required this.gradeNameEn,
    this.sections = const [],
  });

  final String subjectId;
  final String subjectNameAr;
  final String subjectNameEn;
  final String gradeId;
  final String gradeNameAr;
  final String gradeNameEn;
  final List<TeacherSection> sections;

  String get subjectName => pickLang(subjectNameAr, subjectNameEn);
  String get gradeName => pickLang(gradeNameAr, gradeNameEn);

  /// "رياضيات - الصف الأول"
  String get label => [subjectName, gradeName].where((s) => s.isNotEmpty).join(' - ');

  factory TeacherSubject.fromJson(Map<String, dynamic> json) => TeacherSubject(
        subjectId: '${json['subjectId'] ?? ''}',
        subjectNameAr: (json['subjectNameAr'] as String?) ?? '',
        subjectNameEn: (json['subjectNameEn'] as String?) ?? '',
        gradeId: '${json['gradeId'] ?? ''}',
        gradeNameAr: (json['gradeNameAr'] as String?) ?? '',
        gradeNameEn: (json['gradeNameEn'] as String?) ?? '',
        sections: (json['sections'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(TeacherSection.fromJson)
            .toList(),
      );
}

/// What the teacher teaches (GET /api/teacher/my-classes)
class TeacherClasses {
  const TeacherClasses({this.subjects = const [], this.hasActiveSemester = true});

  final List<TeacherSubject> subjects;

  /// Homework needs a current academic year and semester
  final bool hasActiveSemester;

  factory TeacherClasses.fromJson(Map<String, dynamic> json) => TeacherClasses(
        subjects: (json['subjects'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(TeacherSubject.fromJson)
            .toList(),
        hasActiveSemester: json['academicYearId'] != null && json['semesterId'] != null,
      );
}

/// A question of a homework: header and optional items
class HomeworkQuestion {
  const HomeworkQuestion({required this.header, this.items = const []});

  final String header;
  final List<String> items;

  Map<String, dynamic> toJson() => {'header': header, 'items': items};

  factory HomeworkQuestion.fromJson(Map<String, dynamic> json) => HomeworkQuestion(
        header: (json['header'] as String?) ?? '',
        items: (json['items'] as List? ?? const []).map((e) => '$e').toList(),
      );
}

/// A class a homework was published to
class PublishedSection {
  const PublishedSection({required this.sectionId, required this.nameAr, required this.nameEn, required this.publishedOn});

  final String sectionId;
  final String nameAr;
  final String nameEn;
  final DateTime publishedOn;

  String get name => pickLang(nameAr, nameEn);

  factory PublishedSection.fromJson(Map<String, dynamic> json) => PublishedSection(
        sectionId: '${json['sectionId'] ?? ''}',
        nameAr: (json['nameAr'] as String?) ?? '',
        nameEn: (json['nameEn'] as String?) ?? '',
        publishedOn: DateTime.tryParse('${json['publishedOn']}')?.toLocal() ?? DateTime.now(),
      );
}

/// A homework of the signed-in teacher (GET /api/homework/my)
class TeacherHomework {
  const TeacherHomework({
    required this.id,
    required this.subjectId,
    required this.subjectNameAr,
    required this.subjectNameEn,
    required this.gradeId,
    required this.gradeNameAr,
    required this.gradeNameEn,
    required this.title,
    required this.description,
    required this.dueDate,
    this.questions = const [],
    this.publishedSections = const [],
  });

  final String id;
  final String subjectId;
  final String subjectNameAr;
  final String subjectNameEn;
  final String gradeId;
  final String gradeNameAr;
  final String gradeNameEn;
  final String title;
  final String description;
  final DateTime dueDate;
  final List<HomeworkQuestion> questions;
  final List<PublishedSection> publishedSections;

  String get subjectName => pickLang(subjectNameAr, subjectNameEn);
  String get gradeName => pickLang(gradeNameAr, gradeNameEn);
  bool get isPublished => publishedSections.isNotEmpty;

  /// Due date passed (the day after the due date)
  bool isOverdue(DateTime now) => DateTime(now.year, now.month, now.day).isAfter(dueDate);

  factory TeacherHomework.fromJson(Map<String, dynamic> json) {
    final due = DateTime.tryParse('${json['dueDate']}') ?? DateTime.now();
    return TeacherHomework(
      id: '${json['id'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      subjectNameAr: (json['subjectNameAr'] as String?) ?? '',
      subjectNameEn: (json['subjectNameEn'] as String?) ?? '',
      gradeId: '${json['gradeId'] ?? ''}',
      gradeNameAr: (json['gradeNameAr'] as String?) ?? '',
      gradeNameEn: (json['gradeNameEn'] as String?) ?? '',
      title: (json['title'] as String?) ?? '',
      description: (json['description'] as String?) ?? '',
      dueDate: DateTime(due.year, due.month, due.day),
      questions: (json['questions'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(HomeworkQuestion.fromJson)
          .toList(),
      publishedSections: (json['publishedSections'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(PublishedSection.fromJson)
          .toList(),
    );
  }
}

/// A homework call failed; [message] is ready to show
class HomeworkException implements Exception {
  const HomeworkException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class HomeworkService {
  HomeworkService._();
  static final instance = HomeworkService._();

  Future<TeacherClasses> getMyClasses() async {
    final data = await _send('GET', _withYear('/api/teacher/my-classes'), fallback: tr('تعذر تحميل فصولك.', 'Could not load your classes.'));
    return data is Map<String, dynamic> ? TeacherClasses.fromJson(data) : const TeacherClasses();
  }

  Future<List<TeacherHomework>> getMyHomeworks() async {
    final data = await _send('GET', _withYear('/api/homework/my'), fallback: tr('تعذر تحميل الواجبات.', 'Could not load the homework.'));
    return data is List ? data.whereType<Map<String, dynamic>>().map(TeacherHomework.fromJson).toList() : const [];
  }

  /// Returns the new homework id
  Future<String> createHomework({
    required String subjectId,
    required String gradeId,
    required String title,
    required String description,
    required DateTime dueDate,
    List<HomeworkQuestion> questions = const [],
  }) async {
    final body = await _send('POST', '/api/homework/my',
        body: {
          'subjectId': subjectId,
          'gradeId': gradeId,
          'title': title,
          'description': description,
          'dueDate': _date(dueDate),
          'questions': questions.map((q) => q.toJson()).toList(),
          // The academic year and semester chosen in the app (null: the server uses the current ones)
          'academicYearId': AcademicYearController.instance.selectedId,
          'semesterId': AcademicYearController.instance.selectedSemesterId,
        },
        fallback: tr('تعذر حفظ الواجب.', 'Could not save the homework.'),
        wholeBody: true);
    return '${(body as Map<String, dynamic>?)?['insertedId'] ?? ''}';
  }

  /// Saves the edited title, description, due date and questions (the questions replace the saved ones)
  Future<void> updateHomework(
    String homeworkId, {
    required String title,
    required String description,
    required DateTime dueDate,
    List<HomeworkQuestion> questions = const [],
  }) async {
    await _send('PUT', '/api/homework/my/${Uri.encodeComponent(homeworkId)}',
        body: {
          'title': title,
          'description': description,
          'dueDate': _date(dueDate),
          'questions': questions.map((q) => q.toJson()).toList(),
        },
        fallback: tr('تعذر حفظ التعديلات.', 'Could not save the changes.'));
  }

  /// Returns how many classes it was newly published to
  Future<int> publish(String homeworkId, List<String> sectionIds) async {
    final data = await _send('POST', '/api/homework/my/${Uri.encodeComponent(homeworkId)}/publish',
        body: {'sectionIds': sectionIds}, fallback: tr('تعذر نشر الواجب.', 'Could not publish the homework.'));
    return data is num ? data.toInt() : 0;
  }

  Future<void> delete(String homeworkId) async {
    await _send('DELETE', '/api/homework/my/${Uri.encodeComponent(homeworkId)}',
        fallback: tr('تعذر حذف الواجب.', 'Could not delete the homework.'));
  }

  /// [path] with ?academicYearId= of the year chosen in the app
  static String _withYear(String path) {
    final uri = AcademicYearController.instance.withYear(Uri.parse(path));
    return uri.hasQuery ? '${uri.path}?${uri.query}' : uri.path;
  }

  static String _date(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  /// Calls the API with the teacher's token; returns `data` (or the whole body)
  Future<Object?> _send(String method, String path,
      {Map<String, Object?>? body, required String fallback, bool wholeBody = false}) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw HomeworkException(_sessionEnded, sessionExpired: true);
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
      throw HomeworkException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw HomeworkException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    Map<String, dynamic>? decoded;
    try {
      final json = jsonDecode(utf8.decode(response.bodyBytes));
      if (json is Map<String, dynamic>) decoded = json;
    } catch (_) {}

    if (response.statusCode == 401) throw HomeworkException(_sessionEnded, sessionExpired: true);
    if (response.statusCode == 503) {
      throw HomeworkException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw HomeworkException(_errorText('${decoded?['message'] ?? ''}') ?? fallback);
    }
    return wholeBody ? decoded : decoded?['data'];
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');

  /// API error keys (HomeworksController)
  static String? _errorText(String key) => switch (key) {
        'NotATeacher' => tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'),
        'TitleRequired' => tr('من فضلك أدخل عنوان الواجب.', 'Please enter the homework title.'),
        'TitleTooLong' => tr('العنوان طويل جداً.', 'The title is too long.'),
        'DescriptionTooLong' => tr('الوصف طويل جداً.', 'The description is too long.'),
        'DueDateInPast' => tr('موعد التسليم لا يمكن أن يكون في الماضي.', 'The due date cannot be in the past.'),
        'TooManyQuestions' => tr('عدد الأسئلة كبير جداً.', 'Too many questions.'),
        'TooManySubQuestions' => tr('عدد الأسئلة الفرعية كبير جداً.', 'Too many sub-questions.'),
        'QuestionTooLong' => tr('أحد الأسئلة طويل جداً.', 'One of the questions is too long.'),
        'SubjectNotTaught' => tr('هذه المادة ليست من موادك.', 'You do not teach this subject.'),
        'NoActiveSemester' => tr('لا يوجد فصل دراسي حالي، تواصل مع الإدارة.', 'There is no current semester. Please contact the school office.'),
        'NoSections' => tr('اختر فصلاً واحداً على الأقل.', 'Choose at least one class.'),
        'SectionNotTaught' => tr('أحد الفصول المختارة ليس من فصولك.', 'One of the chosen classes is not yours.'),
        'HomeworkNotFound' => tr('الواجب غير موجود.', 'The homework was not found.'),
        _ => null,
      };
}
