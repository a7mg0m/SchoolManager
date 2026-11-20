import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';
import '../homework/homework_service.dart';

/// One class (section) the teacher teaches, its grade and the subjects the teacher teaches there
class MyClass {
  const MyClass({required this.section, required this.gradeId, required this.gradeNameAr, required this.gradeNameEn, this.subjects = const []});

  final TeacherSection section;
  final String gradeId;
  final String gradeNameAr;
  final String gradeNameEn;
  final List<TeacherSubject> subjects;

  String get gradeName => pickLang(gradeNameAr, gradeNameEn);
  String get name => section.name.isEmpty ? tr('فصل', 'Class') : section.name;

  /// "الصف الأول - فصل أ"
  String get label => [gradeName, name].where((s) => s.isNotEmpty).join(' - ');
}

/// The teacher's classes from what they teach: one per section, with every subject they teach in it,
/// ordered by grade then class name
List<MyClass> myClassesFrom(TeacherClasses classes) {
  final byId = <String, MyClass>{};
  for (final subject in classes.subjects) {
    for (final section in subject.sections) {
      final subjects = [...?byId[section.id]?.subjects];
      if (!subjects.any((s) => s.subjectId == subject.subjectId)) subjects.add(subject);
      byId[section.id] = MyClass(
        section: section,
        gradeId: subject.gradeId,
        gradeNameAr: subject.gradeNameAr,
        gradeNameEn: subject.gradeNameEn,
        subjects: subjects,
      );
    }
  }
  return byId.values.toList()
    ..sort((a, b) {
      final g = a.gradeName.compareTo(b.gradeName);
      return g != 0 ? g : a.name.compareTo(b.name);
    });
}

/// A student of one of the teacher's classes (GET /api/teacher/my-students/{sectionId})
class ClassStudent {
  const ClassStudent({required this.id, required this.nameAr, required this.nameEn, this.gender = '', this.dateOfBirth});

  final String id;
  final String nameAr;
  final String nameEn;

  /// Male / Female
  final String gender;
  final DateTime? dateOfBirth;

  String get name => pickLang(nameAr, nameEn);
  bool get isFemale => gender.toLowerCase().startsWith('f');
  bool get isMale => gender.toLowerCase().startsWith('m');

  /// Age in full years on [now]
  int? ageOn(DateTime now) {
    final d = dateOfBirth;
    if (d == null) return null;
    var age = now.year - d.year;
    if (now.month < d.month || (now.month == d.month && now.day < d.day)) age--;
    return age < 0 ? null : age;
  }

  factory ClassStudent.fromJson(Map<String, dynamic> json) {
    final birth = DateTime.tryParse('${json['dateOfBirth']}');
    return ClassStudent(
      id: '${json['id'] ?? ''}',
      nameAr: (json['nameAr'] as String?)?.trim() ?? '',
      nameEn: (json['nameEn'] as String?)?.trim() ?? '',
      gender: (json['gender'] as String?)?.trim() ?? '',
      // 0001-01-01 means not entered
      dateOfBirth: birth == null || birth.year < 1900 ? null : DateTime(birth.year, birth.month, birth.day),
    );
  }
}

/// Loading failed; [message] is ready to show
class ClassesException implements Exception {
  const ClassesException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ClassesService {
  ClassesService._();
  static final instance = ClassesService._();

  /// The teacher's classes for the academic year / semester chosen in the app
  Future<List<MyClass>> getMyClasses() async {
    try {
      return myClassesFrom(await HomeworkService.instance.getMyClasses());
    } on HomeworkException catch (e) {
      throw ClassesException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<ClassStudent>> getStudents(String sectionId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw ClassesException(_sessionEnded, sessionExpired: true);

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/teacher/my-students/${Uri.encodeComponent(sectionId)}'),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ClassesException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ClassesException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw ClassesException(_sessionEnded, sessionExpired: true);
      case 403:
        throw ClassesException(tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'));
      case 404:
        throw ClassesException(tr('هذا الفصل ليس من فصولك.', 'This class is not one of yours.'));
      default:
        throw ClassesException(tr('تعذر تحميل الطلاب.', 'Could not load the students.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(ClassStudent.fromJson).toList();
    } catch (_) {
      throw ClassesException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
