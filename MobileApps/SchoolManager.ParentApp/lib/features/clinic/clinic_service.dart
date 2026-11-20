import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/auth/auth_service.dart';
import '../../core/config.dart';
import '../../core/i18n/app_language.dart';

/// The child's medical profile kept by the school clinic
/// (GET / PUT /api/student/my-children/{studentId}/medical-profile)
class MedicalProfile {
  const MedicalProfile({
    this.bloodType = '',
    this.allergies = '',
    this.chronicDiseases = '',
    this.regularMedications = '',
    this.emergencyPhone = '',
    this.notes = '',
  });

  final String bloodType;
  final String allergies;
  final String chronicDiseases;
  final String regularMedications;
  final String emergencyPhone;
  final String notes;

  /// Nothing filled in yet
  bool get isEmpty => [bloodType, allergies, chronicDiseases, regularMedications, emergencyPhone, notes]
      .every((v) => v.trim().isEmpty);

  factory MedicalProfile.fromJson(Map<String, dynamic> json) => MedicalProfile(
        bloodType: (json['bloodType'] as String?) ?? '',
        allergies: (json['allergies'] as String?) ?? '',
        chronicDiseases: (json['chronicDiseases'] as String?) ?? '',
        regularMedications: (json['regularMedications'] as String?) ?? '',
        emergencyPhone: (json['emergencyPhone'] as String?) ?? '',
        notes: (json['notes'] as String?) ?? '',
      );

  /// Empty fields are sent as null (the API stores "not set")
  Map<String, dynamic> toJson() {
    String? v(String s) => s.trim().isEmpty ? null : s.trim();
    return {
      'bloodType': v(bloodType),
      'allergies': v(allergies),
      'chronicDiseases': v(chronicDiseases),
      'regularMedications': v(regularMedications),
      'emergencyPhone': v(emergencyPhone),
      'notes': v(notes),
    };
  }
}

/// .NET enum as a number (default JSON) or a name ("SentHome"); [names] in enum value order starting at 1
int _enumValue(Object? value, List<String> names) {
  if (value is num) return value.toInt();
  final index = names.indexWhere((n) => n.toLowerCase() == '$value'.toLowerCase());
  return index < 0 ? 0 : index + 1;
}

double? _number(Object? value) => value is num ? value.toDouble() : double.tryParse('${value ?? ''}');

DateTime? _date(Object? value) => value == null ? null : DateTime.tryParse('$value');

enum VaccinationState { given, notGiven, exempted, parentRefused, unknown }

/// A vaccine dose the clinic recorded for the child (GET /api/student/my-children/{id}/vaccinations)
class ChildVaccination {
  const ChildVaccination({
    required this.id,
    required this.vaccineName,
    required this.doseNumber,
    required this.status,
    this.date,
    this.atSchool = true,
    this.notes = '',
  });

  final String id;
  final String vaccineName;
  final int doseNumber;
  final VaccinationState status;
  final DateTime? date;

  /// Given at school (else outside, e.g. a hospital)
  final bool atSchool;
  final String notes;

  factory ChildVaccination.fromJson(Map<String, dynamic> json) {
    final status = _enumValue(json['status'], ['Given', 'NotGiven', 'Exempted', 'ParentRefused']);
    return ChildVaccination(
      id: '${json['id'] ?? ''}',
      vaccineName: pickLang(json['vaccineNameAr'] as String?, json['vaccineNameEn'] as String?),
      doseNumber: (json['doseNumber'] as num?)?.toInt() ?? 1,
      status: status >= 1 && status <= 4 ? VaccinationState.values[status - 1] : VaccinationState.unknown,
      date: _date(json['vaccinationDate']),
      atSchool: _enumValue(json['place'], ['School', 'External']) != 2,
      notes: (json['notes'] as String?) ?? '',
    );
  }
}

enum VisitResult { returnedToClass, sentHome, referred, unknown }

/// A visit of the child to the school clinic (GET /api/student/my-children/{id}/clinic-visits)
class ClinicVisit {
  const ClinicVisit({
    required this.id,
    required this.date,
    required this.complaint,
    this.doctorName = '',
    this.temperature,
    this.weight,
    this.height,
    this.diagnosis = '',
    this.treatment = '',
    this.outcome = VisitResult.unknown,
    this.parentNotified = false,
    this.notes = '',
    this.medications = const [],
  });

  final String id;
  final DateTime date;
  final String complaint;
  final String doctorName;
  final double? temperature;
  final double? weight;
  final double? height;
  final String diagnosis;
  final String treatment;
  final VisitResult outcome;
  final bool parentNotified;
  final String notes;

  /// (medicine, dose)
  final List<(String, String)> medications;

  factory ClinicVisit.fromJson(Map<String, dynamic> json) {
    final outcome = _enumValue(json['outcome'], ['ReturnedToClass', 'SentHome', 'Referred']);
    final meds = json['medications'];
    return ClinicVisit(
      id: '${json['id'] ?? ''}',
      // The API sends local school time without a zone
      date: _date(json['visitDate']) ?? DateTime.fromMillisecondsSinceEpoch(0),
      complaint: (json['complaint'] as String?) ?? '',
      doctorName: (json['doctorName'] as String?) ?? '',
      temperature: _number(json['temperature']),
      weight: _number(json['weight']),
      height: _number(json['height']),
      diagnosis: (json['diagnosis'] as String?) ?? '',
      treatment: (json['treatment'] as String?) ?? '',
      outcome: outcome >= 1 && outcome <= 3 ? VisitResult.values[outcome - 1] : VisitResult.unknown,
      parentNotified: json['parentNotified'] == true,
      notes: (json['notes'] as String?) ?? '',
      medications: [
        if (meds is List)
          for (final m in meds.whereType<Map<String, dynamic>>())
            ('${m['medicineName'] ?? ''}', '${m['dose'] ?? ''}'),
      ],
    );
  }
}

