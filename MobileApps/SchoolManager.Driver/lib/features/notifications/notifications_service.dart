import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart' show Color, IconData, Icons;
import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// Kind of a notification (NotificationTypeEnum on the API)
enum NoticeType {
  homework(1, 'واجب', 'Homework', Icons.menu_book_rounded, Color(0xFF14916A)),
  exam(2, 'امتحان', 'Exam', Icons.edit_note_rounded, Color(0xFFF08C00)),
  examResult(3, 'نتيجة امتحان', 'Exam result', Icons.emoji_events_rounded, Color(0xFF13864B)),
  event(4, 'فعالية', 'Event', Icons.celebration_rounded, Color(0xFFE0457B)),
  message(5, 'رسالة', 'Message', Icons.campaign_rounded, Color(0xFF1677E8));

  const NoticeType(this.value, this._ar, this._en, this.icon, this.color);

  final int value;
  final String _ar;
  final String _en;
  final IconData icon;
  final Color color;

  String get label => tr(_ar, _en);

  static NoticeType from(Object? value) {
    final v = value is num ? value.toInt() : int.tryParse('$value');
    return NoticeType.values.firstWhere((t) => t.value == v || t.name.toLowerCase() == '$value'.toLowerCase(),
        orElse: () => NoticeType.message);
  }
}

/// A notification sent by the school (GET /api/notification/my-driver)
class AppNotice {
  const AppNotice({
    required this.id,
    required this.title,
    required this.description,
    required this.sentOn,
    required this.type,
    this.imageUrl,
    this.forEveryone = false,
  });

  final String id;
  final String title;
  final String description;

  /// Day it was sent (no time)
  final DateTime sentOn;
  final NoticeType type;
  final String? imageUrl;

  /// For the whole school (else for this driver's bus)
  final bool forEveryone;

  /// Sent today
  bool isToday(DateTime now) => sentOn == DateTime(now.year, now.month, now.day);

  factory AppNotice.fromJson(Map<String, dynamic> json) {
    final d = DateTime.tryParse('${json['sentOn']}') ?? DateTime.now();
    final image = (json['imageUrl'] as String?)?.trim();
    return AppNotice(
      id: '${json['id'] ?? ''}',
      title: (json['title'] as String?)?.trim() ?? '',
      description: (json['description'] as String?)?.trim() ?? '',
      sentOn: DateTime(d.year, d.month, d.day),
      type: NoticeType.from(json['notificationType']),
      imageUrl: image == null || image.isEmpty ? null : image,
      forEveryone: json['forEveryone'] == true,
    );
  }
}

/// Loading failed; [message] is ready to show
class NoticesException implements Exception {
  const NoticesException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class NotificationsService {
  NotificationsService._();
  static final instance = NotificationsService._();

  /// Driver app: notifications for the whole school or for my bus (GET /api/notification/my-driver)
  Future<List<AppNotice>> getMine() => _get('/api/notification/my-driver');

  Future<List<AppNotice>> _get(String path) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw NoticesException(_sessionEnded, sessionExpired: true);

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}$path'),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw NoticesException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw NoticesException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw NoticesException(_sessionEnded, sessionExpired: true);
      case 503:
        throw NoticesException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
      default:
        throw NoticesException(tr('تعذر تحميل الإشعارات.', 'Could not load the notifications.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(AppNotice.fromJson).toList();
    } catch (_) {
      throw NoticesException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
