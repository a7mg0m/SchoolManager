import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';
import '../attendance/attendance_service.dart' show attendanceDate;
import '../timetable/teacher_timetable_service.dart';

/// Rating 1..5 (SchoolManager.Attendance.Domain.EvaluationRating)
String ratingLabel(int rating) => switch (rating) {
      1 => tr('ضعيف', 'Weak'),
      2 => tr('مقبول', 'Acceptable'),
      3 => tr('جيد', 'Good'),
      4 => tr('جيد جداً', 'Very good'),
      _ => tr('ممتاز', 'Excellent'),
    };

/// SchoolManager.Attendance.Domain.HealthStatus (sent as its number)
enum StudentHealth {
  good(0),
  tired(1),
  sick(2);

  const StudentHealth(this.code);
  final int code;

  static StudentHealth fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return StudentHealth.values.firstWhere((s) => s.code == code, orElse: () => StudentHealth.good);
  }

  String get label => switch (this) {
        StudentHealth.good => tr('جيدة', 'Good'),
        StudentHealth.tired => tr('متعب', 'Tired'),
        StudentHealth.sick => tr('مريض', 'Sick'),
      };
}

/// The API sends UTC times without a zone
DateTime? _utc(Object? value) {
  if (value == null) return null;
  final text = '$value';
  final hasZone = text.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(text);
  return DateTime.tryParse(hasZone ? text : '${text}Z')?.toLocal();
}

String? _id(Object? v) => v == null || '$v'.isEmpty ? null : '$v';

DateTime? _day(Object? v) {
  final d = v == null ? null : DateTime.tryParse('$v');
  return d == null ? null : DateTime(d.year, d.month, d.day);
}

/// A parent's request waiting for the logged-in teacher (GET /api/evaluation-request/my-pending)
class PendingEvaluationRequest {
  const PendingEvaluationRequest({required this.id, required this.studentId, required this.requestedAt, this.subjectId, this.note});

  final String id;
  final String studentId;

  /// The subject the parent asked about (null: any)
  final String? subjectId;
  final DateTime requestedAt;
  final String? note;

  factory PendingEvaluationRequest.fromJson(Map<String, dynamic> json) {
    final note = (json['note'] as String?)?.trim();
    return PendingEvaluationRequest(
      id: '${json['id'] ?? ''}',
      studentId: '${json['studentId'] ?? ''}',
      subjectId: _id(json['subjectId']),
      requestedAt: _utc(json['requestedAt']) ?? DateTime(2000),
      note: note == null || note.isEmpty ? null : note,
    );
  }
}

/// An evaluation of a student in a lesson (GET /api/student-evaluation/get-by-period/{slotId}?date=)
class StudentEvaluation {
  const StudentEvaluation({
    required this.id,
    required this.studentId,
    required this.teacherId,
    this.subjectId = '',
    this.timetableSlotId = '',
    this.date,
    this.evaluationRequestId,
    required this.focus,
    required this.interaction,
    required this.behavior,
    required this.health,
    this.comment,
  });

  final String id;
  final String studentId;
  final String teacherId;
  final String subjectId;

  /// The lesson it was given in
  final String timetableSlotId;
  final DateTime? date;

  /// The parent's request it answers (part of it, for an "all subjects" request)
  final String? evaluationRequestId;

  /// 1..5
  final int focus;
  final int interaction;
  final int behavior;
  final StudentHealth health;
  final String? comment;

  double get average => (focus + interaction + behavior) / 3;

  factory StudentEvaluation.fromJson(Map<String, dynamic> json) {
    int rating(Object? v) => ((v is num ? v.toInt() : int.tryParse('$v')) ?? 3).clamp(1, 5);
    final comment = (json['comment'] as String?)?.trim();
    return StudentEvaluation(
      id: '${json['id'] ?? ''}',
      studentId: '${json['studentId'] ?? ''}',
      teacherId: '${json['teacherId'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      timetableSlotId: '${json['timetableSlotId'] ?? ''}',
      date: _day(json['date']),
      evaluationRequestId: _id(json['evaluationRequestId']),
      focus: rating(json['focus']),
      interaction: rating(json['interaction']),
      behavior: rating(json['behavior']),
      health: StudentHealth.fromCode(json['healthStatus']),
      comment: comment == null || comment.isEmpty ? null : comment,
    );
  }
}

/// What the teacher saves for one student in one lesson
class EvaluationInput {
  const EvaluationInput({
    required this.studentId,
    required this.focus,
    required this.interaction,
    required this.behavior,
    required this.health,
    this.comment,
    this.evaluationRequestId,
    this.completeRequest = true,
  });

  final String studentId;
  final int focus;
  final int interaction;
  final int behavior;
  final StudentHealth health;
  final String? comment;

  /// The parent's request being answered
  final String? evaluationRequestId;

  /// False: one subject of an "all subjects" request, more are coming (the request stays open)
  final bool completeRequest;

  Map<String, dynamic> toJson(TeacherLesson lesson, DateTime date) {
    final text = (comment ?? '').trim();
    return {
      'studentId': studentId,
      'subjectId': lesson.subjectId,
      'timetableSlotId': lesson.id,
      'date': attendanceDate(date),
      'focus': focus,
      'interaction': interaction,
      'behavior': behavior,
      'healthStatus': health.code,
      'comment': text.isEmpty ? null : text,
      'evaluationRequestId': evaluationRequestId,
      'completeRequest': completeRequest,
    };
  }
}

