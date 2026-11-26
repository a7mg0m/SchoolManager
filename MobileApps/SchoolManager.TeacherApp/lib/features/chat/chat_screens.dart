import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart' show ImageSource;

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/realtime/chat_realtime.dart';
import '../../core/theme/app_theme.dart';
import 'chat_media.dart';
import 'chat_service.dart';

const _red = Color(0xFFD93025);
const _seenBlue = Color(0xFF34B7F1);

/// The API calls the chat screens use (tests pass their own)
class ChatApi {
  const ChatApi({
    this.loadContacts,
    this.loadChats,
    this.start,
    this.loadMessages,
    this.send,
    this.seen,
    this.userId,
    this.loadPhoto,
    this.sendMedia,
  });

  final Future<List<ChatContact>> Function()? loadContacts;
  final Future<List<ChatSummary>> Function()? loadChats;
  final Future<String> Function(String otherUserId)? start;
  final Future<List<ChatMessage>> Function(String roomId)? loadMessages;
  final Future<ChatMessage> Function(String roomId, String content)? send;
  final Future<void> Function(String roomId, List<String> messageIds)? seen;
  final Future<Uint8List?> Function(String userId)? loadPhoto;

  /// Uploads a picture / video / voice note and sends it
  final Future<ChatMessage> Function(String roomId, ChatOutgoingMedia media)? sendMedia;

  /// The signed-in user's id
  final String? userId;

  String get myUserId => userId ?? ChatService.instance.myUserId;
  Future<List<ChatContact>> contacts({String? studentId}) =>
      loadContacts != null ? loadContacts!() : ChatService.instance.getContacts(studentId: studentId);
  Future<List<ChatSummary>> chats() => (loadChats ?? ChatService.instance.getChats)();
  Future<String> startChat(String otherUserId) => (start ?? ChatService.instance.startChat)(otherUserId);
  Future<List<ChatMessage>> messages(String roomId) => (loadMessages ?? ChatService.instance.getMessages)(roomId);
  Future<ChatMessage> sendMessage(String roomId, String content) =>
      send != null ? send!(roomId, content) : ChatService.instance.sendMessage(roomId, content);
  Future<ChatMessage> sendMediaMessage(String roomId, ChatOutgoingMedia media) =>
      sendMedia != null ? sendMedia!(roomId, media) : ChatService.instance.sendMedia(roomId, media);
  Future<void> markSeen(String roomId, List<String> ids) => (seen ?? ChatService.instance.markSeen)(roomId, ids);
  Future<Uint8List?> photo(String userId) => (loadPhoto ?? ChatService.instance.getPhoto)(userId);
}

/// A person's profile picture, or their initial on a colour while it loads / when they have none.
/// Pictures are kept in memory for the session (pull to refresh on the chats list reloads them).
class ChatAvatar extends StatefulWidget {
  const ChatAvatar({super.key, required this.api, required this.userId, required this.name, this.radius = 22, this.onPrimary = false});

  final ChatApi api;
  final String userId;
  final String name;
  final double radius;

  /// On the coloured app bar (white initial on a light circle)
  final bool onPrimary;

  static final _cache = <String, Future<Uint8List?>>{};

  /// Forget the loaded pictures (they are fetched again)
  static void clearCache() => _cache.clear();

  @override
  State<ChatAvatar> createState() => _ChatAvatarState();
}

class _ChatAvatarState extends State<ChatAvatar> {
  late Future<Uint8List?> _photo;

  @override
  void initState() {
    super.initState();
    _photo = _load();
  }

  @override
  void didUpdateWidget(ChatAvatar old) {
    super.didUpdateWidget(old);
    if (old.userId != widget.userId) _photo = _load();
  }

  Future<Uint8List?> _load() => ChatAvatar._cache.putIfAbsent(
        widget.userId,
        () => widget.api.photo(widget.userId).catchError((Object _) => null),
      );

  @override
  Widget build(BuildContext context) {
    final color = chatColor(widget.userId);
    return FutureBuilder<Uint8List?>(
      future: _photo,
      builder: (context, snapshot) {
        final bytes = snapshot.data;
        if (bytes != null) {
          return CircleAvatar(
            key: ValueKey('photo-${widget.userId}'),
            radius: widget.radius,
            backgroundColor: color.withValues(alpha: .15),
            backgroundImage: MemoryImage(bytes),
            // A picture that cannot be decoded falls back to the colour
            onBackgroundImageError: (error, stackTrace) {},
          );
        }
        return CircleAvatar(
          radius: widget.radius,
          backgroundColor: widget.onPrimary ? Colors.white24 : color.withValues(alpha: .15),
          child: Text(
            _initial(widget.name),
            style: TextStyle(
              color: widget.onPrimary ? Colors.white : color,
              fontWeight: FontWeight.bold,
              fontSize: widget.radius * .75,
            ),
          ),
        );
      },
    );
  }
}

String _initial(String name) => name.trim().isEmpty ? '?' : name.trim().characters.first;

