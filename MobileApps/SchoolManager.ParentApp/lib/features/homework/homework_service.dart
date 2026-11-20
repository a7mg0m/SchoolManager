import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/academic_year/academic_year_controller.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// A question of a homework: header and optional items
class HomeworkQuestion {
  const HomeworkQuestion({required this.header, this.items = const []});

  final String header;
  final List<String> items;

  factory HomeworkQuestion.fromJson(Map<String, dynamic> json) => HomeworkQuestion(
        header: (json['header'] as String?) ?? '',
        items: (json['items'] as List? ?? const []).map((e) => '$e').toList(),
      );
}

/// A homework the teacher published to the child's class (GET /api/homework/my-child/{studentId})
class ChildHomework {
  const ChildHomework({
    required this.id,
    required this.subjectNameAr,
    required this.subjectNameEn,
    required this.title,
    required this.description,
    required this.dueDate,
    required this.publishedOn,
    this.questions = const [],
  });

  final String id;
  final String subjectNameAr;
  final String subjectNameEn;
  final String title;
  final String description;
  final DateTime dueDate;
  final DateTime publishedOn;
  final List<HomeworkQuestion> questions;

  String get subject {
    final name = pickLang(subjectNameAr, subjectNameEn);
    return name.isNotEmpty ? name : tr('مادة', 'Subject');
  }

  /// Days until the due date (0 = today, negative = past)
  int daysLeft(DateTime now) => dueDate.difference(DateTime(now.year, now.month, now.day)).inDays;

  /// Still to hand in (due today or later)
  bool isOpen(DateTime now) => daysLeft(now) >= 0;

  factory ChildHomework.fromJson(Map<String, dynamic> json) {
    final due = DateTime.tryParse('${json['dueDate']}') ?? DateTime.now();
    return ChildHomework(
      id: '${json['id'] ?? ''}',
      subjectNameAr: (json['subjectNameAr'] as String?) ?? '',
      subjectNameEn: (json['subjectNameEn'] as String?) ?? '',
      title: (json['title'] as String?) ?? '',
      description: (json['description'] as String?) ?? '',
      dueDate: DateTime(due.year, due.month, due.day),
      publishedOn: DateTime.tryParse('${json['publishedOn']}')?.toLocal() ?? DateTime.now(),
      questions: (json['questions'] as List? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(HomeworkQuestion.fromJson)
          .toList(),
    );
  }
}

/// Loading the homework failed; [message] is ready to show
class HomeworkException implements Exception {
  const HomeworkException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class HomeworkService {
  HomeworkService._();
  static final instance = HomeworkService._();

  Future<List<ChildHomework>> getChildHomeworks(String studentId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw HomeworkException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'),
          sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        AcademicYearController.instance.withYear(
            Uri.parse('${AppConfig.apiBaseUrl}/api/homework/my-child/${Uri.encodeComponent(studentId)}')),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw HomeworkException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw HomeworkException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw HomeworkException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'),
            sessionExpired: true);
      case 503:
        throw HomeworkException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
      default:
        throw HomeworkException(tr('تعذر تحميل الواجبات.', 'Could not load the homework.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(ChildHomework.fromJson).toList();
    } catch (_) {
      throw HomeworkException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }
}
