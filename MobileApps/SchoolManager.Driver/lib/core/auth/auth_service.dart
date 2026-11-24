import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../config.dart';
import '../../core/i18n/app_language.dart';

/// Signed-in user, saved on the device so the next launch opens Home directly
class AuthSession {
  const AuthSession({
    required this.userId,
    required this.loginName,
    required this.token,
    this.firstName,
    this.lastName,
    this.email,
    this.busId,
  });

  final String userId;
  final String loginName;
  final String token;
  final String? firstName;
  final String? lastName;
  final String? email;

  /// Bus (Attendance service) whose driver this account is; null when the account is not a driver
  final String? busId;

  bool get isDriver => busId != null && busId!.trim().isNotEmpty;

  /// Name to greet the driver with (profile name, else login name)
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
        'busId': busId,
      };

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        userId: '${json['userId'] ?? ''}',
        loginName: '${json['loginName'] ?? ''}',
        token: '${json['token'] ?? ''}',
        firstName: json['firstName'] as String?,
        lastName: json['lastName'] as String?,
        email: json['email'] as String?,
        busId: json['busId'] as String?,
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
      busId: _nonEmpty(p['busId']?.toString()) ?? busIdFromToken('${data['token'] ?? ''}'),
    );
  }

  static String? _nonEmpty(String? value) => value == null || value.trim().isEmpty ? null : value.trim();

  /// The bus_id claim of the JWT (written by the Identity service for drivers)
  static String? busIdFromToken(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;
      final payload = jsonDecode(utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
      return payload is Map ? _nonEmpty(payload['bus_id']?.toString()) : null;
    } catch (_) {
      return null;
    }
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
      if (session.token.isEmpty || session.isExpired || !session.isDriver) {
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
          // This app is for bus drivers only: do not keep other accounts signed in
          if (!session.isDriver) {
            throw AuthException(tr('هذا الحساب غير مرتبط بحافلة، تواصل مع إدارة المدرسة.',
                'This account is not linked to a bus. Please contact the school office.'));
          }
          await _save(session);
          return _session = session;
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
      busId: s.busId,
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

    if (keys.contains('UserInactive')) return tr('هذا الحساب غير مفعّل، تواصل مع إدارة المدرسة.', 'This account is not active. Please contact the school office.');
    if (keys.contains('InvalidCredentials')) return tr('اسم المستخدم أو كلمة المرور غير صحيحة.', 'Wrong username or password.');
    if (keys.contains('LoginNameRequired')) return tr('من فضلك أدخل اسم المستخدم.', 'Please enter your username.');
    if (keys.contains('PasswordRequired')) return tr('من فضلك أدخل كلمة المرور.', 'Please enter your password.');
    if (statusCode == 401) return tr('اسم المستخدم أو كلمة المرور غير صحيحة.', 'Wrong username or password.');
    if (statusCode >= 500) return tr('حدث خطأ في الخادم، حاول لاحقاً.', 'A server error occurred. Please try again later.');
    return tr('تعذر تسجيل الدخول، حاول مرة أخرى.', 'Could not sign in. Please try again.');
  }
}