/// One colour per person (picked from the id, so it stays the same)
Color chatColor(String id) {
  const colors = [Color(0xFF1677E8), Color(0xFF13864B), Color(0xFF8957E5), Color(0xFFF08C00), Color(0xFFE0457B), Color(0xFF0FA5A5)];
  return colors[id.codeUnits.fold<int>(0, (sum, c) => (sum * 31 + c) & 0x7fffffff) % colors.length];
}

/// "10:42 ص" today, "أمس", else "20/11"
String chatListTime(DateTime t, DateTime now) {
  final day = DateTime(t.year, t.month, t.day);
  final today = DateTime(now.year, now.month, now.day);
  if (day == today) return chatClock(t);
  if (day == today.subtract(const Duration(days: 1))) return tr('أمس', 'Yesterday');
  return '${t.day}/${t.month}';
}

/// "10:42 ص" / "10:42 AM"
String chatClock(DateTime t) {
  final h = t.hour % 12 == 0 ? 12 : t.hour % 12;
  return '$h:${t.minute.toString().padLeft(2, '0')} ${t.hour < 12 ? tr('ص', 'AM') : tr('م', 'PM')}';
}

/// Day header in a chat: "اليوم", "أمس", else "20/11/2026"
String chatDay(DateTime t, DateTime now) {
  final day = DateTime(t.year, t.month, t.day);
  final today = DateTime(now.year, now.month, now.day);
  if (day == today) return tr('اليوم', 'Today');
  if (day == today.subtract(const Duration(days: 1))) return tr('أمس', 'Yesterday');
  return '${t.day}/${t.month}/${t.year}';
}

/// My chats of the chosen academic year, newest first, with unread counts; start a new one with a contact
/// (a new chat belongs to the chosen year; another year has its own chats)
class ChatListScreen extends StatefulWidget {
  const ChatListScreen({
    super.key,
    this.api = const ChatApi(),
    this.onSessionExpired,
    this.refreshEvery = const Duration(seconds: 15),
    this.now,
    this.newChatHint,
    this.openRoomId,
    this.contactsStudentId,
  });

  final ChatApi api;
  final Future<void> Function()? onSessionExpired;

  /// How often the list is reloaded while open and the live connection is down (null: never; tests)
  final Duration? refreshEvery;
  final DateTime? now;

  /// "Message your students' parents" / "Message your children's teachers"
  final String? newChatHint;

  /// A tapped chat notification: open this chat once the list is loaded
  final String? openRoomId;

  /// Parent app: the child chosen on Home — a new chat lists only the teachers of that child's section timetable
  final String? contactsStudentId;

  @override
  State<ChatListScreen> createState() => _ChatListScreenState();
}

class _ChatListScreenState extends State<ChatListScreen> with ReloadOnAcademicYear, WidgetsBindingObserver {
  /// Another year: its own chats
  @override
  Future<void> reloadForAcademicYear() {
    setState(() => _chats = null);
    return _load();
  }

  List<ChatSummary>? _chats;
  String? _error;
  bool _loading = true;
  bool _starting = false;
  String _query = '';
  Timer? _timer;

  /// Live updates: a message / tick anywhere reloads the list (once per burst)
  StreamSubscription<ChatLiveMessage>? _liveMessages;
  StreamSubscription<ChatLiveStatus>? _liveStatuses;
  Timer? _liveDebounce;

  /// The chat of a tapped notification, until it is opened
  String? _pendingRoomId;

  DateTime get _now => widget.now ?? DateTime.now();

  @override
  void initState() {
    super.initState();
    _pendingRoomId = widget.openRoomId;
    WidgetsBinding.instance.addObserver(this);
    _liveMessages = ChatRealtime.instance.messages.listen((_) => _reloadSoon());
    _liveStatuses = ChatRealtime.instance.statuses.listen((_) => _reloadSoon());
    // Back online: catch up on what came while the connection was down
    ChatRealtime.instance.connected.addListener(_onConnectionChanged);
    _load();
    _startPolling();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    ChatRealtime.instance.connected.removeListener(_onConnectionChanged);
    _liveMessages?.cancel();
    _liveStatuses?.cancel();
    _liveDebounce?.cancel();
    _timer?.cancel();
    super.dispose();
  }

  void _reloadSoon() {
    _liveDebounce?.cancel();
    _liveDebounce = Timer(const Duration(milliseconds: 300), () {
      if (mounted) _load(quiet: true);
    });
  }

  void _onConnectionChanged() {
    if (ChatRealtime.instance.connected.value) _reloadSoon();
  }

