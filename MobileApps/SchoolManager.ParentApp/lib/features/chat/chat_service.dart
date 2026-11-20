import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// Longest message the app lets you type
const chatMaxLength = 2000;

/// Someone you may chat with: a teacher sees their students' parents, a parent their children's teachers
class ChatContact {
  const ChatContact({required this.userId, required this.nameAr, required this.nameEn, this.detailAr = '', this.detailEn = ''});

  final String userId;
  final String nameAr;
  final String nameEn;

  /// Children ("ولي أمر سارة") or subjects ("رياضيات، علوم")
  final String detailAr;
  final String detailEn;

  String get name => pickLang(nameAr, nameEn);
  String get detail => pickLang(detailAr, detailEn);

  bool matches(String query) {
    final q = query.trim().toLowerCase();
    if (q.isEmpty) return true;
    return [nameAr, nameEn, detailAr, detailEn].any((s) => s.toLowerCase().contains(q));
  }

  factory ChatContact.fromJson(Map<String, dynamic> json) => ChatContact(
        userId: '${json['userId'] ?? ''}',
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        detailAr: (json['detailAr'] as String?)?.trim() ?? '',
        detailEn: (json['detailEn'] as String?)?.trim() ?? '',
      );
}

/// One of my chats (GET /api/chat/my)
class ChatSummary {
  const ChatSummary({
    required this.roomId,
    required this.otherUserId,
    required this.nameAr,
    required this.nameEn,
    this.detailAr = '',
    this.detailEn = '',
    this.lastMessage,
    this.lastMessageSenderId,
    this.lastMessageOn,
    this.unreadCount = 0,
  });

  final String roomId;
  final String otherUserId;
  final String nameAr;
  final String nameEn;
  final String detailAr;
  final String detailEn;
  final String? lastMessage;
  final String? lastMessageSenderId;
  final DateTime? lastMessageOn;
  final int unreadCount;

  String get name => pickLang(nameAr, nameEn);
  String get detail => pickLang(detailAr, detailEn);

  factory ChatSummary.fromJson(Map<String, dynamic> json) => ChatSummary(
        roomId: '${json['roomId'] ?? ''}',
        otherUserId: '${json['otherUserId'] ?? ''}',
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        detailAr: (json['detailAr'] as String?)?.trim() ?? '',
        detailEn: (json['detailEn'] as String?)?.trim() ?? '',
        lastMessage: json['lastMessage'] as String?,
        lastMessageSenderId: json['lastMessageSenderId'] == null ? null : '${json['lastMessageSenderId']}',
        lastMessageOn: chatTime(json['lastMessageOn']),
        unreadCount: (json['unreadCount'] as num?)?.toInt() ?? 0,
      );
}

/// Delivery of a message (MessageDeliveryStatus on the API)
enum ChatStatus { sending, sent, delivered, seen, failed }

/// A message of a chat (history / send)
class ChatMessage {
  const ChatMessage({required this.id, required this.senderId, required this.content, required this.sentOn, this.status = ChatStatus.sent});

  final String id;
  final String senderId;
  final String content;
  final DateTime sentOn;

  /// For my messages: how far the other person got; for theirs: what I did with it
  final ChatStatus status;

  ChatMessage withStatus(ChatStatus value) => ChatMessage(id: id, senderId: senderId, content: content, sentOn: sentOn, status: value);

  static ChatStatus _status(Object? value) => switch ('$value') {
        '2' || 'Delivered' => ChatStatus.delivered,
        '3' || 'Seen' => ChatStatus.seen,
        _ => ChatStatus.sent,
      };

  factory ChatMessage.fromJson(Map<String, dynamic> json) => ChatMessage(
        id: '${json['id'] ?? json['messageId'] ?? ''}',
        senderId: '${json['senderId'] ?? ''}',
        content: (json['content'] as String?) ?? '',
        sentOn: chatTime(json['sentOn']) ?? DateTime.now(),
        status: _status(json['status']),
      );
}

/// The API saves times in UTC without a zone: read them as UTC, show them local
DateTime? chatTime(Object? value) {
  final text = value?.toString();
  if (text == null || text.isEmpty) return null;
  final hasZone = text.endsWith('Z') || RegExp(r'[+-]\d\d:?\d\d$').hasMatch(text);
  return DateTime.tryParse(hasZone ? text : '${text}Z')?.toLocal();
}

