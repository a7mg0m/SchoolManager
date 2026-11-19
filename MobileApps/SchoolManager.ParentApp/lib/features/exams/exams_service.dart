import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../schedule/schedule_format.dart';
import '../../core/i18n/app_language.dart';

export '../schedule/schedule_format.dart' show monthNames;

/// Exam types (ExamsHomework ExamType)
enum ExamKind {
  quiz(1, 'اختبار قصير', 'اختبارات قصيرة', 'Quiz', 'Quizzes'),
  monthly(2, 'امتحان شهري', 'امتحانات شهرية', 'Monthly exam', 'Monthly exams'),
  midTerm(3, 'منتصف الفصل', 'امتحانات منتصف الفصل', 'Mid-term', 'Mid-term exams'),
  finalExam(4, 'نهائي', 'امتحانات نهائية', 'Final', 'Final exams');

  const ExamKind(this.value, this._labelAr, this._pluralAr, this._labelEn, this._pluralEn);

  /// Value sent by the API (Quiz = 1 … Final = 4)
  final int value;
  final String _labelAr;
  final String _pluralAr;
  final String _labelEn;
  final String _pluralEn;

  String get label => tr(_labelAr, _labelEn);
  String get plural => tr(_pluralAr, _pluralEn);

  static ExamKind from(Object? value) {
    if (value is num) {
      return ExamKind.values.firstWhere((k) => k.value == value.toInt(), orElse: () => ExamKind.quiz);
    }
    return switch ('$value'.toLowerCase()) {
      'monthly' => ExamKind.monthly,
      'midterm' => ExamKind.midTerm,
      'final' => ExamKind.finalExam,
      _ => ExamKind.quiz,
    };
  }
}

/// An exam of the child, with its own result once entered (GET /api/exam/my-child/{studentId})
class ChildExam {
  const ChildExam({
    required this.id,
    required this.kind,
    this.month,
    required this.subject,
    required this.date,
    required this.start,
    required this.end,
    required this.duration,
    required this.fullMark,
    required this.passMark,
    this.score,
    this.isPassed,
    this.percentage,
  });

  final String id;
  final ExamKind kind;

  /// Month of a Monthly exam (1..12); null for the other types
  final int? month;

  /// "نوفمبر" (null without a month)
  String? get monthName => month != null && month! >= 1 && month! <= 12 ? monthNames[month! - 1] : null;
  final String subject;

  /// Day of the exam (no time)
  final DateTime date;

  /// Minutes since midnight
  final int start;
  final int end;

  /// Minutes
  final int duration;
  final double fullMark;
  final double passMark;
  final double? score;
  final bool? isPassed;
  final double? percentage;

  bool get hasResult => score != null;

  /// The exam has not finished yet (today before it ends, or a later day)
  bool isUpcoming(DateTime now) {
    final today = DateTime(now.year, now.month, now.day);
    if (date.isAfter(today)) return true;
    if (date.isBefore(today)) return false;
    return now.hour * 60 + now.minute < end;
  }

  factory ChildExam.fromJson(Map<String, dynamic> json) {
    final ar = (json['subjectNameAr'] as String?)?.trim() ?? '';
    final en = (json['subjectNameEn'] as String?)?.trim() ?? '';
    final date = DateTime.tryParse('${json['examDate']}') ?? DateTime.now();
    return ChildExam(
      id: '${json['id'] ?? ''}',
      kind: ExamKind.from(json['examType']),
      month: (json['month'] as num?)?.toInt(),
      subject: pickLang(ar, en).isNotEmpty ? pickLang(ar, en) : tr('مادة', 'Subject'),
      date: DateTime(date.year, date.month, date.day),
      start: _minutes(json['startTime']),
      end: _minutes(json['endTime']),
      duration: _num(json['duration'])?.round() ?? 0,
      fullMark: _num(json['weight']) ?? 0,
      passMark: _num(json['successScore']) ?? 0,
      score: _num(json['score']),
      isPassed: json['isPassed'] as bool?,
      percentage: _num(json['percentage']),
    );
  }
}

/// Loading the exams failed; [message] is ready to show (Arabic)
class ExamsException implements Exception {
  const ExamsException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ExamsService {
  ExamsService._();
  static final instance = ExamsService._();

  Future<List<ChildExam>> getChildExams(String studentId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw ExamsException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/exam/my-child/${Uri.encodeComponent(studentId)}'),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ExamsException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ExamsException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw ExamsException(tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'), sessionExpired: true);
      case 503:
        throw ExamsException(tr('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.', 'The data service is not available right now. Please try later.'));
      default:
        throw ExamsException(tr('تعذر تحميل الاختبارات.', 'Could not load the exams.'));
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(ChildExam.fromJson).toList();
    } catch (_) {
      throw ExamsException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }
}

double? _num(Object? value) => value is num ? value.toDouble() : (value == null ? null : double.tryParse('$value'));

/// "08:30:00" -> 510
int _minutes(Object? value) {
  final parts = '$value'.split(':');
  if (parts.length < 2) return 0;
  return (int.tryParse(parts[0]) ?? 0) * 60 + (int.tryParse(parts[1]) ?? 0);
}