  /// Polling only while the app is on screen: stopped in the background, a fresh load on return
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      _load(quiet: true);
      _startPolling();
    } else if (state == AppLifecycleState.paused || state == AppLifecycleState.hidden) {
      _timer?.cancel();
      _timer = null;
    }
  }

  /// Fallback while the live connection is down (live updates make it unnecessary)
  void _startPolling() {
    if (widget.refreshEvery == null || _timer != null) return;
    _timer = Timer.periodic(widget.refreshEvery!, (_) {
      if (!ChatRealtime.instance.connected.value) _load(quiet: true);
    });
  }

  Future<void> _load({bool quiet = false}) async {
    if (!quiet) {
      setState(() {
        _loading = _chats == null;
        _error = null;
      });
    }
    try {
      final result = await widget.api.chats();
      if (!mounted) return;
      setState(() {
        _chats = result;
        _loading = false;
        _error = null;
      });
      _openPendingRoom(result);
    } on ChatException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        _timer?.cancel();
        await widget.onSessionExpired!();
        return;
      }
      // A failed background refresh keeps what is shown
      if (quiet && _chats != null) return;
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  /// The chat of a tapped notification: open it once it is in the list
  void _openPendingRoom(List<ChatSummary> chats) {
    final roomId = _pendingRoomId;
    if (roomId == null || roomId.isEmpty) return;
    _pendingRoomId = null;
    for (final c in chats) {
      if (c.roomId == roomId) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) _openChat(c.roomId, c.name, c.detail, c.otherUserId);
        });
        return;
      }
    }
  }

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _openChat(String roomId, String name, String detail, String otherUserId) async {
    await Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => ChatScreen(
        api: widget.api,
        roomId: roomId,
        title: name,
        subtitle: detail,
        otherUserId: otherUserId,
        onSessionExpired: widget.onSessionExpired,
        refreshEvery: widget.refreshEvery == null ? null : const Duration(seconds: 5),
        now: widget.now,
      ),
    ));
    if (mounted) await _load(quiet: true);
  }

  Future<void> _newChat() async {
    final contact = await Navigator.of(context).push<ChatContact>(MaterialPageRoute(
      builder: (_) => ChatContactsScreen(
          api: widget.api, hint: widget.newChatHint, onSessionExpired: widget.onSessionExpired, studentId: widget.contactsStudentId),
    ));
    if (contact == null || !mounted) return;

    setState(() => _starting = true);
    try {
      final roomId = await widget.api.startChat(contact.userId);
      if (!mounted) return;
      setState(() => _starting = false);
      await _openChat(roomId, contact.name, contact.detail, contact.userId);
    } on ChatException catch (e) {
      if (!mounted) return;
      setState(() => _starting = false);
      _show(e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        // The chats follow the academic year chosen on home (shown here, changed there)
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('المحادثات', 'Chats'), style: const TextStyle(fontWeight: FontWeight.bold)),
            if (AcademicYearController.instance.value?.name case final year? when year.isNotEmpty)
              Text(tr('العام الدراسي $year', 'Academic year $year'), style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      floatingActionButton: _loading || _error != null
          ? null
          : FloatingActionButton.extended(
              key: const ValueKey('new-chat'),
              onPressed: _starting ? null : _newChat,
              icon: _starting
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.edit_rounded),
              label: Text(tr('محادثة جديدة', 'New chat')),
            ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _centerMessage(context, Icons.error_outline_rounded, _error!, retry: _load)
              : RefreshIndicator(
                  onRefresh: () {
                    ChatAvatar.clearCache();
                    return _load();
                  },
                  child: _content(_chats!),
                ),
    );
  }

  Widget _content(List<ChatSummary> chats) {
    final palette = context.palette;
    if (chats.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 90),
          _centerMessage(context, Icons.forum_outlined,
              tr('لا توجد محادثات في هذا العام الدراسي. اضغط "محادثة جديدة" للبدء.', 'No chats in this academic year. Tap "New chat" to start one.')),
        ],
      );
    }

    final q = _query.trim().toLowerCase();
    final shown = q.isEmpty
        ? chats
        : chats.where((c) => [c.nameAr, c.nameEn, c.detailAr, c.detailEn].any((s) => s.toLowerCase().contains(q))).toList();
    final unread = chats.fold<int>(0, (sum, c) => sum + c.unreadCount);
    final me = widget.api.myUserId;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 96),
      children: [
        TextField(
          key: const ValueKey('chats-search'),
          onChanged: (v) => setState(() => _query = v),
          decoration: InputDecoration(hintText: tr('ابحث في المحادثات', 'Search chats'), prefixIcon: const Icon(Icons.search_rounded), isDense: true),
        ),
        if (unread > 0)
          Padding(
            padding: const EdgeInsets.only(top: 10),
            child: Text(tr('$unread رسالة غير مقروءة', '$unread unread messages'),
                style: const TextStyle(color: AppTheme.blue, fontWeight: FontWeight.bold, fontSize: 12.5)),
          ),
        const SizedBox(height: 8),
        for (final c in shown)
          Material(
            color: Colors.transparent,
            child: InkWell(
              key: ValueKey('chat-${c.roomId}'),
              borderRadius: BorderRadius.circular(16),
              onTap: () => _openChat(c.roomId, c.name, c.detail, c.otherUserId),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 4),
                child: Row(
                  children: [
                    ChatAvatar(api: widget.api, userId: c.otherUserId, name: c.name, radius: 26),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(c.name.isEmpty ? tr('مستخدم', 'User') : c.name,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: palette.ink)),
                              ),
                              if (c.lastMessageOn != null)
                                Text(chatListTime(c.lastMessageOn!, _now),
                                    style: TextStyle(
                                        fontSize: 11.5,
                                        color: c.unreadCount > 0 ? AppTheme.blue : palette.muted,
                                        fontWeight: c.unreadCount > 0 ? FontWeight.bold : FontWeight.normal)),
                            ],
                          ),
                          const SizedBox(height: 3),
                          Row(
                            children: [
                              if (c.lastMessage != null && c.lastMessageSenderId == me)
                                Padding(
                                  padding: const EdgeInsetsDirectional.only(end: 4),
                                  child: Text(tr('أنت:', 'You:'), style: TextStyle(fontSize: 13, color: palette.muted)),
                                ),
                              Expanded(
                                child: Text(
                                  c.lastMessagePreview ?? c.detail,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: c.unreadCount > 0 ? palette.ink : palette.muted,
                                    fontWeight: c.unreadCount > 0 ? FontWeight.w600 : FontWeight.normal,
                                  ),
                                ),
                              ),
                              if (c.unreadCount > 0)
                                Container(
                                  key: ValueKey('unread-${c.roomId}'),
                                  margin: const EdgeInsetsDirectional.only(start: 6),
                                  padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
                                  decoration: BoxDecoration(color: AppTheme.blue, borderRadius: BorderRadius.circular(12)),
                                  child: Text('${c.unreadCount}',
                                      style: const TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.bold)),
                                ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        if (shown.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: Text(tr('لا توجد نتائج.', 'No results.'), textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
          ),
      ],
    );
  }
}