/// Why the medical profile could not be loaded or saved; [message] is ready to show
class ClinicException implements Exception {
  const ClinicException(this.message, {this.sessionExpired = false});
  final String message;
  final bool sessionExpired;

  @override
  String toString() => message;
}

class ClinicService {
  ClinicService._();
  static final instance = ClinicService._();

  /// Longest values the API accepts
  static const maxBloodType = 10;
  static const maxEmergencyPhone = 50;

  Uri _uri(String studentId, [String part = 'medical-profile']) =>
      Uri.parse('${AppConfig.apiBaseUrl}/api/student/my-children/${Uri.encodeComponent(studentId)}/$part');

  /// The child's vaccinations, newest first (not given yet = no date, last)
  Future<List<ChildVaccination>> getVaccinations(String studentId) async {
    final list = await _getList(studentId, 'vaccinations', ChildVaccination.fromJson,
        tr('تعذر تحميل التطعيمات.', 'Could not load the vaccinations.'));
    return list
      ..sort((a, b) {
        if (a.date == null || b.date == null) return a.date == null ? (b.date == null ? 0 : 1) : -1;
        return b.date!.compareTo(a.date!);
      });
  }

  /// The child's clinic visits, newest first
  Future<List<ClinicVisit>> getClinicVisits(String studentId) async {
    final list = await _getList(studentId, 'clinic-visits', ClinicVisit.fromJson,
        tr('تعذر تحميل زيارات العيادة.', 'Could not load the clinic visits.'));
    return list..sort((a, b) => b.date.compareTo(a.date));
  }

  Future<List<T>> _getList<T>(
      String studentId, String part, T Function(Map<String, dynamic>) fromJson, String failed) async {
    final token = _token();
    final response = await _send(() => http.get(
          _uri(studentId, part),
          headers: {'Accept': 'application/json', 'Authorization': 'Bearer $token'},
        ));
    _checkStatus(response, failed);

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      final data = body is Map<String, dynamic> ? body['data'] : null;
      if (data is! List) throw const FormatException();
      return data.whereType<Map<String, dynamic>>().map(fromJson).toList();
    } catch (_) {
      throw ClinicException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  /// The child's medical profile; null when the clinic has none yet
  Future<MedicalProfile?> getMedicalProfile(String studentId) async {
    final token = _token();
    final response = await _send(() => http.get(
          _uri(studentId),
          headers: {'Accept': 'application/json', 'Authorization': 'Bearer $token'},
        ));
    _checkStatus(response, tr('تعذر تحميل الملف الصحي.', 'Could not load the health profile.'));

    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      if (body is! Map<String, dynamic>) throw const FormatException();
      final data = body['data'];
      if (data == null) return null;
      if (data is! Map<String, dynamic>) throw const FormatException();
      return MedicalProfile.fromJson(data);
    } catch (_) {
      throw ClinicException(tr('استجابة غير متوقعة من الخادم.', 'Unexpected response from the server.'));
    }
  }

  /// Creates or updates the child's medical profile
  Future<void> saveMedicalProfile(String studentId, MedicalProfile profile) async {
    final token = _token();
    final response = await _send(() => http.put(
          _uri(studentId),
          headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'Authorization': 'Bearer $token',
          },
          body: jsonEncode(profile.toJson()),
        ));

    if (response.statusCode == 400) {
      throw ClinicException(_validationMessage(response));
    }
    _checkStatus(response, tr('تعذر حفظ الملف الصحي.', 'Could not save the health profile.'));
  }

  String _token() {
    final session = AuthService.instance.session;
    if (session == null || session.isExpired) throw _expired();
    return session.token;
  }

  Future<http.Response> _send(Future<http.Response> Function() request) async {
    try {
      return await request().timeout(AppConfig.requestTimeout);
    } on TimeoutException {
      throw ClinicException(tr('انتهت مهلة الاتصال بالخادم.', 'The server took too long to respond.'));
    } catch (_) {
      throw ClinicException(tr('تعذر الاتصال بالخادم.', 'Could not reach the server.'));
    }
  }

  void _checkStatus(http.Response response, String fallback) {
    if (response.statusCode == 401) throw _expired();
    if (response.statusCode == 404) {
      throw ClinicException(tr('هذا الطالب غير مرتبط بحسابك.', 'This student is not linked to your account.'));
    }
    if (response.statusCode != 200) throw ClinicException(fallback);
  }

  ClinicException _expired() => ClinicException(
      tr('انتهت الجلسة، من فضلك سجّل الدخول مرة أخرى.', 'Your session has ended. Please sign in again.'),
      sessionExpired: true);

  /// API validation codes ("BloodTypeTooLong", ...) as a readable message
  String _validationMessage(http.Response response) {
    String? code;
    try {
      final body = jsonDecode(utf8.decode(response.bodyBytes));
      if (body is Map<String, dynamic>) code = (body['message'] ?? body['Message']) as String?;
    } catch (_) {}
    return switch (code) {
      'BloodTypeTooLong' => tr('فصيلة الدم طويلة جداً.', 'The blood type is too long.'),
      'EmergencyPhoneTooLong' => tr('رقم الطوارئ طويل جداً.', 'The emergency phone is too long.'),
      _ => tr('تعذر حفظ الملف الصحي، راجع البيانات.', 'Could not save the health profile. Check the details.'),
    };
  }
}