/// A chat call failed; [message] is ready to show
class ChatException implements Exception {
  const ChatException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ChatService {
  ChatService._();
  static final instance = ChatService._();

  /// The signed-in user's id (their messages are theirs)
  String get myUserId => AuthService.instance.session?.userId ?? '';

  Future<List<ChatContact>> getContacts() async {
    final data = await _send('GET', '/api/chat/my/contacts', fallback: tr('تعذر تحميل جهات الاتصال.', 'Could not load the contacts.'));
    return data is List ? data.whereType<Map<String, dynamic>>().map(ChatContact.fromJson).toList() : const [];
  }

  /// Chats of the academic year chosen in the app
  Future<List<ChatSummary>> getChats() async {
    final data = await _send('GET', _withYear('/api/chat/my'), fallback: tr('تعذر تحميل المحادثات.', 'Could not load the chats.'));
    return data is List ? data.whereType<Map<String, dynamic>>().map(ChatSummary.fromJson).toList() : const [];
  }

  /// Opens (or creates) the chat with [otherUserId] in the academic year chosen in the app; returns its room id
  Future<String> startChat(String otherUserId) async {
    final yearId = AcademicYearController.instance.selectedId;
    if (yearId == null || yearId.isEmpty) {
      throw ChatException(tr('اختر العام الدراسي أولاً.', 'Choose the academic year first.'));
    }
    final data = await _send('POST', '/api/chat/my',
        body: {'otherUserId': otherUserId, 'academicYearId': yearId}, fallback: tr('تعذر بدء المحادثة.', 'Could not start the chat.'));
    return '${data ?? ''}';
  }

  /// Newest messages first (page 1 = the latest)
  Future<List<ChatMessage>> getMessages(String roomId, {int page = 1, int pageSize = 50}) async {
    final body = await _send('GET', '/api/chat/history/${Uri.encodeComponent(roomId)}?page=$page&pageSize=$pageSize',
        fallback: tr('تعذر تحميل الرسائل.', 'Could not load the messages.'), wholeBody: true);
    final list = body is Map<String, dynamic> ? body['messages'] : null;
    return list is List ? list.whereType<Map<String, dynamic>>().map(ChatMessage.fromJson).toList() : const [];
  }

  Future<ChatMessage> sendMessage(String roomId, String content) async {
    final body = await _send('POST', '/api/chat/send-message',
        body: {'roomId': roomId, 'content': content}, fallback: tr('تعذر إرسال الرسالة.', 'Could not send the message.'), wholeBody: true);
    if (body is! Map<String, dynamic>) throw ChatException(tr('تعذر إرسال الرسالة.', 'Could not send the message.'));
    return ChatMessage.fromJson(body);
  }

  /// Profile picture of [userId]; null when they have none (or it could not be loaded)
  Future<Uint8List?> getPhoto(String userId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired || userId.isEmpty) return null;
    try {
      final response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/user-profile/photo/${Uri.encodeComponent(userId)}'),
        headers: {'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
      final type = response.headers['content-type'] ?? '';
      return response.statusCode == 200 && type.startsWith('image/') && response.bodyBytes.isNotEmpty ? response.bodyBytes : null;
    } catch (_) {
      return null; // the initial is shown instead
    }
  }

  /// Tells the other person I have seen these messages
  Future<void> markSeen(String roomId, List<String> messageIds) async {
    if (messageIds.isEmpty) return;
    await _send('PUT', '/api/chat/history/${Uri.encodeComponent(roomId)}/status',
        body: {'messageIds': messageIds, 'status': 3}, fallback: '');
  }

  /// [path] with ?academicYearId= of the year chosen in the app
  static String _withYear(String path) {
    final uri = AcademicYearController.instance.withYear(Uri.parse(path));
    return uri.hasQuery ? '${uri.path}?${uri.query}' : uri.path;
  }

  /// Calls the API with the user's token; returns `data` of an ApiResponse, or the whole body
  Future<Object?> _send(String method, String path,
      {Map<String, Object?>? body, required String fallback, bool wholeBody = false}) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw ChatException(_sessionEnded, sessionExpired: true);

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
        _ => http.get(uri, headers: headers),
      };
      response = await request.timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ChatException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ChatException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    Object? decoded;
    try {
      decoded = jsonDecode(utf8.decode(response.bodyBytes));
    } catch (_) {}
    final message = decoded is Map<String, dynamic> ? '${decoded['message'] ?? decoded['title'] ?? ''}' : '';

    if (response.statusCode == 401) throw ChatException(_sessionEnded, sessionExpired: true);
    if (response.statusCode == 503) {
      throw ChatException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
    }
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ChatException(switch (message) {
        'NotAContact' => tr('لا يمكنك مراسلة هذا الشخص.', 'You cannot message this person.'),
        'AcademicYearRequired' => tr('اختر العام الدراسي أولاً.', 'Choose the academic year first.'),
        'NotTeacherOrParent' => tr('هذا الحساب غير مرتبط بمعلم أو ولي أمر.', 'This account is not linked to a teacher or a parent.'),
        _ when response.statusCode == 403 => tr('لست مشاركاً في هذه المحادثة.', 'You are not part of this chat.'),
        _ => fallback,
      });
    }
    if (wholeBody) return decoded;
    return decoded is Map<String, dynamic> ? decoded['data'] : null;
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
