import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// A subject a teacher teaches the child
class TeacherSubjectRef {
  const TeacherSubjectRef({required this.id, required this.nameAr, required this.nameEn});

  final String id;
  final String nameAr;
  final String nameEn;

  String get name => pickLang(nameAr, nameEn);
}

/// A teacher of the child (GET /api/student/my-teachers, the ones teaching this child)
class ChildTeacher {
  const ChildTeacher({required this.id, required this.nameAr, required this.nameEn, this.subjects = const []});

  final String id;
  final String nameAr;
  final String nameEn;
  final List<TeacherSubjectRef> subjects;

  String get name => pickLang(nameAr, nameEn);

  /// "رياضيات · علوم"
  String get subjectsText => subjects.map((s) => s.name).where((s) => s.isNotEmpty).join(' · ');

  /// The subject's name when the teacher teaches it
  String? subjectName(String? subjectId) {
    if (subjectId == null) return null;
    for (final s in subjects) {
      if (s.id == subjectId) return s.name;
    }
    return null;
  }

  factory ChildTeacher.fromJson(Map<String, dynamic> json) => ChildTeacher(
        id: '${json['teacherId'] ?? ''}',
        nameAr: (json['nameAr'] as String?)?.trim() ?? '',
        nameEn: (json['nameEn'] as String?)?.trim() ?? '',
        subjects: (json['subjects'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map((s) => TeacherSubjectRef(
                  id: '${s['subjectId'] ?? ''}',
                  nameAr: (s['nameAr'] as String?)?.trim() ?? '',
                  nameEn: (s['nameEn'] as String?)?.trim() ?? '',
                ))
            .toList(),
      );
}

/// SchoolManager.Attendance.Domain.EvaluationRequestStatus (sent as its number)
enum RequestStatus {
  pending(0),
  evaluated(1),
  cancelled(2);

  const RequestStatus(this.code);
  final int code;

  static RequestStatus fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return RequestStatus.values.firstWhere((s) => s.code == code, orElse: () => RequestStatus.pending);
  }

  String get label => switch (this) {
        RequestStatus.pending => tr('بانتظار المعلم', 'Waiting for the teacher'),
        RequestStatus.evaluated => tr('تم التقييم', 'Evaluated'),
        RequestStatus.cancelled => tr('ملغي', 'Cancelled'),
      };
}

/// The API sends UTC times without a zone
DateTime? _utc(Object? value) {
  if (value == null) return null;
  final text = '$value';
  final hasZone = text.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(text);
  return DateTime.tryParse(hasZone ? text : '${text}Z')?.toLocal();
}

/// A request of the parent to a teacher (GET /api/evaluation-request/my-requests?studentId=)
class EvaluationRequestItem {
  const EvaluationRequestItem({
    required this.id,
    required this.studentId,
    required this.teacherId,
    required this.requestedAt,
    required this.status,
    this.subjectId,
    this.note,
    this.evaluationId,
    this.evaluatedAt,
  });

  final String id;
  final String studentId;
  final String teacherId;
  final String? subjectId;
  final DateTime requestedAt;
  final String? note;
  final RequestStatus status;
  final String? evaluationId;
  final DateTime? evaluatedAt;

  factory EvaluationRequestItem.fromJson(Map<String, dynamic> json) {
    String? id(Object? v) => v == null || '$v'.isEmpty ? null : '$v';
    final note = (json['note'] as String?)?.trim();
    return EvaluationRequestItem(
      id: '${json['id'] ?? ''}',
      studentId: '${json['studentId'] ?? ''}',
      teacherId: '${json['teacherId'] ?? ''}',
      subjectId: id(json['subjectId']),
      requestedAt: _utc(json['requestedAt']) ?? DateTime(2000),
      note: note == null || note.isEmpty ? null : note,
      status: RequestStatus.fromCode(json['status']),
      evaluationId: id(json['evaluationId']),
      evaluatedAt: _utc(json['evaluatedAt']),
    );
  }
}

/// SchoolManager.Attendance.Domain.HealthStatus
enum ChildHealth {
  good(0),
  tired(1),
  sick(2);

