import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart' show MediaType;

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

/// What a chat file is
enum ChatMediaKind { image, video, audio, file }

ChatMediaKind chatMediaKind(String? contentTypeOrKind) {
  final v = (contentTypeOrKind ?? '').toLowerCase();
  if (v.startsWith('image')) return ChatMediaKind.image;
  if (v.startsWith('video')) return ChatMediaKind.video;
  if (v.startsWith('audio')) return ChatMediaKind.audio;
  return ChatMediaKind.file;
}

/// "📷 Photo" / "🎥 Video" / "🎤 Voice message" for a message without words
String chatMediaLabel(ChatMediaKind kind) => switch (kind) {
      ChatMediaKind.image => tr('📷 صورة', '📷 Photo'),
      ChatMediaKind.video => tr('🎥 فيديو', '🎥 Video'),
      ChatMediaKind.audio => tr('🎤 رسالة صوتية', '🎤 Voice message'),
      ChatMediaKind.file => tr('📎 ملف', '📎 File'),
    };

/// Largest files the server takes (the same limits as the API)
const chatMaxImageBytes = 10 * 1024 * 1024;
const chatMaxVideoBytes = 25 * 1024 * 1024;
const chatMaxAudioBytes = 10 * 1024 * 1024;

/// A picture / video / voice note of a message (uploaded with POST /api/chat/{roomId}/attachments)
class ChatAttachment {
  const ChatAttachment({required this.fileName, required this.url, required this.contentType, this.sizeInBytes = 0});

  final String fileName;

  /// Relative to the API ("/api/chat/files/{roomId}/{name}"); see [ChatService.fileUrl]
  final String url;
  final String contentType;
  final int sizeInBytes;

  ChatMediaKind get kind => chatMediaKind(contentType);

  factory ChatAttachment.fromJson(Map<String, dynamic> json) => ChatAttachment(
        fileName: '${json['fileName'] ?? ''}',
        url: '${json['url'] ?? ''}',
        contentType: '${json['contentType'] ?? ''}',
        sizeInBytes: (json['sizeInBytes'] as num?)?.toInt() ?? 0,
      );

  Map<String, Object?> toJson() => {'fileName': fileName, 'url': url, 'contentType': contentType, 'sizeInBytes': sizeInBytes};
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
    this.lastAttachmentKind,
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

  /// The last message is a picture / video / voice note ("image" | "video" | "audio" | "file"; null: text)
  final String? lastAttachmentKind;

  String get name => pickLang(nameAr, nameEn);
  String get detail => pickLang(detailAr, detailEn);

  /// The last message as the list shows it: its words, else "🎤 Voice message"…
  String? get lastMessagePreview {
    final text = lastMessage?.trim() ?? '';
    if (text.isNotEmpty) return text;
    if (lastAttachmentKind != null) return chatMediaLabel(chatMediaKind(lastAttachmentKind));
    return lastMessage;
  }

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
        lastAttachmentKind: json['lastAttachmentKind'] as String?,
      );
}

/// Delivery of a message (MessageDeliveryStatus on the API)
enum ChatStatus { sending, sent, delivered, seen, failed }

/// A message of a chat (history / send)
class ChatMessage {
  const ChatMessage({
    required this.id,
    required this.senderId,
    required this.content,
    required this.sentOn,
    this.status = ChatStatus.sent,
    this.attachments = const [],
    this.localMedia,
  });

  final String id;
  final String senderId;
  final String content;
  final DateTime sentOn;

  /// For my messages: how far the other person got; for theirs: what I did with it
  final ChatStatus status;

  /// Pictures / videos / voice notes
  final List<ChatAttachment> attachments;

  /// A file still being sent from this phone (shown while it uploads, sent again on retry)
  final ChatOutgoingMedia? localMedia;

  ChatMessage withStatus(ChatStatus value) => ChatMessage(
      id: id, senderId: senderId, content: content, sentOn: sentOn, status: value, attachments: attachments, localMedia: localMedia);

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
        attachments: json['attachments'] is List
            ? (json['attachments'] as List).whereType<Map>().map((a) => ChatAttachment.fromJson(Map<String, dynamic>.from(a))).toList()
            : const [],
      );
}

/// A picture / video / voice note picked or recorded here, before it is uploaded
class ChatOutgoingMedia {
  const ChatOutgoingMedia({required this.bytes, required this.fileName, required this.kind, this.duration});

  final Uint8List bytes;

  /// With the right extension (the server checks it): photo.jpg, clip.mp4, voice.m4a…
  final String fileName;
  final ChatMediaKind kind;

  /// Length of a voice note (shown while it is sent)
  final Duration? duration;

  int get maxBytes => switch (kind) {
        ChatMediaKind.video => chatMaxVideoBytes,
        ChatMediaKind.audio => chatMaxAudioBytes,
        _ => chatMaxImageBytes,
      };

  bool get tooLarge => bytes.length > maxBytes;
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

