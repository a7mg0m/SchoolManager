import 'dart:async';
import 'dart:convert';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:http/http.dart' as http;

import '../auth/auth_service.dart';
import '../config.dart';
import '../i18n/app_language.dart';
import '../realtime/chat_realtime.dart';
import '../live/live_refresh.dart';

/// Push messages that arrive while the app is closed / in the background. Firebase shows the notification itself;
/// the tap is handled by [PushService] (onMessageOpenedApp / getInitialMessage) when the app opens.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // TODO(firebase): after `flutterfire configure`, pass DefaultFirebaseOptions.currentPlatform here and in init().
  await Firebase.initializeApp();
}

/// Push notifications (Firebase Cloud Messaging):
/// - asks permission (Android 13+ / iOS) and registers the device token with the server (POST /api/devices/register)
///   after login and on app start when already signed in, and again when Firebase renews the token;
/// - shows messages that arrive while the app is open (local notification on the "school_notifications" channel);
/// - a tapped notification puts its `route` (e.g. /chat/{roomId}, /notifications/{id}) in [openRoute] for the
///   home screen to open;
/// - at logout the token is removed from the server and from Firebase.
/// Without Firebase configuration (google-services.json / GoogleService-Info.plist) or on the web, push is disabled
/// and every method does nothing.
class PushService {
  PushService._();
  static final instance = PushService._();

  static const channelId = 'school_notifications';

  /// Route of a tapped notification, for the home screen to open (it sets it back to null)
  final openRoute = ValueNotifier<String?>(null);

  /// "parent" | "teacher" | "driver"
  String _appName = '';
  bool _ready = false;
  String? _registeredToken;
  final _local = FlutterLocalNotificationsPlugin();
  StreamSubscription<String>? _tokenRefresh;

  bool get isEnabled => _ready;

  /// Call once in main() before runApp
  Future<void> init({required String appName}) async {
    _appName = appName;
    if (kIsWeb || _ready) return;
    try {
      // TODO(firebase): run `flutterfire configure` and use Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform)
      await Firebase.initializeApp();
    } catch (e) {
      debugPrint('Push disabled: Firebase is not configured ($e)');
      return;
    }

    try {
      FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

      await _local.initialize(
        settings: const InitializationSettings(
          android: AndroidInitializationSettings('@mipmap/ic_launcher'),
          iOS: DarwinInitializationSettings(
            requestAlertPermission: false,
            requestBadgePermission: false,
            requestSoundPermission: false,
          ),
        ),
        onDidReceiveNotificationResponse: (response) => _open(response.payload),
      );
      await _local
          .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
          ?.createNotificationChannel(AndroidNotificationChannel(
            channelId,
            tr('إشعارات المدرسة', 'School notifications'),
            description: tr('إشعارات ورسائل المدرسة', 'School notifications and messages'),
            importance: Importance.high,
          ));

      // iOS shows notifications itself while the app is open; Android gets a local notification (below)
      await FirebaseMessaging.instance.setForegroundNotificationPresentationOptions(alert: true, badge: true, sound: true);

      FirebaseMessaging.onMessage.listen(_showForeground);
      FirebaseMessaging.onMessageOpenedApp.listen((m) => _open(m.data['route']));
      final initial = await FirebaseMessaging.instance.getInitialMessage();
      if (initial != null) _open(initial.data['route']);

      _ready = true;
    } catch (e) {
      debugPrint('Push disabled: $e');
    }
  }

  /// Registers this device for the signed-in user (after login and on app start). Never throws.
  Future<void> registerCurrentUser() async {
    if (!_ready || AuthService.instance.session == null) return;
    try {
      await FirebaseMessaging.instance.requestPermission(alert: true, badge: true, sound: true);
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) await _register(token);

      _tokenRefresh ??= FirebaseMessaging.instance.onTokenRefresh.listen((t) => _register(t));
    } catch (e) {
      debugPrint('Push registration failed: $e');
    }
  }

  /// Logout: the server forgets this device for the user and Firebase drops the token. Never throws.
  Future<void> unregisterCurrentUser() async {
    if (!_ready) return;
    try {
      final token = _registeredToken ?? await FirebaseMessaging.instance.getToken();
      final session = AuthService.instance.session;
      if (token != null && session != null && !session.isExpired) {
        await http
            .delete(
              Uri.parse('${AppConfig.apiBaseUrl}/api/devices/${Uri.encodeComponent(token)}'),
              headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
            )
            .timeout(AppConfig.requestTimeout);
      }
    } catch (e) {
      debugPrint('Push unregister failed: $e');
    }
    try {
      await FirebaseMessaging.instance.deleteToken();
    } catch (_) {}
    _registeredToken = null;
  }

  Future<void> _register(String token) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) return;
    try {
      final response = await http
          .post(
            Uri.parse('${AppConfig.apiBaseUrl}/api/devices/register'),
            headers: {
              'Accept': 'application/json',
              'Content-Type': 'application/json',
              'Authorization': 'Bearer ${session.token}',
            },
            body: jsonEncode({'token': token, 'platform': _platform, 'appName': _appName}),
          )
          .timeout(AppConfig.requestTimeout);
      if (response.statusCode == 200) _registeredToken = token;
    } catch (e) {
      debugPrint('Push token not sent: $e');
    }
  }

  /// Server values: 1 Android, 2 iOS, 3 Web
  int get _platform => kIsWeb
      ? 3
      : defaultTargetPlatform == TargetPlatform.iOS || defaultTargetPlatform == TargetPlatform.macOS
          ? 2
          : 1;

  /// A message while the app is open: Android shows it as a local notification (iOS shows it itself).
  /// Not for a chat message of the chat on screen: it already appeared there live.
  Future<void> _showForeground(RemoteMessage message) async {
    // Something was saved for this user: the open page reloads (quietly)
    LiveRefresh.instance.ping();
    final n = message.notification;
    if (n == null || defaultTargetPlatform != TargetPlatform.android) return;
    final active = ChatRealtime.instance.activeRoomId;
    if (message.data['type'] == 'chat' && active != null && message.data['roomId'] == active) return;
    await _local.show(
      id: message.messageId?.hashCode ?? DateTime.now().millisecondsSinceEpoch.remainder(1 << 31),
      title: n.title,
      body: n.body,
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          channelId,
          tr('إشعارات المدرسة', 'School notifications'),
          importance: Importance.high,
          priority: Priority.high,
        ),
      ),
      payload: message.data['route'] as String?,
    );
  }

  void _open(Object? route) {
    final value = route is String ? route.trim() : '';
    if (value.isNotEmpty) openRoute.value = value;
  }
}
