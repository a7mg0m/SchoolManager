import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../config.dart';

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
      throw const AuthException('انتهت مهلة الاتصال بالخادم، حاول مرة أخرى.');
    } catch (_) {
      throw const AuthException('تعذر الاتصال بالخادم، تأكد من اتصالك بالإنترنت.');
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
          return _session = session;
        }
      }
      throw const AuthException('استجابة غير متوقعة من الخادم.');
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

    if (keys.contains('UserInactive')) return 'هذا الحساب غير مفعّل، تواصل مع إدارة المدرسة.';
    if (keys.contains('InvalidCredentials')) return 'اسم المستخدم أو كلمة المرور غير صحيحة.';
    if (keys.contains('LoginNameRequired')) return 'من فضلك أدخل اسم المستخدم.';
    if (keys.contains('PasswordRequired')) return 'من فضلك أدخل كلمة المرور.';
    if (statusCode == 401) return 'اسم المستخدم أو كلمة المرور غير صحيحة.';
    if (statusCode >= 500) return 'حدث خطأ في الخادم، حاول لاحقاً.';
    return 'تعذر تسجيل الدخول، حاول مرة أخرى.';
  }
}