  /// Who I may chat with, in the academic year / semester chosen in the app:
  /// - a teacher: the parents of the classes they teach in that timetable;
  /// - a parent with the child chosen in the app ([studentId]): the teachers of that child's section timetable.
  Future<List<ChatContact>> getContacts({String? studentId}) async {
    final path = studentId == null || studentId.isEmpty
        ? _withYear('/api/chat/my/contacts')
        : _withYear('/api/chat/my/contacts?studentId=${Uri.encodeQueryComponent(studentId)}');
    final data = await _send('GET', path, fallback: tr('تعذر تحميل جهات الاتصال.', 'Could not load the contacts.'));
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

  Future<ChatMessage> sendMessage(String roomId, String content, {List<ChatAttachment> attachments = const []}) async {
    final body = await _send('POST', '/api/chat/send-message',
        body: {'roomId': roomId, 'content': content, if (attachments.isNotEmpty) 'attachments': [for (final a in attachments) a.toJson()]},
        fallback: tr('تعذر إرسال الرسالة.', 'Could not send the message.'),
        wholeBody: true);
    if (body is! Map<String, dynamic>) throw ChatException(tr('تعذر إرسال الرسالة.', 'Could not send the message.'));
    final sent = ChatMessage.fromJson(body);
    // The send answer has no attachments: keep the ones sent
    return sent.attachments.isEmpty && attachments.isNotEmpty
        ? ChatMessage(id: sent.id, senderId: sent.senderId, content: sent.content, sentOn: sent.sentOn, status: sent.status, attachments: attachments)
        : sent;
  }

  /// Uploads a picture / video / voice note, then sends it as a message (with [caption] as its words)
  Future<ChatMessage> sendMedia(String roomId, ChatOutgoingMedia media, {String caption = ''}) async {
    final attachment = await uploadAttachment(roomId, media);
    return sendMessage(roomId, caption, attachments: [attachment]);
  }

  /// POST /api/chat/{roomId}/attachments (multipart "file")
  Future<ChatAttachment> uploadAttachment(String roomId, ChatOutgoingMedia media) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw ChatException(_sessionEnded, sessionExpired: true);
    if (media.tooLarge) throw ChatException(_tooLarge(media.kind));

    final request = http.MultipartRequest('POST', Uri.parse('${AppConfig.apiBaseUrl}/api/chat/${Uri.encodeComponent(roomId)}/attachments'))
      ..headers.addAll({'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'})
      ..files.add(http.MultipartFile.fromBytes('file', media.bytes,
          filename: media.fileName, contentType: MediaType.parse(_contentType(media.fileName))));

    final http.Response response;
    try {
      // Videos take longer than a normal call
      response = await http.Response.fromStream(await request.send()).timeout(const Duration(minutes: 3));
    } on TimeoutException {
      throw ChatException(tr('انتهت مهلة رفع الملف.', 'The upload took too long.'));
    } catch (_) {
      throw ChatException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    Object? decoded;
    try {
      decoded = jsonDecode(utf8.decode(response.bodyBytes));
    } catch (_) {}
    final message = decoded is Map<String, dynamic> ? '${decoded['message'] ?? ''}' : '';
    if (response.statusCode == 401) throw ChatException(_sessionEnded, sessionExpired: true);
    if (response.statusCode == 413 || message == 'FileTooLarge') throw ChatException(_tooLarge(media.kind));
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ChatException(switch (message) {
        'FileTypeNotAllowed' => tr('نوع الملف غير مدعوم.', 'This type of file is not supported.'),
        'NotAParticipant' => tr('لست مشاركاً في هذه المحادثة.', 'You are not part of this chat.'),
        _ => tr('تعذر رفع الملف.', 'Could not upload the file.'),
      });
    }
    final data = decoded is Map<String, dynamic> ? decoded['data'] : null;
    if (data is! Map) throw ChatException(tr('تعذر رفع الملف.', 'Could not upload the file.'));
    return ChatAttachment.fromJson(Map<String, dynamic>.from(data));
  }

  /// Full address of a chat file for pictures / players: the token goes in ?access_token= (players can't send headers everywhere)
  String fileUrl(ChatAttachment attachment) {
    final token = AuthService.instance.session?.token ?? '';
    final base = attachment.url.startsWith('http') ? attachment.url : '${AppConfig.apiBaseUrl}${attachment.url}';
    return token.isEmpty ? base : '$base${base.contains('?') ? '&' : '?'}access_token=${Uri.encodeQueryComponent(token)}';
  }

  static String _tooLarge(ChatMediaKind kind) => switch (kind) {
        ChatMediaKind.video => tr('الفيديو كبير جداً (الحد 25 ميجابايت).', 'The video is too large (25 MB max).'),
        ChatMediaKind.audio => tr('التسجيل طويل جداً.', 'The recording is too long.'),
        _ => tr('الصورة كبيرة جداً (الحد 10 ميجابايت).', 'The picture is too large (10 MB max).'),
      };

  static String _contentType(String fileName) {
    final ext = fileName.contains('.') ? fileName.split('.').last.toLowerCase() : '';
    return switch (ext) {
      'jpg' || 'jpeg' => 'image/jpeg',
      'png' => 'image/png',
      'webp' => 'image/webp',
      'gif' => 'image/gif',
      'heic' => 'image/heic',
      'mp4' => 'video/mp4',
      'mov' => 'video/quicktime',
      '3gp' => 'video/3gpp',
      'webm' => 'video/webm',
      'm4a' => 'audio/mp4',
      'aac' => 'audio/aac',
      'mp3' => 'audio/mpeg',
      'wav' => 'audio/wav',
      'ogg' || 'opus' => 'audio/ogg',
      'weba' => 'audio/webm',
      _ => 'application/octet-stream',
    };
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

  /// Tells the sender these messages reached my phone (grey double tick)
  Future<void> markDelivered(String roomId, List<String> messageIds) async {
    if (messageIds.isEmpty) return;
    await _send('PUT', '/api/chat/history/${Uri.encodeComponent(roomId)}/status',
        body: {'messageIds': messageIds, 'status': 2}, fallback: '');
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
