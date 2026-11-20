import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// A child of the parent who is in one of the teacher's classes
class ParentChildInfo {
  const ParentChildInfo({
    required this.studentId,
    required this.nameAr,
    required this.nameEn,
    required this.sectionId,
    this.sectionNameAr = '',
    this.sectionNameEn = '',
    this.gradeNameAr = '',
    this.gradeNameEn = '',
  });

  final String studentId;
  final String nameAr;
  final String nameEn;
  final String sectionId;
  final String sectionNameAr;
  final String sectionNameEn;
  final String gradeNameAr;
  final String gradeNameEn;

  String get name => pickLang(nameAr, nameEn);
  String get sectionName => pickLang(sectionNameAr, sectionNameEn);
  String get gradeName => pickLang(gradeNameAr, gradeNameEn);

  /// "الصف الأول - فصل أ"
  String get classLabel => [gradeName, sectionName].where((s) => s.isNotEmpty).join(' - ');

  factory ParentChildInfo.fromJson(Map<String, dynamic> json) => ParentChildInfo(
        studentId: '${json['studentId'] ?? ''}',
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        sectionId: '${json['sectionId'] ?? ''}',
        sectionNameAr: (json['sectionNameAr'] as String?) ?? '',
        sectionNameEn: (json['sectionNameEn'] as String?) ?? '',
        gradeNameAr: (json['gradeNameAr'] as String?) ?? '',
        gradeNameEn: (json['gradeNameEn'] as String?) ?? '',
      );
}

/// A parent of students in the teacher's classes (GET /api/teacher/my-parents)
class TeacherParent {
  const TeacherParent({
    required this.id,
    required this.nameAr,
    required this.nameEn,
    this.phoneNumber = '',
    this.email,
    this.children = const [],
  });

  final String id;
  final String nameAr;
  final String nameEn;
  final String phoneNumber;
  final String? email;
  final List<ParentChildInfo> children;

  String get name => pickLang(nameAr, nameEn);

  /// Matches the parent's or a child's name (Arabic or English) or the phone number
  bool matches(String query) {
    final q = query.trim().toLowerCase();
    if (q.isEmpty) return true;
    bool has(String s) => s.toLowerCase().contains(q);
    return has(nameAr) || has(nameEn) || has(phoneNumber) || children.any((c) => has(c.nameAr) || has(c.nameEn));
  }

  factory TeacherParent.fromJson(Map<String, dynamic> json) {
    final email = (json['email'] as String?)?.trim();
    return TeacherParent(
      id: '${json['id'] ?? ''}',
      nameAr: (json['nameAr'] as String?)?.trim() ?? '',
      nameEn: (json['nameEn'] as String?)?.trim() ?? '',
      phoneNumber: (json['phoneNumber'] as String?)?.trim() ?? '',
      email: email == null || email.isEmpty ? null : email,
      children: (json['children'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(ParentChildInfo.fromJson)
          .toList(),
    );
  }
}

/// Loading failed; [message] is ready to show
class ParentsException implements Exception {
  const ParentsException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ParentsService {
  ParentsService._();
  static final instance = ParentsService._();

  Future<List<TeacherParent>> getMyParents() async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw ParentsException(_sessionEnded, sessionExpired: true);

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/teacher/my-parents'),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ParentsException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ParentsException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw ParentsException(_sessionEnded, sessionExpired: true);
      case 403:
        throw ParentsException(tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'));
      default:
        throw ParentsException(tr('تعذر تحميل أولياء الأمور.', 'Could not load the parents.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(TeacherParent.fromJson).toList();
    } catch (_) {
      throw ParentsException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
