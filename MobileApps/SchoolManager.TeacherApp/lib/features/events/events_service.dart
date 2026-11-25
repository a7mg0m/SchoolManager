import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/academic_year/academic_year_controller.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// Who an event is for (Notification EventForEnum)
enum EventAudience {
  all(1, 'للجميع', 'Everyone'),
  stage(2, 'لمرحلة الطالب', "Student's stage"),
  grade(3, 'لصف الطالب', "Student's grade"),
  section(4, 'لفصل الطالب', "Student's class"),
  teacher(5, 'للمعلمين', 'Teachers'),
  student(6, 'للطالب خاصة', 'This student only');

  const EventAudience(this.value, this._labelAr, this._labelEn);

  final int value;
  final String _labelAr;
  final String _labelEn;

  String get label => tr(_labelAr, _labelEn);

  static EventAudience from(Object? value) {
    final v = value is num ? value.toInt() : int.tryParse('$value');
    return EventAudience.values.firstWhere((a) => a.value == v, orElse: () => EventAudience.all);
  }
}

/// A school event for the teacher: for everyone or for teachers (GET /api/school-event/my)
class SchoolEvent {
  const SchoolEvent({
    required this.id,
    required this.title,
    required this.description,
    required this.date,
    required this.start,
    required this.end,
    required this.audience,
    this.imageUrl,
  });

  final String id;
  final String title;
  final String description;

  /// Day of the event (no time)
  final DateTime date;

  /// Minutes since midnight
  final int start;
  final int end;
  final EventAudience audience;
  final String? imageUrl;

  /// Not over yet (a later day, or today before it ends)
  bool isUpcoming(DateTime now) {
    final today = DateTime(now.year, now.month, now.day);
    if (date.isAfter(today)) return true;
    if (date.isBefore(today)) return false;
    return now.hour * 60 + now.minute < end;
  }

  /// Happening right now (today, between start and end)
  bool isNow(DateTime now) {
    final today = DateTime(now.year, now.month, now.day);
    final minutes = now.hour * 60 + now.minute;
    return date == today && minutes >= start && minutes < end;
  }

  factory SchoolEvent.fromJson(Map<String, dynamic> json) {
    final date = DateTime.tryParse('${json['eventDate']}') ?? DateTime.now();
    final image = (json['imageUrl'] as String?)?.trim();
    return SchoolEvent(
      id: '${json['id'] ?? ''}',
      title: (json['title'] as String?)?.trim() ?? '',
      description: (json['description'] as String?)?.trim() ?? '',
      date: DateTime(date.year, date.month, date.day),
      start: _minutes(json['startTime']),
      end: _minutes(json['endTime']),
      audience: EventAudience.from(json['eventFor']),
      imageUrl: image == null || image.isEmpty ? null : pictureUrl(image),
    );
  }
}

/// Loading the events failed; [message] is ready to show
class EventsException implements Exception {
  const EventsException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class EventsService {
  EventsService._();
  static final instance = EventsService._();

  Future<List<SchoolEvent>> getMyEvents() async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw EventsException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        AcademicYearController.instance.withYear(
            Uri.parse('${AppConfig.apiBaseUrl}/api/school-event/my')),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw EventsException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw EventsException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw EventsException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
      case 403:
        throw EventsException(tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'));
      case 503:
        throw EventsException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
      default:
        throw EventsException(tr('تعذر تحميل الفعاليات.', 'Could not load the events.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(SchoolEvent.fromJson).toList();
    } catch (_) {
      throw EventsException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }
}

/// "08:30:00" -> 510
int _minutes(Object? value) {
  final parts = '$value'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}

/// Uploaded pictures (and the default ones) come as "/notification-files/…": loaded through the gateway.
/// A full URL (older records) is kept as it is.
String pictureUrl(String url) =>
    url.startsWith('http://') || url.startsWith('https://') ? url : '${AppConfig.apiBaseUrl}${url.startsWith('/') ? '' : '/'}$url';