/// Pick who to message; pops with the chosen contact
class ChatContactsScreen extends StatefulWidget {
  const ChatContactsScreen({super.key, this.api = const ChatApi(), this.hint, this.onSessionExpired, this.studentId});

  final ChatApi api;
  final String? hint;
  final Future<void> Function()? onSessionExpired;

  /// Parent app: only the teachers of this child's section timetable
  final String? studentId;

  @override
  State<ChatContactsScreen> createState() => _ChatContactsScreenState();
}

class _ChatContactsScreenState extends State<ChatContactsScreen> {
  List<ChatContact>? _contacts;
  String? _error;
  String _query = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final result = await widget.api.contacts(studentId: widget.studentId);
      if (mounted) setState(() => _contacts = result);
    } on ChatException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      setState(() => _error = e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final contacts = _contacts;
    final shown = (contacts ?? const <ChatContact>[]).where((c) => c.matches(_query)).toList();

    return Scaffold(
      appBar: AppBar(title: Text(tr('محادثة جديدة', 'New chat'), style: const TextStyle(fontWeight: FontWeight.bold))),
      body: _error != null
          ? _centerMessage(context, Icons.error_outline_rounded, _error!, retry: _load)
          : contacts == null
              ? const Center(child: CircularProgressIndicator())
              : ListView(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                  children: [
                    if (widget.hint != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: Text(widget.hint!, style: TextStyle(color: palette.muted, fontSize: 13)),
                      ),
                    TextField(
                      key: const ValueKey('contacts-search'),
                      onChanged: (v) => setState(() => _query = v),
                      decoration: InputDecoration(hintText: tr('ابحث بالاسم', 'Search by name'), prefixIcon: const Icon(Icons.search_rounded), isDense: true),
                    ),
                    const SizedBox(height: 8),
                    if (contacts.isEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 60),
                        child: _centerMessage(context, Icons.person_search_rounded, widget.studentId != null
                            ? tr('لا يوجد معلمون في جدول فصل الطالب لهذا الفصل الدراسي.',
                                "No teachers in the student's class timetable for this semester.")
                            : tr('لا توجد جهات اتصال متاحة.', 'No contacts available.')),
                      )
                    else
                      for (final c in shown)
                        ListTile(
                          key: ValueKey('contact-${c.userId}'),
                          contentPadding: const EdgeInsets.symmetric(horizontal: 4),
                          leading: ChatAvatar(api: widget.api, userId: c.userId, name: c.name, radius: 20),
                          title: Text(c.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                          subtitle: c.detail.isEmpty ? null : Text(c.detail, maxLines: 1, overflow: TextOverflow.ellipsis),
                          trailing: const Icon(Icons.chat_bubble_outline_rounded, color: AppTheme.blue),
                          onTap: () => Navigator.of(context).pop(c),
                        ),
                  ],
                ),
    );
  }
}

/// One conversation: messages by day, my bubbles on one side with delivery ticks, and a box to type
class ChatScreen extends StatefulWidget {
  const ChatScreen({
    super.key,
    this.api = const ChatApi(),
    required this.roomId,
    required this.title,
    this.subtitle = '',
    this.otherUserId = '',
    this.onSessionExpired,
    this.refreshEvery = const Duration(seconds: 5),
    this.now,
  });

