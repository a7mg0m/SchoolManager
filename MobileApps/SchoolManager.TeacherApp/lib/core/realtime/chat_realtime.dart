import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../../features/chat/chat_service.dart';
import '../auth/auth_service.dart';
import '../config.dart';

/// A message that arrived live, with its chat
class ChatLiveMessage {
  const ChatLiveMessage(this.roomId, this.message);
  final String roomId;
  final ChatMessage message;
}

/// My messages delivered to / seen by the other person (their ticks)
class ChatLiveStatus {
  const ChatLiveStatus({required this.roomId, required this.userId, required this.messageIds, required this.status});
  final String roomId;
  final String userId;
  final Set<String> messageIds;
  final ChatStatus status;
}

/// Live chat (SignalR, {API}/hubs/chat): new messages and ticks reach the chat list and the open chat without a refresh.
/// - connects after login and on app start when signed in ([start]); disconnects at logout ([stop]);
/// - only while the app is on screen: it disconnects in the background (battery; the push notification takes over)
///   and connects again on return, and reconnects by itself when the network drops;
/// - [activeRoomId] is the chat on screen: its push notifications are not shown, and its messages are marked seen
///   by the chat screen; messages of other chats are marked delivered (grey double tick for the sender).
/// The screens still poll while [connected] is false (server unreachable), so nothing is ever missed.
class ChatRealtime with WidgetsBindingObserver {
  ChatRealtime._();
  static final instance = ChatRealtime._();

  static const _messageEvent = 'chatMessage';
  static const _statusEvent = 'chatStatus';

  /// True while the live connection is up
  final connected = ValueNotifier<bool>(false);

  /// The chat open on screen (set by the chat screen)
  String? activeRoomId;

  final _messages = StreamController<ChatLiveMessage>.broadcast();
  final _statuses = StreamController<ChatLiveStatus>.broadcast();
  Stream<ChatLiveMessage> get messages => _messages.stream;
  Stream<ChatLiveStatus> get statuses => _statuses.stream;

  HubConnection? _connection;

  /// Signed in and wants the connection (between [start] and [stop])
  bool _wanted = false;
  bool _inBackground = false;
  bool _observing = false;
  Timer? _retry;
  int _attempt = 0;

  /// Widget tests: no connection (it would leave timers and sockets behind)
  /// (the real app runs on WidgetsFlutterBinding; flutter test uses its own binding). Works on the web too.
  static bool get _isTest => WidgetsBinding.instance is! WidgetsFlutterBinding;

  /// After login / on app start when signed in. Never throws.
  Future<void> start() async {
    if (_isTest) return;
    _wanted = true;
    if (!_observing) {
      WidgetsBinding.instance.addObserver(this);
      _observing = true;
    }
    await _connect();
  }

  /// Logout. Never throws.
  Future<void> stop() async {
    _wanted = false;
    activeRoomId = null;
    await _disconnect();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      _inBackground = false;
      _attempt = 0;
      unawaited(_connect());
    } else if (state == AppLifecycleState.paused || state == AppLifecycleState.hidden) {
      _inBackground = true;
      unawaited(_disconnect());
    }
  }

  Future<void> _connect() async {
    if (!_wanted || _inBackground || _connection != null) return;
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) return;
    _retry?.cancel();

    final connection = HubConnectionBuilder()
        .withUrl(
          '${AppConfig.apiBaseUrl}/hubs/chat',
          options: HttpConnectionOptions(
            // WebSockets straight away (no negotiate round trip); the token goes in ?access_token=
            transport: HttpTransportType.WebSockets,
            skipNegotiation: true,
            accessTokenFactory: () async => AuthService.instance.session?.token ?? '',
          ),
        )
        .withAutomaticReconnect(retryDelays: const [0, 2000, 5000, 10000, 20000, 30000])
        .build();

    connection.on(_messageEvent, _onMessage);
    connection.on(_statusEvent, _onStatus);
    connection.onreconnecting(({error}) => connected.value = false);
    connection.onreconnected(({connectionId}) => connected.value = true);
    connection.onclose(({error}) {
      connected.value = false;
      if (identical(_connection, connection)) {
        _connection = null;
        _scheduleRetry();
      }
    });

    _connection = connection;
    try {
      await connection.start();
      if (!identical(_connection, connection)) return; // stopped meanwhile
      _attempt = 0;
      connected.value = true;
    } catch (e) {
      debugPrint('Live chat not connected: $e');
      if (identical(_connection, connection)) _connection = null;
      connected.value = false;
      _scheduleRetry();
    }
  }

  /// The built-in reconnect gave up / the first connect failed: try again later (up to every minute)
  void _scheduleRetry() {
    if (!_wanted || _inBackground) return;
    _retry?.cancel();
    final seconds = [2, 5, 10, 30, 60][_attempt.clamp(0, 4)];
    _attempt++;
    _retry = Timer(Duration(seconds: seconds), () => unawaited(_connect()));
  }

  Future<void> _disconnect() async {
    _retry?.cancel();
    final connection = _connection;
    _connection = null;
    connected.value = false;
    if (connection == null) return;
    try {
      await connection.stop();
    } catch (_) {}
  }

  void _onMessage(List<Object?>? args) {
    final json = _map(args);
    if (json == null) return;
    final roomId = '${json['chatRoomId'] ?? json['roomId'] ?? ''}';
    if (roomId.isEmpty) return;
    final message = ChatMessage.fromJson(json);
    _messages.add(ChatLiveMessage(roomId, message));

    // Someone else's message in a chat that is not on screen: it reached my phone (grey double tick for them)
    final me = AuthService.instance.session?.userId ?? '';
    if (message.senderId != me && roomId != activeRoomId && message.id.isNotEmpty) {
      unawaited(ChatService.instance.markDelivered(roomId, [message.id]).catchError((_) {}));
    }
  }

  void _onStatus(List<Object?>? args) {
    final json = _map(args);
    if (json == null) return;
    final ids = json['messageIds'];
    final status = switch ('${json['status']}') {
      '3' || 'Seen' => ChatStatus.seen,
      '2' || 'Delivered' => ChatStatus.delivered,
      _ => null,
    };
    if (ids is! List || status == null) return;
    _statuses.add(ChatLiveStatus(
      roomId: '${json['roomId'] ?? ''}',
      userId: '${json['userId'] ?? ''}',
      messageIds: ids.map((e) => '$e').toSet(),
      status: status,
    ));
  }

  static Map<String, dynamic>? _map(List<Object?>? args) {
    final first = args == null || args.isEmpty ? null : args.first;
    return first is Map ? Map<String, dynamic>.from(first) : null;
  }
}
