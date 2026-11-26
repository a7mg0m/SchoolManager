import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../config.dart';
import '../../core/i18n/app_language.dart';
import '../push/push_service.dart';
import '../realtime/chat_realtime.dart';

/// Signed-in user, saved on the device so the next launch opens Home directly
class AuthSession {
  const AuthSession({
    required this.userId,
    required this.loginName,
    required this.token,
    this.firstName,
    this.lastName,
    this.email,
    this.parentId,
  });

  final String userId;
  final String loginName;
  final String token;
  final String? firstName;
  final String? lastName;
  final String? email;

  /// Parent (Management service) this account belongs to; null when the account is not a parent
  final String? parentId;

  /// Name to greet the parent with (profile name, else login name)
  String get displayName {
    final full = [firstName, lastName].where((p) => p != null && p.trim().isNotEmpty).join(' ').trim();
    return full.isNotEmpty ? full : loginName;
  }

  /// Expiry time from the JWT `exp` claim (null if it can't be read)
  DateTime? get expiresAt {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;
      final payload = jsonDecode(utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
      final exp = payload is Map ? payload['exp'] : null;
      return exp is num ? DateTime.fromMillisecondsSinceEpoch(exp.toInt() * 1000, isUtc: true) : null;
    } catch (_) {
      return null;
    }
  }

  bool get isExpired {
    final exp = expiresAt;
    // Unreadable token: treat as expired so the user signs in again
    return exp == null || DateTime.now().toUtc().isAfter(exp.subtract(const Duration(seconds: 30)));
  }

  Map<String, dynamic> toJson() => {
        'userId': userId,
        'loginName': loginName,
        'token': token,
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'parentId': parentId,
      };

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        userId: '${json['userId'] ?? ''}',
        loginName: '${json['loginName'] ?? ''}',
        token: '${json['token'] ?? ''}',
        firstName: json['firstName'] as String?,
        lastName: json['lastName'] as String?,
        email: json['email'] as String?,
        parentId: json['parentId'] as String?,
      );

  /// From the `data` of POST /api/user/login
  factory AuthSession.fromLoginResult(Map<String, dynamic> data) {
    final profile = data['userProfile'];
    final p = profile is Map<String, dynamic> ? profile : const <String, dynamic>{};
    return AuthSession(
      userId: '${data['userId'] ?? ''}',
      loginName: '${data['loginName'] ?? ''}',
      token: '${data['token'] ?? ''}',
      firstName: p['firstName'] as String?,
      lastName: p['lastName'] as String?,
      email: p['email'] as String?,
      parentId: p['parentId'] as String?,
    );
  }
}

/// Why a login failed; [message] is ready to show (Arabic)
class AuthException implements Exception {
  const AuthException(this.message);
  final String message;

  @override
  String toString() => message;
}

/// Login against the Identity service (through the gateway) and keep the session on the device
class AuthService {
  AuthService._();
  static final instance = AuthService._();

  static const _sessionKey = 'auth_session';

  AuthSession? _session;

  /// Why the app signed the user out by itself (e.g. no child at the school any more); the login screen shows it once
  String? _signedOutReason;

  /// The reason, once (then null)
  String? takeSignedOutReason() {
    final reason = _signedOutReason;
    _signedOutReason = null;
    return reason;
  }

  /// At app start: a saved session of a parent whose children have all graduated / been transferred is signed out
  /// (the server would refuse a new sign-in too). Offline / server errors keep the session: the check runs again next time.
  /// Returns false when the user was signed out.
  /// [check] answers: 'ok' (has a current child), 'none' (no current child), 'expired' (token rejected) or
  /// 'unknown' (offline / server error).
  Future<bool> checkStillHasCurrentChildren(Future<String> Function() check) async {
    if (_session == null) return true;
    final String result;
    try {
      result = await check().timeout(const Duration(seconds: 8), onTimeout: () => 'unknown');
    } catch (_) {
      return true;
    }
    switch (result) {
      case 'none':
        _signedOutReason = tr('لا يوجد أبناء مسجلون حالياً في المدرسة لهذا الحساب، تم تسجيل خروجك.',
            'This account has no children currently enrolled at the school, so you have been signed out.');
      case 'expired':
        _signedOutReason = tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
      default:
        // Has a child, or could not be checked (offline): keep the session
        return true;
    }
    await logout();
    return false;
  }
  AuthSession? get session => _session;

  /// Load the saved session at startup; drops it when the token has expired
  Future<AuthSession?> restoreSession() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final raw = prefs.getString(_sessionKey);
      if (raw == null) return null;

