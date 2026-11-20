import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/academic_year/academic_year_controller.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

enum SlotStatus { finished, current, upcoming }

/// One lesson of the timetable
class ScheduleSlot {
  const ScheduleSlot({
    required this.id,
    required this.name,
    required this.start,
    required this.end,
    required this.subject,
    required this.teacher,
  });

  final String id;
  final String name;

  /// Minutes since midnight
  final int start;
  final int end;
  final String subject;
  final String teacher;

  /// Subject, or the slot's name when it has no subject
  String get title => subject.isNotEmpty ? subject : name;

  /// Status on [isToday] at [now] (minutes since midnight); other days are all upcoming
  SlotStatus statusAt(int now, {required bool isToday}) {
    if (!isToday) return SlotStatus.upcoming;
    if (end <= now) return SlotStatus.finished;
    if (start <= now) return SlotStatus.current;
    return SlotStatus.upcoming;
  }

  factory ScheduleSlot.fromJson(Map<String, dynamic> json) => ScheduleSlot(
        id: '${json['id'] ?? ''}',
        name: _pick(json, 'nameAr', 'nameEn'),
        start: _minutes(json['startTime']),
        end: _minutes(json['endTime']),
        subject: _pick(json, 'subjectNameAr', 'subjectNameEn'),
        teacher: _pick(json, 'teacherNameAr', 'teacherNameEn'),
      );
}

/// Lessons of one day of the week
class TimetableDay {
  const TimetableDay({required this.dayOfWeek, required this.slots});

  /// 0 = Sunday … 6 = Saturday
  final int dayOfWeek;
  final List<ScheduleSlot> slots;

  factory TimetableDay.fromJson(Map<String, dynamic> json) => TimetableDay(
        dayOfWeek: _dayOfWeek(json['dayOfWeek']),
        slots: (json['slots'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(ScheduleSlot.fromJson)
            .toList(),
      );
}

/// A child's whole week (GET /api/student/my-children/{id}/timetable)
class ChildTimetable {
  const ChildTimetable({required this.today, required this.now, required this.days});

  /// School day (0 = Sunday … 6 = Saturday) and time (minutes since midnight) when it was loaded
  final int today;
  final int now;

  /// Days that have lessons, Saturday first
  final List<TimetableDay> days;

  TimetableDay? dayOf(int dayOfWeek) => days.where((d) => d.dayOfWeek == dayOfWeek).firstOrNull;

  factory ChildTimetable.fromJson(Map<String, dynamic> json) => ChildTimetable(
        today: _dayOfWeek(json['today']),
        now: _minutes(json['now']),
        days: (json['days'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(TimetableDay.fromJson)
            .where((d) => d.slots.isNotEmpty)
            .toList(),
      );
}

/// Loading the timetable failed; [message] is ready to show (Arabic)
class ScheduleException implements Exception {
  const ScheduleException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ScheduleService {
  ScheduleService._();
  static final instance = ScheduleService._();

  Future<ChildTimetable> getChildTimetable(String studentId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw ScheduleException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        AcademicYearController.instance.withYear(
            Uri.parse('${AppConfig.apiBaseUrl}/api/student/my-children/${Uri.encodeComponent(studentId)}/timetable')),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ScheduleException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ScheduleException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    if (response.statusCode == 401) {
      throw ScheduleException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
    }
    if (response.statusCode != 200) {
      throw ScheduleException(tr('تعذر تحميل الجدول الدراسي.', 'Could not load the timetable.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! Map<String, dynamic>) throw const FormatException();
      return ChildTimetable.fromJson(data);
    } catch (_) {
      throw ScheduleException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }
}

String _pick(Map<String, dynamic> json, String ar, String en) {
  return pickLang(json[ar] as String?, json[en] as String?);
}

/// "08:30:00" -> 510
int _minutes(Object? value) {
  final parts = '$value'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}

/// .NET DayOfWeek as a number (default JSON) or a name ("Sunday")
int _dayOfWeek(Object? value) {
  if (value is num) return value.toInt() % 7;
  const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
  final index = names.indexOf('$value'.toLowerCase());
  return index < 0 ? 0 : index;
}
