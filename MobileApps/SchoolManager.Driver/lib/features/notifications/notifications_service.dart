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
  message(5, 'رسالة', 'Message', Icons.campaign_rounded, Color(0xFF1677E8)),
  bus(6, 'الحافلة', 'Bus', Icons.directions_bus_rounded, Color(0xFF0F766E));

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
      imageUrl: image == null || image.isEmpty ? null : pictureUrl(image),
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

/// One page of notifications from the server, with how many there are of each type (for the filter chips)
class NoticePage {
  const NoticePage({required this.items, required this.total, required this.hasMore, required this.counts});

  final List<AppNotice> items;
  /// How many in all (of the type asked)
  final int total;
  final bool hasMore;
  /// How many of each type, whatever type was asked
  final Map<NoticeType, int> counts;

  factory NoticePage.fromJson(Map<String, dynamic> json) {
    final counts = <NoticeType, int>{};
    final raw = json['counts'];
    if (raw is Map) {
      raw.forEach((key, value) {
        final type = NoticeType.from(int.tryParse('$key'));
        if (value is num) counts[type] = (counts[type] ?? 0) + value.toInt();
      });
    }
    return NoticePage(
      items: (json['items'] as List? ?? const []).whereType<Map<String, dynamic>>().map(AppNotice.fromJson).toList(),
      total: (json['total'] as num?)?.toInt() ?? 0,
      hasMore: json['hasMore'] == true,
      counts: counts,
    );
  }
}

class NotificationsService {
  NotificationsService._();
  static final instance = NotificationsService._();

  /// Driver app: notifications for the whole school or for my bus (GET /api/notification/my-driver)
  Future<List<AppNotice>> getMine() => _get('/api/notification/my-driver');

  /// A page at a time: notifications for the whole school or for my bus
  Future<NoticePage> getMinePage({int page = 1, int pageSize = 30, NoticeType? type}) =>
      _page('/api/notification/my-driver', page, pageSize, type);

  Future<List<AppNotice>> _get(String path) async {
    final data = await _data(path);
    if (data is! List) throw NoticesException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    return data.whereType<Map<String, dynamic>>().map(AppNotice.fromJson).toList();
  }

  /// One page from the server: newest first, [pageSize] items, optionally only one [type]
  Future<NoticePage> _page(String path, int page, int pageSize, NoticeType? type) async {
    final query = 'page=$page&pageSize=$pageSize${type == null ? '' : '&type=${type.value}'}';
    final data = await _data('$path${path.contains('?') ? '&' : '?'}$query');
    if (data is! Map<String, dynamic>) {
      throw NoticesException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
    return NoticePage.fromJson(data);
  }

  Future<dynamic> _data(String path) async {
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
      if (data == null) throw const FormatException();
      return data;
    } catch (_) {
      throw NoticesException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}

/// Uploaded pictures (and the default ones) come as "/notification-files/…": loaded through the gateway.
/// A full URL (older records) is kept as it is.
String pictureUrl(String url) =>
    url.startsWith('http://') || url.startsWith('https://') ? url : '${AppConfig.apiBaseUrl}${url.startsWith('/') ? '' : '/'}$url';