  final ChatApi api;
  final String roomId;
  final String title;
  final String subtitle;
  final String otherUserId;
  final Future<void> Function()? onSessionExpired;

  /// How often new messages are fetched while open (null: never; tests)
  final Duration? refreshEvery;
  final DateTime? now;

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> with WidgetsBindingObserver {
  final _text = TextEditingController();

  /// Oldest first
  List<ChatMessage>? _messages;

  /// Sent here but not confirmed yet (shown at the end)
  final List<ChatMessage> _pending = [];
  String? _error;
  Timer? _timer;
  int _localId = 0;

  /// Voice note being recorded (the bar replaces the text box), ticking every second
  final _recorder = ChatVoiceRecorder();
  Timer? _recordTicker;

  /// Live: new messages of this chat and the other person's ticks
  StreamSubscription<ChatLiveMessage>? _liveMessages;
  StreamSubscription<ChatLiveStatus>? _liveStatuses;

  String get _me => widget.api.myUserId;
  DateTime get _now => widget.now ?? DateTime.now();

  @override
  void initState() {
    super.initState();
    _text.addListener(() => setState(() {}));
    WidgetsBinding.instance.addObserver(this);
    // This chat is on screen: its push notifications are not shown, its messages are seen right away
    ChatRealtime.instance.activeRoomId = widget.roomId;
    _liveMessages = ChatRealtime.instance.messages.listen(_onLiveMessage);
    _liveStatuses = ChatRealtime.instance.statuses.listen(_onLiveStatus);
    ChatRealtime.instance.connected.addListener(_onConnectionChanged);
    _load();
    _startPolling();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    if (ChatRealtime.instance.activeRoomId == widget.roomId) ChatRealtime.instance.activeRoomId = null;
    ChatRealtime.instance.connected.removeListener(_onConnectionChanged);
    _liveMessages?.cancel();
    _liveStatuses?.cancel();
    _timer?.cancel();
    _recordTicker?.cancel();
    _recorder.dispose();
    _text.dispose();
    super.dispose();
  }

  /// Back online: catch up on what came while the connection was down
  void _onConnectionChanged() {
    if (ChatRealtime.instance.connected.value && mounted) _load(quiet: true);
  }

  /// A message of this chat arrived live: add it (once) and, when it is theirs, tell them I have seen it
  void _onLiveMessage(ChatLiveMessage live) {
    if (!mounted || live.roomId != widget.roomId) return;
    final message = live.message;
    if (message.id.isEmpty || (_messages?.any((m) => m.id == message.id) ?? false)) return;
    setState(() {
      // My own message (sent from here and still "sending", or from another device)
      if (message.senderId == _me) {
        final i = _pending.indexWhere((p) => p.status == ChatStatus.sending && p.content == message.content);
        if (i >= 0) _pending.removeAt(i);
      }
      _messages = [...?_messages, message];
    });
    if (message.senderId != _me) {
      widget.api.markSeen(widget.roomId, [message.id]).catchError((_) {});
    }
  }

  /// The other person received / saw my messages: move their ticks forward (never back)
  void _onLiveStatus(ChatLiveStatus live) {
    if (!mounted || live.roomId != widget.roomId || _messages == null) return;
    bool moves(ChatMessage m) => m.senderId == _me && live.messageIds.contains(m.id) && _rank(live.status) > _rank(m.status);
    if (!_messages!.any(moves)) return;
    setState(() => _messages = [for (final m in _messages!) moves(m) ? m.withStatus(live.status) : m]);
  }

  static int _rank(ChatStatus s) => switch (s) {
        ChatStatus.seen => 3,
        ChatStatus.delivered => 2,
        ChatStatus.sent => 1,
        _ => 0,
      };

  /// Polling only while the app is on screen: stopped in the background, a fresh load on return
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      _load(quiet: true);
      _startPolling();
    } else if (state == AppLifecycleState.paused || state == AppLifecycleState.hidden) {
      _timer?.cancel();
      _timer = null;
    }
  }

  /// Fallback while the live connection is down (live updates make it unnecessary)
  void _startPolling() {
    if (widget.refreshEvery == null || _timer != null) return;
    _timer = Timer.periodic(widget.refreshEvery!, (_) {
      if (!ChatRealtime.instance.connected.value) _load(quiet: true);
    });
  }

  Future<void> _load({bool quiet = false}) async {
    try {
      final latest = await widget.api.messages(widget.roomId);
      if (!mounted) return;
      setState(() {
        _messages = latest.reversed.toList();
        _error = null;
      });
      // What they sent and I have not seen yet
      final unseen = latest.where((m) => m.senderId != _me && m.status != ChatStatus.seen).map((m) => m.id).toList();
      if (unseen.isNotEmpty) {
        try {
          await widget.api.markSeen(widget.roomId, unseen);
        } on ChatException {
          // Not important: they just won't see the blue ticks yet
        }
      }
    } on ChatException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        _timer?.cancel();
        await widget.onSessionExpired!();
        return;
      }
      if (quiet && _messages != null) return;
      setState(() => _error = e.message);
    }
  }

  Future<void> _send([ChatMessage? retry]) async {
    if (retry?.localMedia != null) return _sendMedia(retry!.localMedia!, retry: retry);
    final content = retry?.content ?? _text.text.trim();
    if (content.isEmpty) return;
    if (content.length > chatMaxLength) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(tr('الرسالة طويلة جداً.', 'The message is too long.'))));
      return;
    }

    final pending = ChatMessage(id: 'local-${_localId++}', senderId: _me, content: content, sentOn: DateTime.now(), status: ChatStatus.sending);
    setState(() {
      if (retry != null) _pending.remove(retry);
      _pending.add(pending);
      if (retry == null) _text.clear();
    });

    try {
      final sent = await widget.api.sendMessage(widget.roomId, content);
      if (!mounted) return;
      setState(() {
        _pending.remove(pending);
        // The live update may have brought it already
        if (!(_messages?.any((m) => m.id == sent.id) ?? false)) _messages = [...?_messages, sent];
      });
    } on ChatException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      setState(() {
        final i = _pending.indexOf(pending);
        if (i >= 0) _pending[i] = pending.withStatus(ChatStatus.failed);
      });
    }
  }

  void _snack(String text) => ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(text)));

  /// Uploads and sends a picture / video / voice note; it shows at once with a spinner, "tap to retry" if it fails
  Future<void> _sendMedia(ChatOutgoingMedia media, {ChatMessage? retry}) async {
    final pending = ChatMessage(
        id: 'local-${_localId++}', senderId: _me, content: '', sentOn: DateTime.now(), status: ChatStatus.sending, localMedia: media);
    setState(() {
      if (retry != null) _pending.remove(retry);
      _pending.add(pending);
    });
    try {
      final sent = await widget.api.sendMediaMessage(widget.roomId, media);
      if (!mounted) return;
      setState(() {
        _pending.remove(pending);
        if (!(_messages?.any((m) => m.id == sent.id) ?? false)) _messages = [...?_messages, sent];
      });
    } on ChatException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      _snack(e.message);
      setState(() {
        final i = _pending.indexOf(pending);
        if (i >= 0) _pending[i] = pending.withStatus(ChatStatus.failed);
      });
    }
  }

  /// Camera / gallery, picture / video
  Future<void> _attach() async {
    final choice = await showModalBottomSheet<(bool, ImageSource)>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              key: const ValueKey('attach-camera-photo'),
              leading: const Icon(Icons.photo_camera_rounded),
              title: Text(tr('التقاط صورة', 'Take a photo')),
              onTap: () => Navigator.pop(context, (false, ImageSource.camera)),
            ),
            ListTile(
              key: const ValueKey('attach-gallery-photo'),
              leading: const Icon(Icons.photo_library_rounded),
              title: Text(tr('صورة من المعرض', 'Photo from gallery')),
              onTap: () => Navigator.pop(context, (false, ImageSource.gallery)),
            ),
            ListTile(
              key: const ValueKey('attach-camera-video'),
              leading: const Icon(Icons.videocam_rounded),
              title: Text(tr('تصوير فيديو', 'Record a video')),
              onTap: () => Navigator.pop(context, (true, ImageSource.camera)),
            ),
            ListTile(
              key: const ValueKey('attach-gallery-video'),
              leading: const Icon(Icons.video_library_rounded),
              title: Text(tr('فيديو من المعرض', 'Video from gallery')),
              subtitle: Text(tr('حتى 25 ميجابايت', 'Up to 25 MB')),
              onTap: () => Navigator.pop(context, (true, ImageSource.gallery)),
            ),
          ],
        ),
      ),
    );
    if (choice == null || !mounted) return;
    final media = await pickChatMedia(video: choice.$1, source: choice.$2, onError: (m) {
      if (mounted) _snack(m);
    });
    if (media != null && mounted) await _sendMedia(media);
  }

  Future<void> _startRecording() async {
    if (!await _recorder.start()) {
      if (mounted) {
        _snack(tr('اسمح للتطبيق باستخدام الميكروفون لتسجيل رسالة صوتية.', 'Allow the microphone to record a voice message.'));
      }
      return;
    }
    if (!mounted) return;
    setState(() {});
    _recordTicker?.cancel();
    _recordTicker = Timer.periodic(const Duration(milliseconds: 500), (_) {
      if (!mounted) return;
      // Longest note: it is sent by itself
      if (_recorder.elapsed >= ChatVoiceRecorder.maxLength) {
        _stopRecording(send: true);
      } else {
        setState(() {});
      }
    });
  }

  Future<void> _stopRecording({required bool send}) async {
    _recordTicker?.cancel();
    _recordTicker = null;
    if (!send) {
      await _recorder.cancel();
      if (mounted) setState(() {});
      return;
    }
    final note = await _recorder.stop();
    if (!mounted) return;
    setState(() {});
    if (note == null) {
      _snack(tr('التسجيل قصير جداً.', 'The recording is too short.'));
      return;
    }
    await _sendMedia(note);
  }

  /// Recording: cancel, the time with a red dot, send
  Widget _recordingBar() {
    final palette = context.palette;
    return Row(
      key: const ValueKey('chat-recording'),
      children: [
        IconButton(
          key: const ValueKey('record-cancel'),
          tooltip: tr('إلغاء', 'Cancel'),
          onPressed: () => _stopRecording(send: false),
          icon: const Icon(Icons.delete_outline_rounded, color: _red),
        ),
        Expanded(
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(24)),
            child: Row(children: [
              const Icon(Icons.fiber_manual_record_rounded, color: _red, size: 14),
              const SizedBox(width: 8),
              Text(chatDuration(_recorder.elapsed), style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
              const SizedBox(width: 10),
              Expanded(
                child: Text(tr('جارٍ التسجيل…', 'Recording…'),
                    maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(color: palette.muted)),
              ),
            ]),
          ),
        ),
        const SizedBox(width: 8),
        IconButton.filled(
          key: const ValueKey('record-send'),
          tooltip: tr('إرسال', 'Send'),
          onPressed: () => _stopRecording(send: true),
          icon: const Icon(Icons.send_rounded),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    return Scaffold(
      appBar: AppBar(
        titleSpacing: 0,
        title: Row(
          children: [
            ChatAvatar(api: widget.api, userId: widget.otherUserId, name: widget.title, radius: 18, onPrimary: true),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(widget.title, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  if (widget.subtitle.isNotEmpty)
                    Text(widget.subtitle, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 12, color: Colors.white70)),
                ],
              ),
            ),
          ],
        ),
      ),
      body: Column(
        children: [
          Expanded(
            child: _error != null
                ? _centerMessage(context, Icons.error_outline_rounded, _error!, retry: _load)
                : _messages == null
                    ? const Center(child: CircularProgressIndicator())
                    : _messageList(),
          ),
          SafeArea(
            top: false,
            child: Container(
              padding: const EdgeInsets.fromLTRB(10, 8, 10, 8),
              decoration: BoxDecoration(color: palette.card, border: Border(top: BorderSide(color: palette.line))),
              child: _recorder.isRecording
                  ? _recordingBar()
                  : Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  IconButton(
                    key: const ValueKey('chat-attach'),
                    tooltip: tr('صورة أو فيديو', 'Photo or video'),
                    onPressed: _attach,
                    icon: Icon(Icons.add_photo_alternate_rounded, color: palette.muted),
                  ),
                  Expanded(
                    child: TextField(
                      key: const ValueKey('chat-input'),
                      controller: _text,
                      minLines: 1,
                      maxLines: 5,
                      maxLength: chatMaxLength,
                      textInputAction: TextInputAction.newline,
                      decoration: InputDecoration(
                        hintText: tr('اكتب رسالة…', 'Type a message…'),
                        counterText: '',
                        isDense: true,
                        filled: true,
                        fillColor: palette.soft,
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(24), borderSide: BorderSide.none),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  // Nothing typed: the mic records a voice note
                  _text.text.trim().isEmpty
                      ? IconButton.filled(
                          key: const ValueKey('chat-mic'),
                          tooltip: tr('تسجيل رسالة صوتية', 'Record a voice message'),
                          onPressed: _startRecording,
                          icon: const Icon(Icons.mic_rounded),
                        )
                      : IconButton.filled(
                          key: const ValueKey('chat-send'),
                          tooltip: tr('إرسال', 'Send'),
                          onPressed: () => _send(),
                          icon: const Icon(Icons.send_rounded),
                        ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _messageList() {
    final all = [...?_messages, ..._pending];
    if (all.isEmpty) {
      return _centerMessage(context, Icons.waving_hand_rounded,
          tr('لا توجد رسائل بعد. اكتب أول رسالة.', 'No messages yet. Write the first one.'));
    }

    // Newest at the bottom: a reversed list starts there
    final items = <Widget>[];
    for (var i = all.length - 1; i >= 0; i--) {
      final m = all[i];
      final newer = i < all.length - 1 ? all[i + 1] : null;
      // Their picture goes on the last message of a run (the next one is mine, or another day)
      final lastOfRun = newer == null || newer.senderId != m.senderId || !_sameDay(newer.sentOn, m.sentOn);
      items.add(_bubble(m, showAvatar: lastOfRun));
      final older = i > 0 ? all[i - 1] : null;
      if (older == null || !_sameDay(older.sentOn, m.sentOn)) items.add(_dayHeader(m.sentOn));
    }
    return ListView(reverse: true, padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10), children: items);
  }

  static bool _sameDay(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;

  Widget _dayHeader(DateTime t) => Center(
        child: Container(
          margin: const EdgeInsets.symmetric(vertical: 10),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
          decoration: BoxDecoration(color: context.palette.chip, borderRadius: BorderRadius.circular(12)),
          child: Text(chatDay(t, _now), style: TextStyle(fontSize: 11.5, color: context.palette.muted, fontWeight: FontWeight.w600)),
        ),
      );

  Widget _bubble(ChatMessage m, {bool showAvatar = false}) {
    final palette = context.palette;
    final mine = m.senderId == _me;
    final failed = m.status == ChatStatus.failed;
    final hasMedia = m.attachments.isNotEmpty || m.localMedia != null;
    final bubble = Container(
      constraints: BoxConstraints(maxWidth: MediaQuery.sizeOf(context).width * .75),
      margin: const EdgeInsets.symmetric(vertical: 3),
      padding: hasMedia ? const EdgeInsets.fromLTRB(5, 5, 5, 6) : const EdgeInsets.fromLTRB(12, 8, 12, 6),
      decoration: BoxDecoration(
        color: mine ? (failed ? _red.withValues(alpha: .12) : AppTheme.blue) : palette.card,
        border: mine ? null : Border.all(color: palette.line),
        borderRadius: BorderRadiusDirectional.only(
          topStart: const Radius.circular(18),
          topEnd: const Radius.circular(18),
          bottomStart: Radius.circular(mine ? 18 : 4),
          bottomEnd: Radius.circular(mine ? 4 : 18),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        mainAxisSize: MainAxisSize.min,
        children: [
          if (m.localMedia != null)
            ChatMediaView(local: m.localMedia, mine: mine && !failed, sending: m.status == ChatStatus.sending)
          else
            for (final a in m.attachments)
              Padding(
                padding: const EdgeInsets.only(bottom: 2),
                child: ChatMediaView(attachment: a, mine: mine),
              ),
          if (m.content.isNotEmpty)
            Padding(
              padding: hasMedia ? const EdgeInsets.fromLTRB(7, 6, 7, 0) : EdgeInsets.zero,
              child: Align(
                alignment: AlignmentDirectional.centerStart,
                widthFactor: 1,
                child: Text(m.content,
                    style: TextStyle(fontSize: 14.5, height: 1.4, color: mine && !failed ? Colors.white : palette.ink)),
              ),
            ),
          const SizedBox(height: 3),
          Padding(
            padding: hasMedia ? const EdgeInsetsDirectional.only(end: 7) : EdgeInsets.zero,
            child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(chatClock(m.sentOn),
                  style: TextStyle(fontSize: 10.5, color: mine && !failed ? Colors.white70 : palette.muted)),
              if (mine) ...[const SizedBox(width: 4), _tick(m.status)],
            ],
          ),
          ),
        ],
      ),
    );

    if (!mine) {
      // Their messages: picture (or the same space) on the start side
      const size = 14.0;
      return Align(
        alignment: AlignmentDirectional.centerStart,
        child: Row(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            showAvatar
                ? Padding(
                    padding: const EdgeInsets.only(bottom: 3),
                    child: ChatAvatar(api: widget.api, userId: m.senderId, name: widget.title, radius: size),
                  )
                : const SizedBox(width: size * 2),
            const SizedBox(width: 6),
            Flexible(child: bubble),
          ],
        ),
      );
    }

    return Align(
      alignment: mine ? AlignmentDirectional.centerEnd : AlignmentDirectional.centerStart,
      child: failed
          ? InkWell(
              key: ValueKey('retry-${m.id}'),
              onTap: () => _send(m),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  bubble,
                  Text(tr('لم تُرسل، اضغط لإعادة المحاولة', 'Not sent, tap to retry'),
                      style: const TextStyle(color: _red, fontSize: 11)),
                ],
              ),
            )
          : bubble,
    );
  }

  Widget _tick(ChatStatus status) => switch (status) {
        ChatStatus.sending => const Icon(Icons.schedule_rounded, size: 14, color: Colors.white70),
        ChatStatus.sent => const Icon(Icons.done_rounded, size: 15, color: Colors.white70),
        ChatStatus.delivered => const Icon(Icons.done_all_rounded, size: 15, color: Colors.white70),
        ChatStatus.seen => const Icon(Icons.done_all_rounded, size: 15, color: _seenBlue, key: ValueKey('seen-tick')),
        ChatStatus.failed => const Icon(Icons.error_outline_rounded, size: 15, color: _red),
      };
}

Widget _centerMessage(BuildContext context, IconData icon, String text, {Future<void> Function()? retry}) => Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry != null ? _red : AppTheme.blue.withValues(alpha: .6)),
            const SizedBox(height: 12),
            Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.ink)),
            if (retry != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: retry, child: Text(tr('إعادة المحاولة', 'Try again'))),
            ],
          ],
        ),
      ),
    );
