import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// One lesson of the teacher: when, which subject, which class
class TeacherLesson {
  const TeacherLesson({
    required this.id,
    required this.start,
    required this.end,
    required this.subjectNameAr,
    required this.subjectNameEn,
    this.nameAr = '',
    this.nameEn = '',
    this.gradeNameAr = '',
    this.gradeNameEn = '',
    this.sectionNameAr = '',
    this.sectionNameEn = '',
  });

  final String id;

  /// Minutes since midnight
  final int start;
  final int end;
  final String nameAr;
  final String nameEn;
  final String subjectNameAr;
  final String subjectNameEn;
  final String gradeNameAr;
  final String gradeNameEn;
  final String sectionNameAr;
  final String sectionNameEn;

  String get subject {
    final s = pickLang(subjectNameAr, subjectNameEn);
    return s.isNotEmpty ? s : pickLang(nameAr, nameEn);
  }

  /// "الصف الأول · فصل أ"
  String get classLabel =>
      [pickLang(gradeNameAr, gradeNameEn), pickLang(sectionNameAr, sectionNameEn)].where((s) => s.isNotEmpty).join(' · ');

  factory TeacherLesson.fromJson(Map<String, dynamic> json) => TeacherLesson(
        id: '${json['id'] ?? ''}',
        start: timeToMinutes(json['startTime']),
        end: timeToMinutes(json['endTime']),
        nameAr: (json['nameAr'] as String?) ?? '',
        nameEn: (json['nameEn'] as String?) ?? '',
        subjectNameAr: (json['subjectNameAr'] as String?) ?? '',
        subjectNameEn: (json['subjectNameEn'] as String?) ?? '',
        gradeNameAr: (json['gradeNameAr'] as String?) ?? '',
        gradeNameEn: (json['gradeNameEn'] as String?) ?? '',
        sectionNameAr: (json['sectionNameAr'] as String?) ?? '',
        sectionNameEn: (json['sectionNameEn'] as String?) ?? '',
      );
}

/// Lessons of one day of the week
class TeacherTimetableDay {
  const TeacherTimetableDay({required this.dayOfWeek, required this.lessons});

  /// 0 = Sunday … 6 = Saturday
  final int dayOfWeek;
  final List<TeacherLesson> lessons;

  factory TeacherTimetableDay.fromJson(Map<String, dynamic> json) => TeacherTimetableDay(
        dayOfWeek: dayOfWeekIndex(json['dayOfWeek']),
        lessons: (json['slots'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(TeacherLesson.fromJson)
            .toList()
          ..sort((a, b) => a.start.compareTo(b.start)),
      );
}

/// The teacher's week (GET /api/teacher/my-timetable)
class TeacherTimetable {
  const TeacherTimetable({required this.today, required this.now, required this.days});

  /// School day (0 = Sunday … 6 = Saturday) and time (minutes) when it was loaded
  final int today;
  final int now;

  /// Days that have lessons, Saturday first
  final List<TeacherTimetableDay> days;

  TeacherTimetableDay? dayOf(int dayOfWeek) => days.where((d) => d.dayOfWeek == dayOfWeek).firstOrNull;

  int get lessonsCount => days.fold(0, (sum, d) => sum + d.lessons.length);

  factory TeacherTimetable.fromJson(Map<String, dynamic> json) => TeacherTimetable(
        today: dayOfWeekIndex(json['today']),
        now: timeToMinutes(json['now']),
        days: (json['days'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(TeacherTimetableDay.fromJson)
            .where((d) => d.lessons.isNotEmpty)
            .toList(),
      );
}

/// "08:30:00" -> 510
int timeToMinutes(Object? value) {
  final parts = '$value'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}

/// 0..6 or "Sunday".."Saturday" -> 0 = Sunday … 6 = Saturday
int dayOfWeekIndex(Object? value) {
  if (value is num) return value.toInt() % 7;
  const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
  final i = names.indexOf('$value'.toLowerCase());
  return i < 0 ? 0 : i;
}

/// Loading the timetable failed; [message] is ready to show
class TeacherTimetableException implements Exception {
  const TeacherTimetableException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class TeacherTimetableService {
  TeacherTimetableService._();
  static final instance = TeacherTimetableService._();

  /// For the academic year / semester chosen in the app
  Future<TeacherTimetable> getMyTimetable() async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw TeacherTimetableException(_sessionEnded, sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        AcademicYearController.instance.withYear(Uri.parse('${AppConfig.apiBaseUrl}/api/teacher/my-timetable')),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw TeacherTimetableException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw TeacherTimetableException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw TeacherTimetableException(_sessionEnded, sessionExpired: true);
      case 403:
        throw TeacherTimetableException(tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'));
      default:
        throw TeacherTimetableException(tr('تعذر تحميل الجدول.', 'Could not load the timetable.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! Map<String, dynamic>) throw const FormatException();
      return TeacherTimetable.fromJson(data);
    } catch (_) {
      throw TeacherTimetableException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
