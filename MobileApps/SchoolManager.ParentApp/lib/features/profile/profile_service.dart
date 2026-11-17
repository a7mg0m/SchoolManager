import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';

/// Profile of the signed-in user (GET /api/user-profile/my-profile)
class MyProfile {
  const MyProfile({
    required this.firstName,
    required this.lastName,
    required this.loginName,
    this.email = '',
    this.phoneNumber = '',
    this.address = '',
    this.image,
  });

  final String firstName;
  final String lastName;
  final String loginName;
  final String email;
  final String phoneNumber;
  final String address;

  /// Profile picture bytes, or null when the user has none
  final Uint8List? image;

  String get displayName {
    final full = '$firstName $lastName'.trim();
    return full.isNotEmpty ? full : loginName;
  }

  /// Parents without an email get an internal placeholder address (…@parents.local); it is not shown
  bool get hasPlaceholderEmail => email.toLowerCase().endsWith('@parents.local');

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

  /// Null when the profile could not be loaded (the home screen then keeps the default avatar)
  Future<MyProfile?> getMyProfile() async {
    try {
      final response = await _send('GET', '/api/user-profile/my-profile');
      if (response.statusCode != 200) return null;
      final data = _data(response);
      return data is Map<String, dynamic> ? MyProfile.fromJson(data) : null;
    } on SessionExpiredException {
      rethrow;
    } catch (_) {
      return null;
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
      if (data is Map<String, dynamic>) return MyProfile.fromJson(data);
    }
    throw ProfileException(_errorMessage(response, 'تعذر حفظ الملف الشخصي، حاول مرة أخرى.'));
  }

  Future<void> changePassword(String currentPassword, String newPassword) async {
    final response = await _sendOrThrow('PUT', '/api/user/change-password', {
      'currentPassword': currentPassword,
      'newPassword': newPassword,
    });
    if (response.statusCode != 200) {
      throw ProfileException(_errorMessage(response, 'تعذر تغيير كلمة المرور، حاول مرة أخرى.'));
    }
  }

  /// Like [_send], but network problems become a [ProfileException]
  Future<http.Response> _sendOrThrow(String method, String path, Map<String, dynamic> body) async {
    try {
      return await _send(method, path, body);
    } on SessionExpiredException {
      rethrow;
    } on TimeoutException {
      throw const ProfileException('انتهت مهلة الاتصال بالخادم، حاول مرة أخرى.');
    } catch (_) {
      throw const ProfileException('تعذر الاتصال بالخادم، تأكد من اتصالك بالإنترنت.');
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
      'FirstNameRequired' => 'من فضلك أدخل الاسم الأول.',
      'LastNameRequired' => 'من فضلك أدخل اسم العائلة.',
      'EmailRequired' => 'من فضلك أدخل البريد الإلكتروني.',
      'EmailInvalid' => 'البريد الإلكتروني غير صحيح.',
      'EmailExists' => 'البريد الإلكتروني مستخدم بالفعل لحساب آخر.',
      'ProfileImageTooLarge' => 'الصورة كبيرة جداً، اختر صورة أصغر.',
      'CurrentPasswordRequired' => 'من فضلك أدخل كلمة المرور الحالية.',
      'CurrentPasswordInvalid' => 'كلمة المرور الحالية غير صحيحة.',
      'NewPasswordSameAsCurrent' => 'كلمة المرور الجديدة يجب أن تختلف عن الحالية.',
      'PasswordRequired' => 'من فضلك أدخل كلمة المرور الجديدة.',
      'PasswordTooShort' => 'كلمة المرور يجب ألا تقل عن 6 أحرف.',
      _ => response.statusCode >= 500 ? 'حدث خطأ في الخادم، حاول لاحقاً.' : fallback,
    };
  }
}