  const ChildHealth(this.code);
  final int code;

  static ChildHealth fromCode(Object? value) {
    final code = value is num ? value.toInt() : int.tryParse('$value');
    return ChildHealth.values.firstWhere((s) => s.code == code, orElse: () => ChildHealth.good);
  }

  String get label => switch (this) {
        ChildHealth.good => tr('جيدة', 'Good'),
        ChildHealth.tired => tr('متعب', 'Tired'),
        ChildHealth.sick => tr('مريض', 'Sick'),
      };
}

/// Rating 1..5 (SchoolManager.Attendance.Domain.EvaluationRating)
String ratingLabel(int rating) => switch (rating) {
      1 => tr('ضعيف', 'Weak'),
      2 => tr('مقبول', 'Acceptable'),
      3 => tr('جيد', 'Good'),
      4 => tr('جيد جداً', 'Very good'),
      _ => tr('ممتاز', 'Excellent'),
    };

/// A teacher's evaluation of the child in a lesson (GET /api/student-evaluation/my-child/{id}?from=&to=)
class ChildEvaluation {
  const ChildEvaluation({
    required this.id,
    required this.teacherId,
    required this.subjectId,
    required this.date,
    required this.focus,
    required this.interaction,
    required this.behavior,
    required this.health,
    this.comment,
    this.evaluationRequestId,
  });

  final String id;
  final String teacherId;
  final String subjectId;
  final DateTime date;

  /// 1..5
  final int focus;
  final int interaction;
  final int behavior;
  final ChildHealth health;
  final String? comment;
  final String? evaluationRequestId;

