import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';

/// Status of a student fee (Financial service FeeStatus)
enum FeeState { pending, partiallyPaid, paid, waived, overdue }

/// One fee assigned to the child (GET /api/student-fee/my-child/{studentId})
class StudentFeeItem {
  const StudentFeeItem({
    required this.id,
    required this.total,
    required this.discount,
    required this.paid,
    required this.waived,
    required this.net,
    required this.balance,
    required this.currency,
    required this.status,
    required this.dueDate,
    this.waiveReason,
  });

  final String id;
  final double total;
  final double discount;
  final double paid;
  final double waived;

  /// Total after discount
  final double net;

  /// Still to pay
  final double balance;
  final String currency;
  final FeeState status;
  final DateTime dueDate;
  final String? waiveReason;

  /// Overdue also when the server has not marked it yet (unpaid and past the due date)
  FeeState get effectiveStatus {
    if ((status == FeeState.pending || status == FeeState.partiallyPaid) && balance > 0) {
      final today = DateTime.now();
      final due = DateTime(dueDate.year, dueDate.month, dueDate.day);
      if (due.isBefore(DateTime(today.year, today.month, today.day))) return FeeState.overdue;
    }
    return status;
  }

  bool get isOutstanding => balance > 0 && status != FeeState.waived && status != FeeState.paid;

  /// Part of the net amount that is settled (paid or waived), 0..1
  double get settledRatio => net <= 0 ? 1 : ((paid + waived) / net).clamp(0, 1).toDouble();

  factory StudentFeeItem.fromJson(Map<String, dynamic> json) => StudentFeeItem(
        id: '${json['id'] ?? ''}',
        total: _num(json['totalAmount']),
        discount: _num(json['discountAmount']),
        paid: _num(json['paidAmount']),
        waived: _num(json['waivedAmount']),
        net: _num(json['netAmount']),
        balance: _num(json['balance']),
        currency: (json['currency'] as String?)?.trim() ?? '',
        status: _status(json['status']),
        dueDate: DateTime.tryParse('${json['dueDate']}') ?? DateTime.now(),
        waiveReason: (json['waiveReason'] as String?)?.trim(),
      );
}

/// Loading the fees failed; [message] is ready to show (Arabic)
class FeesException implements Exception {
  const FeesException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class FeesService {
  FeesService._();
  static final instance = FeesService._();

  Future<List<StudentFeeItem>> getChildFees(String studentId) async {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) {
      throw const FeesException('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', sessionExpired: true);
    }

    final http.Response response;
    try {
      response = await http.get(
        Uri.parse('${AppConfig.apiBaseUrl}/api/student-fee/my-child/${Uri.encodeComponent(studentId)}'),
        headers: {'Accept': 'application/json', 'Authorization': 'Bearer ${session.token}'},
      ).timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw const FeesException('انتهت مهلة الاتصال بالخادم.');
    } catch (_) {
      throw const FeesException('تعذر الاتصال بالخادم.');
    }

    switch (response.statusCode) {
      case 200:
        break;
      case 401:
        throw const FeesException('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', sessionExpired: true);
      case 503:
        throw const FeesException('خدمة البيانات غير متاحة حالياً، حاول لاحقاً.');
      default:
        throw const FeesException('تعذر تحميل الرسوم الدراسية.');
    }

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(StudentFeeItem.fromJson).toList();
    } catch (_) {
      throw const FeesException('استجابة غير متوقعة من الخادم.');
    }
  }
}

double _num(Object? value) => value is num ? value.toDouble() : double.tryParse('$value') ?? 0;

/// FeeStatus as a number (default JSON: Pending=0 … Overdue=4) or a name
FeeState _status(Object? value) {
  if (value is num) {
    final i = value.toInt();
    return i >= 0 && i < FeeState.values.length ? FeeState.values[i] : FeeState.pending;
  }
  return switch ('$value'.toLowerCase()) {
    'partiallypaid' => FeeState.partiallyPaid,
    'paid' => FeeState.paid,
    'waived' => FeeState.waived,
    'overdue' => FeeState.overdue,
    _ => FeeState.pending,
  };
}
