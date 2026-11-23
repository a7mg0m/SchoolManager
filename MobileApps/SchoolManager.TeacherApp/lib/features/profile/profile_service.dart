import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// Profile of the signed-in user (GET /api/user-profile/my-profile)
class MyProfile {
  const MyProfile({
    required this.firstName,
    required this.lastName,
    required this.loginName,
    this.email = '',
    this.phoneNumber = '',
    this.address = '',
    this.jobLevel = '',
    this.image,
  });

  final String firstName;
  final String lastName;
  final String loginName;
  final String email;
  final String phoneNumber;
  final String address;

  /// Job level (المستوى الوظيفي) set by the school in the admin dashboard; read only here, '' when not set
  final String jobLevel;

  /// Profile picture bytes, or null when the user has none
  final Uint8List? image;

  String get displayName {
    final full = '$firstName $lastName'.trim();
    return full.isNotEmpty ? full : loginName;
  }

  /// Accounts without an email get an internal placeholder address (…@teachers.local / …@parents.local); it is not shown
  bool get hasPlaceholderEmail {
    final e = email.toLowerCase();
    return e.endsWith('@teachers.local') || e.endsWith('@parents.local');
  }

  MyProfile withJobLevel(String value) => MyProfile(
        firstName: firstName,
        lastName: lastName,
        loginName: loginName,
        email: email,
        phoneNumber: phoneNumber,
        address: address,
        jobLevel: value,
        image: image,
      );

  factory MyProfile.fromJson(Map<String, dynamic> json) => MyProfile(
        firstName: (json['firstName'] as String?)?.trim() ?? '',
        lastName: (json['lastName'] as String?)?.trim() ?? '',
        loginName: (json['loginName'] as String?)?.trim() ?? '',
        email: (json['email'] as String?)?.trim() ?? '',
        phoneNumber: (json['phoneNumber'] as String?)?.trim() ?? '',
        address: (json['address'] as String?)?.trim() ?? '',
        image: _decodeImage(json['profileImage']),
      );

  /// The API sends the image (byte[]) as base64; a data URL is accepted too
  static Uint8List? _decodeImage(Object? value) {
    if (value is! String || value.isEmpty) return null;
    try {
      final base64 = value.startsWith('data:') ? value.substring(value.indexOf(',') + 1) : value;
      final bytes = base64Decode(base64);
      return bytes.isEmpty ? null : bytes;
    } catch (_) {
      return null;
    }
  }
}

/// The token was rejected: the user has to sign in again
class SessionExpiredException implements Exception {
  const SessionExpiredException();
}

/// A save failed; [message] is ready to show (Arabic)
class ProfileException implements Exception {
  const ProfileException(this.message);
  final String message;

  @override
  String toString() => message;
}

class ProfileService {
  ProfileService._();
  static final instance = ProfileService._();

  /// Largest picture the API accepts
  static const maxImageBytes = 1024 * 1024;

  /// Last job level read from the teacher record (kept on the profile returned after a save)
  String _jobLevel = '';

  /// Null when the profile could not be loaded (the home screen then keeps the default avatar).
  /// The job level comes from the teacher record (GET /api/teacher/my-profile).
  Future<MyProfile?> getMyProfile() async {
    try {
      final results = await Future.wait([_send('GET', '/api/user-profile/my-profile'), _loadJobLevel()]);
      final response = results[0] as http.Response;
      _jobLevel = results[1] as String;
      if (response.statusCode != 200) return null;
      final data = _data(response);
      return data is Map<String, dynamic> ? MyProfile.fromJson(data).withJobLevel(_jobLevel) : null;
    } on SessionExpiredException {
      rethrow;
    } catch (_) {
      return null;
    }
  }

  /// '' when it is not set or cannot be read (the profile still loads)
  Future<String> _loadJobLevel() async {
    try {
      final response = await _send('GET', '/api/teacher/my-profile');
      if (response.statusCode != 200) return '';
      final data = _data(response);
      return data is Map<String, dynamic> ? (data['jobLevel'] as String?)?.trim() ?? '' : '';
    } on SessionExpiredException {
      rethrow;
    } catch (_) {
      return '';
    }
  }