  factory ChildEvaluation.fromJson(Map<String, dynamic> json) {
    int rating(Object? v) => ((v is num ? v.toInt() : int.tryParse('$v')) ?? 3).clamp(1, 5);
    final d = DateTime.tryParse('${json['date']}') ?? DateTime(2000);
    final comment = (json['comment'] as String?)?.trim();
    final requestId = json['evaluationRequestId'];
    return ChildEvaluation(
      id: '${json['id'] ?? ''}',
      teacherId: '${json['teacherId'] ?? ''}',
      subjectId: '${json['subjectId'] ?? ''}',
      date: DateTime(d.year, d.month, d.day),
      focus: rating(json['focus']),
      interaction: rating(json['interaction']),
      behavior: rating(json['behavior']),
      health: ChildHealth.fromCode(json['healthStatus']),
      comment: comment == null || comment.isEmpty ? null : comment,
      evaluationRequestId: requestId == null || '$requestId'.isEmpty ? null : '$requestId',
    );
  }
}

/// Longest note the API accepts (EvaluationRequest.MaxNoteLength)
const maxRequestNoteLength = 500;

/// "yyyy-MM-dd"
String _date(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// A request failed; [message] is ready to show
class EvaluationException implements Exception {
  const EvaluationException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

/// Evaluation requests to the child's teachers (Attendance service) and the teachers (Management service)
class EvaluationService {
  EvaluationService._();
  static final instance = EvaluationService._();

  String get _api => '${AppConfig.apiBaseUrl}/api';

  /// The teachers who teach [studentId]
  Future<List<ChildTeacher>> getChildTeachers(String studentId) async {
    final body = await _send((h) => http.get(Uri.parse('$_api/student/my-teachers'), headers: h),
        failed: tr('تعذر تحميل المعلمين.', 'Could not load the teachers.'));
    final data = body['data'];
    if (data is! List) throw EvaluationException(_unexpected);
    return data
        .whereType<Map<String, dynamic>>()
        .where((t) => (t['children'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .any((c) => '${c['studentId']}' == studentId))
        .map(ChildTeacher.fromJson)
        .where((t) => t.id.isNotEmpty)
        .toList()
      ..sort((a, b) => a.name.compareTo(b.name));
  }

  /// The parent's requests for [studentId], newest first
  Future<List<EvaluationRequestItem>> getRequests(String studentId) async {
    final uri = Uri.parse('$_api/evaluation-request/my-requests').replace(queryParameters: {'studentId': studentId});
    final body = await _send((h) => http.get(uri, headers: h), failed: tr('تعذر تحميل الطلبات.', 'Could not load the requests.'));
    final data = body['data'];
    if (data is! List) throw EvaluationException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(EvaluationRequestItem.fromJson).toList()
      ..sort((a, b) => b.requestedAt.compareTo(a.requestedAt));
  }

  /// Asks [teacherId] to evaluate the child (about [subjectId] when given)
  Future<void> createRequest({required String studentId, required String teacherId, String? subjectId, String? note}) async {
    final text = (note ?? '').trim();
    await _send(
      (h) => http.post(Uri.parse('$_api/evaluation-request/create'),
          headers: {...h, 'Content-Type': 'application/json'},
          body: jsonEncode({
            'studentId': studentId,
            'teacherId': teacherId,
            'subjectId': subjectId,
            'note': text.isEmpty ? null : text,
            'authorization': '',
          })),
      failed: tr('تعذر إرسال الطلب.', 'Could not send the request.'),
    );
  }

  /// Cancels a pending request
  Future<void> cancelRequest(String requestId) async {
    await _send((h) => http.put(Uri.parse('$_api/evaluation-request/cancel/${Uri.encodeComponent(requestId)}'), headers: h),
        failed: tr('تعذر إلغاء الطلب.', 'Could not cancel the request.'));
  }

  /// The teachers' evaluations of the child between two dates
  Future<List<ChildEvaluation>> getEvaluations(String studentId, DateTime from, DateTime to) async {
    final uri = Uri.parse('$_api/student-evaluation/my-child/${Uri.encodeComponent(studentId)}')
        .replace(queryParameters: {'from': _date(from), 'to': _date(to)});
    final body = await _send((h) => http.get(uri, headers: h), failed: tr('تعذر تحميل التقييمات.', 'Could not load the evaluations.'));
    final data = body['data'];
    if (data is! List) throw EvaluationException(_unexpected);
    return data.whereType<Map<String, dynamic>>().map(ChildEvaluation.fromJson).toList();
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
        throw EvaluationException(tr('هذا الحساب غير مرتبط بولي أمر.', 'This account is not linked to a parent.'));
      case 404:
        throw EvaluationException(tr('هذا الطالب ليس من أبنائك.', 'This student is not one of your children.'));
      case 503:
        throw EvaluationException(tr('الخدمة غير متاحة حالياً، حاول لاحقاً.', 'The service is not available right now. Please try later.'));
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
        'There is already a pending evaluation request for this student to this teacher.' =>
          tr('لديك طلب تقييم قيد الانتظار لدى هذا المعلم بالفعل.', 'You already have a pending request with this teacher.'),
        'The student is not one of your children.' => tr('هذا الطالب ليس من أبنائك.', 'This student is not one of your children.'),
        'Your children could not be checked right now. Please try again later.' =>
          tr('تعذر التحقق من أبنائك الآن، حاول لاحقاً.', 'Your children could not be checked right now. Please try again later.'),
        'Only a pending request can be cancelled.' => tr('لا يمكن إلغاء إلا طلب قيد الانتظار.', 'Only a pending request can be cancelled.'),
        'Only the parent who made the request can cancel it.' =>
          tr('لا يمكن إلغاء الطلب إلا من ولي الأمر الذي أرسله.', 'Only the parent who made the request can cancel it.'),
        'Teacher is required.' => tr('اختر المعلم.', 'Choose the teacher.'),
        'Only a parent can do this.' || 'NotAParent' => tr('هذا الحساب غير مرتبط بولي أمر.', 'This account is not linked to a parent.'),
        _ when message.startsWith('Note cannot exceed') =>
          tr('الملاحظة طويلة جداً (الحد $maxRequestNoteLength حرف).', 'The note is too long (max $maxRequestNoteLength characters).'),
        _ => fallback,
      };

  static String get _unexpected => tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.');

  static String get _sessionEnded => tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.');
}
