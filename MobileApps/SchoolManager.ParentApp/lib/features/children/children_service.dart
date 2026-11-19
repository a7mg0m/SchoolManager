import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// A child of the signed-in parent (GET /api/student/my-children)
class Child {
  const Child({
    required this.id,
    required this.fullNameAr,
    required this.fullNameEn,
    required this.gender,
    required this.stageName,
    required this.gradeName,
    required this.sectionName,
  });

  final String id;
  final String fullNameAr;
  final String fullNameEn;
  final String gender;
  final String stageName;
  final String gradeName;
  final String sectionName;

  /// Name in the app language (the other one when it is missing)
  String get name => pickLang(fullNameAr, fullNameEn);

  bool get isFemale => gender.toLowerCase().startsWith('f');

  String get avatar => isFemale ? '👧🏻' : '👦🏻';

  /// "الصف الأول - فصل أ"
  String get classInfo => [gradeName, sectionName].where((s) => s.trim().isNotEmpty).join(' - ');

  static String _pick(Map<String, dynamic> json, String ar, String en) {
    return pickLang(json[ar] as String?, json[en] as String?);
  }

  factory Child.fromJson(Map<String, dynamic> json) => Child(
        id: '${json['id'] ?? ''}',
        fullNameAr: (json['fullNameAr'] as String?) ?? '',
        fullNameEn: (json['fullNameEn'] as String?) ?? '',
        gender: (json['gender'] as String?) ?? '',
        stageName: _pick(json, 'stageNameAr', 'stageNameEn'),
        gradeName: _pick(json, 'gradeNameAr', 'gradeNameEn'),
        sectionName: _pick(json, 'sectionNameAr', 'sectionNameEn'),
      );
}

/// Why the children could not be loaded; [message] is ready to show (Arabic)
class ChildrenException implements Exception {
  const ChildrenException(this.message, {this.sessionExpired = false});
  final String message;

  /// The token was rejected: the parent has to sign in again
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ChildrenService {
  ChildrenService._();
  static final instance = ChildrenService._();

  /// Children of the signed-in parent; the server takes the parent id from the token
  Future<List<Child>> getMyChildren() async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw ChildrenException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/student/my-children'),
        headers: {
          'Accept': 'application/json',
          'Authorization': 'Bearer ${session.token}',
        },
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ChildrenException(tr('انتهت مهلة الاتصال بالخادم، حاول مرة أخرى.', 'The server took too long to respond. Please try again.'));
    } catch (_) {
      throw ChildrenException(tr('تعذر الاتصال بالخادم، تأكد من اتصالك بالإنترنت.', 'Could not reach the server. Check your internet connection.'));
    }

    if (response.statusCode == 401) {
      throw ChildrenException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
    }
    if (response.statusCode == 403) {
      throw ChildrenException(tr('هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.', 'This account is not linked to a parent. Please contact the school office.'));
    }
    if (response.statusCode != 200) {
      throw ChildrenException(tr('تعذر تحميل بيانات الأبناء، حاول مرة أخرى.', 'Could not load your children. Please try again.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(Child.fromJson).toList();
    } catch (_) {
      throw ChildrenException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }
}
