import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';

/// Who an event is for (Notification EventForEnum)
enum EventAudience {
  all(1, 'لجميع الطلاب'),
  stage(2, 'لمرحلة الطالب'),
  grade(3, 'لصف الطالب'),
  section(4, 'لفصل الطالب'),
  teacher(5, 'للمعلمين'),
  student(6, 'للطالب خاصة');

  const EventAudience(this.value, this.label);

  final int value;
  final String label;

  static EventAudience from(Object? value) {
    final v = value is num ? value.toInt() : int.tryParse('$value');
    return EventAudience.values.firstWhere((a) => a.value == v, orElse: () => EventAudience.all);
  }
}

/// A school event that concerns the child (GET /api/school-event/my-child/{studentId})
class ChildEvent {
  const ChildEvent({
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

  factory ChildEvent.fromJson(Map<String, dynamic> json) {
    final date = DateTime.tryParse('${json['eventDate']}') ?? DateTime.now();
    final image = (json['imageUrl'] as String?)?.trim();
    return ChildEvent(
      id: '${json['id'] ?? ''}',
      title: (json['title'] as String?)?.trim() ?? '',
      description: (json['description'] as String?)?.trim() ?? '',
      date: DateTime(date.year, date.month, date.day),
      start: _minutes(json['startTime']),
      end: _minutes(json['endTime']),
      audience: EventAudience.from(json['eventFor']),
      imageUrl: image == null || image.isEmpty ? null : image,
    );
  }
}

/// Loading the events failed; [message] is ready to show (Arabic)
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

  Future<List<ChildEvent>> getChildEvents(String studentId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw const EventsException('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/school-event/my-child/${Uri.encodeComponent(studentId)}'),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw const EventsException('انتهت مهلة الاتصال بالخادم.');
    } catch (_) {
      throw const EventsException('تعذر الاتصال بالخادم.');
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw const EventsException('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', sessionExpired: true);
      case 503:
        throw const EventsException('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.');
      default:
        throw const EventsException('تعذر تحميل الفعاليات.');
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(ChildEvent.fromJson).toList();
    } catch (_) {
      throw const EventsException('استجابة غير متوقعة من الخادم.');
    }
  }
}

/// "08:30:00" -> 510
int _minutes(Object? value) {
  final parts = '$value'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}