      final session = AuthSession.fromJson(jsonDecode(raw) as Map<String, dynamic>);
      if (session.token.isEmpty || session.isExpired) {
        await prefs.remove(_sessionKey);
        return null;
      }
      return _session = session;
    } catch (_) {
      return null;
    }
  }

  Future<AuthSession> login(String loginName, String password) async {
    final uri = Uri.parse('${AppConfig.apiBaseUrl}/api/user/login');

    final http.Response response;
    try {
      response = await http
          .post(
            uri,
            headers: const {'Content-Type': 'application/json', 'Accept': 'application/json'},
            body: jsonEncode({'loginName': loginName.trim(), 'password': password}),
          )
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw AuthException(tr('انتهت مهلة الاتصال بالخادم، حاول مرة أخرى.', 'The server took too long to respond. Please try again.'));
    } catch (_) {
      throw AuthException(tr('تعذر الاتصال بالخادم، تأكد من اتصالك بالإنترنت.', 'Could not reach the server. Check your internet connection.'));
    }

    Map<String, dynamic>? body;
    try {
      final decoded = jsonDecode(utf8.decode(response.bodyBytes));
      if (decoded is Map<String, dynamic>) body = decoded;
    } catch (_) {}

    if (response.statusCode == 200) {
      final data = body?['data'];
      if (data is Map<String, dynamic>) {
        final session = AuthSession.fromLoginResult(data);
        if (session.token.isNotEmpty) {
          await _save(session);
          _session = session;
          // Push: this device now receives the user's notifications
          unawaited(PushService.instance.registerCurrentUser());
          // Live chat: messages and ticks without a refresh
          unawaited(ChatRealtime.instance.start());
          return session;
        }
      }
      throw AuthException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }

    throw AuthException(_errorMessage(response.statusCode, body));
  }

  /// Keeps the saved name and email in step with the profile (after an edit), so the next launch shows them
  Future<void> updateProfileDetails({required String firstName, required String lastName, required String email}) async {
    final s = _session;
    if (s == null) return;
    if (s.firstName == firstName && s.lastName == lastName && s.email == email) return;
    final updated = AuthSession(
      userId: s.userId,
      loginName: s.loginName,
      token: s.token,
      firstName: firstName,
      lastName: lastName,
      email: email,
      parentId: s.parentId,
    );
    _session = updated;
    try {
      await _save(updated);
    } catch (_) {
      // Only the next launch's first frame depends on it; the profile is reloaded anyway
    }
  }

  Future<void> logout() async {
    // While the session is still there: the server forgets this device for the user
    await PushService.instance.unregisterCurrentUser();
    await ChatRealtime.instance.stop();
    _session = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_sessionKey);
    } catch (_) {}
  }

  Future<void> _save(AuthSession session) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_sessionKey, jsonEncode(session.toJson()));
  }

  /// The API puts the reason key in `errors` (e.g. UserInactive) or `message` (InvalidCredentials)
  String _errorMessage(int statusCode, Map<String, dynamic>? body) {
    final keys = <String>[];
    final errors = body?['errors'];
    if (errors is String) keys.add(errors);
    if (errors is List) keys.addAll(errors.whereType<String>());
    final message = body?['message'];
    if (message is String) keys.add(message);

    if (keys.contains('UserInactive')) return tr('هذا الحساب غير مفعّل، تواصل مع إدارة المدرسة.', 'This account is not active. Please contact the school office.');
    if (keys.contains('NoCurrentStudents')) {
      return tr('لا يوجد أبناء مسجلون حالياً في المدرسة لهذا الحساب.', 'This account has no children currently enrolled at the school.');
    }
    if (keys.contains('ParentCheckUnavailable')) {
      return tr('تعذر التحقق من بيانات أبنائك الآن، حاول لاحقاً.', "Your children's details could not be checked right now. Please try again later.");
    }
    if (keys.contains('InvalidCredentials')) return tr('اسم المستخدم أو كلمة المرور غير صحيحة.', 'Wrong username or password.');
    if (keys.contains('LoginNameRequired')) return tr('من فضلك أدخل اسم المستخدم.', 'Please enter your username.');
    if (keys.contains('PasswordRequired')) return tr('من فضلك أدخل كلمة المرور.', 'Please enter your password.');
    if (statusCode == 401) return tr('اسم المستخدم أو كلمة المرور غير صحيحة.', 'Wrong username or password.');
    if (statusCode >= 500) return tr('حدث خطأ في الخادم، حاول لاحقاً.', 'A server error occurred. Please try again later.');
    return tr('تعذر تسجيل الدخول، حاول مرة أخرى.', 'Could not sign in. Please try again.');
  }
}