  /// Saves name / contact details / picture; returns the saved profile.
  /// [image] null keeps the current picture, [removeImage] deletes it.
  Future<MyProfile> updateMyProfile({
    required String firstName,
    required String lastName,
    required String email,
    required String phoneNumber,
    required String address,
    Uint8List? image,
    bool removeImage = false,
  }) async {
    final response = await _sendOrThrow('PUT', '/api/user-profile/update-my-profile', {
      'firstName': firstName.trim(),
      'lastName': lastName.trim(),
      'email': email.trim(),
      'phoneNumber': phoneNumber.trim(),
      'address': address.trim(),
      'profileImage': image == null ? null : base64Encode(image),
      'removeProfileImage': removeImage,
    });

    if (response.statusCode == 200) {
      final data = _data(response);
      if (data is Map<String, dynamic>) return MyProfile.fromJson(data).withJobLevel(_jobLevel);
    }
    throw ProfileException(_errorMessage(response, tr('تعذر حفظ الملف الشخصي، حاول مرة أخرى.', 'Could not save your profile. Please try again.')));
  }

  Future<void> changePassword(String currentPassword, String newPassword) async {
    final response = await _sendOrThrow('PUT', '/api/user/change-password', {
      'currentPassword': currentPassword,
      'newPassword': newPassword,
    });
    if (response.statusCode != 200) {
      throw ProfileException(_errorMessage(response, tr('تعذر تغيير كلمة المرور، حاول مرة أخرى.', 'Could not change your password. Please try again.')));
    }
  }

  /// Like [_send], but network problems become a [ProfileException]
  Future<http.Response> _sendOrThrow(String method, String path, Map<String, dynamic> body) async {
    try {
      return await _send(method, path, body);
    } on SessionExpiredException {
      rethrow;
    } on TimeoutException {
      throw ProfileException(tr('انتهت مهلة الاتصال بالخادم، حاول مرة أخرى.', 'The server took too long to respond. Please try again.'));
    } catch (_) {
      throw ProfileException(tr('تعذر الاتصال بالخادم، تأكد من اتصالك بالإنترنت.', 'Could not reach the server. Check your internet connection.'));
    }
  }

  Future<http.Response> _send(String method, String path, [Map<String, dynamic>? body]) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw const SessionExpiredException();

    final uri = Uri.parse('${AppConfig.apiBaseUrl}$path');
    final headers = {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ${session.token}',
    };

    final response = await (method == 'PUT'
            ? http.put(uri, headers: headers, body: jsonEncode(body))
            : http.get(uri, headers: headers))
        .timeout(AppConfig.requestTimeout);

    if (response.statusCode == 401) throw const SessionExpiredException();
    return response;
  }

  static Object? _data(http.Response response) {
    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      return body is Map<String, dynamic> ? body['data'] : null;
    } catch (_) {
      return null;
    }
  }

  /// The API returns an error key in `message` (e.g. EmailExists)
  static String _errorMessage(http.Response response, String fallback) {
    String? key;
    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      if (body is Map<String, dynamic>) key = body['message'] as String?;
    } catch (_) {}

    return switch (key) {
      'FirstNameRequired' => tr('من فضلك أدخل الاسم الأول.', 'Please enter your first name.'),
      'LastNameRequired' => tr('من فضلك أدخل اسم العائلة.', 'Please enter your last name.'),
      'EmailRequired' => tr('من فضلك أدخل البريد الإلكتروني.', 'Please enter your email.'),
      'EmailInvalid' => tr('البريد الإلكتروني غير صحيح.', 'Invalid email address.'),
      'EmailExists' => tr('البريد الإلكتروني مستخدم بالفعل لحساب آخر.', 'This email is already used by another account.'),
      'ProfileImageTooLarge' => tr('الصورة كبيرة جداً، اختر صورة أصغر.', 'The photo is too large. Choose a smaller one.'),
      'CurrentPasswordRequired' => tr('من فضلك أدخل كلمة المرور الحالية.', 'Please enter your current password.'),
      'CurrentPasswordInvalid' => tr('كلمة المرور الحالية غير صحيحة.', 'The current password is wrong.'),
      'NewPasswordSameAsCurrent' => tr('كلمة المرور الجديدة يجب أن تختلف عن الحالية.', 'The new password must be different from the current one.'),
      'PasswordRequired' => tr('من فضلك أدخل كلمة المرور الجديدة.', 'Please enter the new password.'),
      'PasswordTooShort' => tr('كلمة المرور يجب ألا تقل عن 6 أحرف.', 'Password must be at least 6 characters.'),
      _ => response.statusCode >= 500 ? tr('حدث خطأ في الخادم، حاول لاحقاً.', 'A server error occurred. Please try again later.') : fallback,
    };
  }
}