/// Longest comment the API accepts (StudentEvaluation.MaxCommentLength)
const maxEvaluationCommentLength = 1000;

/// A request failed; [message] is ready to show
class EvaluationException implements Exception {
  const EvaluationException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

/// Student evaluations of the logged-in teacher and the parents' requests (Attendance service)
class EvaluationService {
  EvaluationService._();
  static final instance = EvaluationService._();

  String get _api => '${AppConfig.apiBaseUrl}/api';

  /// Requests waiting for the teacher, oldest first
  Future<List<PendingEvaluationRequest>> getPendingRequests() async {
    final body = await _send((h) => http.get(Uri.parse('$_api/evaluation-request/my-pending'), headers: h),
        failed: tr('تعذر تحميل الطلبات.', 'Could not load the requests.'));
    final data = body['data'];
    if (data is! List) throw EvaluationException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(PendingEvaluationRequest.fromJson).toList()
      ..sort((a, b) => a.requestedAt.compareTo(b.requestedAt));
  }

  /// The evaluations already saved for [lesson] on [date] (by any teacher)
  Future<List<StudentEvaluation>> getLessonEvaluations(TeacherLesson lesson, DateTime date) async {
    final uri = Uri.parse('$_api/student-evaluation/get-by-period/${Uri.encodeComponent(lesson.id)}')
        .replace(queryParameters: {'date': attendanceDate(date)});
    final body = await _send((h) => http.get(uri, headers: h), failed: tr('تعذر تحميل التقييمات.', 'Could not load the evaluations.'));
    final data = body['data'];
    if (data is! List) throw EvaluationException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(StudentEvaluation.fromJson).toList();
  }

  /// A student's evaluations between two dates (by any teacher)
  Future<List<StudentEvaluation>> getStudentEvaluations(String studentId, DateTime from, DateTime to) async {
    final uri = Uri.parse('$_api/student-evaluation/get-by-student/${Uri.encodeComponent(studentId)}')
        .replace(queryParameters: {'from': attendanceDate(from), 'to': attendanceDate(to)});
    final body = await _send((h) => http.get(uri, headers: h), failed: tr('تعذر تحميل التقييمات.', 'Could not load the evaluations.'));
    final data = body['data'];
    if (data is! List) throw EvaluationException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(StudentEvaluation.fromJson).toList();
  }

  /// Saves the student's evaluation in [lesson] on [date] (saving again updates it). Answers the request
  /// given, or the student's waiting requests to this teacher.
  Future<void> evaluate(TeacherLesson lesson, DateTime date, EvaluationInput input) async {
    await _send(
      (h) => http.post(Uri.parse('$_api/student-evaluation/evaluate'),
          headers: {...h, 'Content-Type': 'application/json'}, body: jsonEncode(input.toJson(lesson, date))),
      failed: tr('تعذر حفظ التقييم.', 'Could not save the evaluation.'),
    );
  }

  Future<Map<String, dynamic>> _send(Future<http.Response> Function(Map<String, String> headers) request, {required String failed}) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw EvaluationException(_sessionEnded, sessionExpired: true);

    final http.Response response;
    try {
      response = await request({'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'})
          .timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw EvaluationException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw EvaluationException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }

    switch (response.statusCode) {
      case 200:
      case 201:
        break;
      case 401:
        throw EvaluationException(_sessionEnded, sessionExpired: true);
      case 403:
        throw EvaluationException(tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'));
      case 503:
        throw EvaluationException(tr('خدمة التقييم غير متاحة الآن، حاول لاحقاً.', 'The evaluation service is not available right now. Try again later.'));
      default:
        throw EvaluationException(failed);
    }

    final Object? body;
    try {
      body = jsonDecode(utf8.decode(response.bodyBytes));
    } catch (_) {
      throw EvaluationException(_unexpected);
    }
    if (body is! Map<String, dynamic>) throw EvaluationException(_unexpected);
    // Rules broken come back as 200 with status 400 and a message
    final status = body['status'];
    if (status is num && status != 200) throw EvaluationException(_message('${body['message'] ?? ''}', failed));
    return body;
  }

  /// Arabic / English text of an API message
  static String _message(String message, String fallback) => switch (message) {
        'Another teacher already evaluated this student in this lesson.' =>
          tr('قيّم معلم آخر هذا الطالب في هذه الحصة بالفعل.', 'Another teacher already evaluated this student in this lesson.'),
        'Evaluation request not found.' => tr('طلب التقييم غير موجود.', 'The evaluation request was not found.'),
        'The evaluation request is not for this student and teacher.' =>
          tr('طلب التقييم ليس لهذا الطالب أو ليس لك.', 'The request is not for this student or not for you.'),
        'Only a pending request can be evaluated.' => tr('تم الرد على هذا الطلب أو إلغاؤه.', 'This request was already answered or cancelled.'),
        'Ratings must be from Weak (1) to Excellent (5).' => tr('التقييم من 1 إلى 5.', 'Ratings are from 1 to 5.'),
        'Health status is not valid.' => tr('اختر الحالة الصحية.', 'Choose the health status.'),
        'Only a teacher can evaluate a student.' || 'NotATeacher' =>
          tr('هذا الحساب غير مرتبط بمعلم.', 'This account is not linked to a teacher.'),
        _ when message.startsWith('Comment cannot exceed') =>
          tr('التعليق طويل جداً (الحد $maxEvaluationCommentLength حرف).', 'The comment is too long (max $maxEvaluationCommentLength characters).'),
        _ => fallback,
      };

  static String get _unexpected => tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.');

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
