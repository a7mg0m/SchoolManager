import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../auth/auth_service.dart';
import '../config.dart';

DateTime _day(Object? v) {
  final d = DateTime.tryParse('$v') ?? DateTime(2000);
  return DateTime(d.year, d.month, d.day);
}

/// A semester of an academic year
class AcademicSemester {
  const AcademicSemester({
    required this.id,
    required this.name,
    required this.startDate,
    required this.endDate,
    this.isActive = true,
  });

  final String id;

  /// e.g. "الفصل الأول"
  final String name;
  final DateTime startDate;
  final DateTime endDate;
  final bool isActive;

  bool contains(DateTime day) {
    final d = DateTime(day.year, day.month, day.day);
    return !d.isBefore(startDate) && !d.isAfter(endDate);
  }

  factory AcademicSemester.fromJson(Map<String, dynamic> json) => AcademicSemester(
        id: '${json['id'] ?? ''}',
        name: (json['name'] as String?)?.trim() ?? '',
        startDate: _day(json['startDate']),
        endDate: _day(json['endDate']),
        isActive: json['isActive'] != false,
      );
}

/// An academic year with its semesters (Management: GET /api/academic-year/get-all)
class AcademicYear {
  const AcademicYear({
    required this.id,
    required this.name,
    required this.startDate,
    required this.endDate,
    this.isActive = true,
    this.semesters = const [],
  });

  final String id;

  /// e.g. "2025-2026"
  final String name;
  final DateTime startDate;
  final DateTime endDate;
  final bool isActive;

  /// Semesters in date order
  final List<AcademicSemester> semesters;

  /// Today falls inside the year
  bool contains(DateTime day) {
    final d = DateTime(day.year, day.month, day.day);
    return !d.isBefore(startDate) && !d.isAfter(endDate);
  }

  factory AcademicYear.fromJson(Map<String, dynamic> json) => AcademicYear(
        id: '${json['id'] ?? ''}',
        name: (json['name'] as String?)?.trim() ?? '',
        startDate: _day(json['startDate']),
        endDate: _day(json['endDate']),
        isActive: json['isActive'] != false,
        semesters: (json['semesters'] as List? ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(AcademicSemester.fromJson)
            .where((s) => s.id.isNotEmpty)
            .toList()
          ..sort((a, b) => a.startDate.compareTo(b.startDate)),
      );
}

/// The academic year and semester chosen in the app (default: the current ones). Requests that depend on
/// them send `academicYearId` / `semesterId`. The choice is saved on the device.
/// Listeners are told when either changes.
class AcademicYearController extends ValueNotifier<AcademicYear?> {
  AcademicYearController._() : super(null);
  static final instance = AcademicYearController._();

  static const _key = 'academic_year_id';
  static const _semesterKey = 'semester_id';

  /// All years, newest first
  List<AcademicYear> years = const [];

  /// Semester chosen in the chosen year (null when the year has none)
  AcademicSemester? semester;

  bool _loaded = false;
  Future<void>? _loading;

  /// Year id to send to the API, or null (the server then uses its own default)
  String? get selectedId => value?.id;

  /// Semester id to send to the API, or null
  String? get selectedSemesterId => semester?.id;

  /// The current year: active and today inside it, else the latest active one, else the latest
  static AcademicYear? currentOf(List<AcademicYear> years, DateTime now) {
    if (years.isEmpty) return null;
    return years.where((y) => y.isActive && y.contains(now)).firstOrNull ??
        years.where((y) => y.isActive).firstOrNull ??
        years.first;
  }

  /// The current semester of [year]: running today, else an active one, else the first
  static AcademicSemester? currentSemesterOf(AcademicYear? year, DateTime now) {
    final list = year?.semesters ?? const <AcademicSemester>[];
    if (list.isEmpty) return null;
    return list.where((s) => s.contains(now)).firstOrNull ??
        list.where((s) => s.isActive).firstOrNull ??
        list.first;
  }

  /// Loads the years once (call again with [force] to refresh). [fetch] replaces the API in tests.
  Future<void> ensureLoaded({bool force = false, Future<List<AcademicYear>> Function()? fetch}) {
    if (_loaded && !force) return Future.value();
    return _loading ??= _load(fetch).whenComplete(() => _loading = null);
  }

  Future<void> _load(Future<List<AcademicYear>> Function()? fetch) async {
    try {
      final list = [...await (fetch ?? _fetch)()]..sort((a, b) => b.startDate.compareTo(a.startDate));
      years = list;
      String? savedYear;
      String? savedSemester;
      try {
        final prefs = await SharedPreferences.getInstance();
        savedYear = prefs.getString(_key);
        savedSemester = prefs.getString(_semesterKey);
      } catch (_) {}
      final year = list.where((y) => y.id == savedYear).firstOrNull ?? currentOf(list, DateTime.now());
      semester = year?.semesters.where((s) => s.id == savedSemester).firstOrNull ??
          currentSemesterOf(year, DateTime.now());
      value = year;
      _loaded = true;
    } catch (_) {
      // No years: requests go without academicYearId / semesterId (the server uses the current ones)
    }
  }

  /// Another year: its current semester is chosen with it
  Future<void> select(AcademicYear year) async {
    if (year.id == value?.id) return;
    semester = currentSemesterOf(year, DateTime.now());
    value = year;
    await _save();
  }

  /// Another semester of the chosen year
  Future<void> selectSemester(AcademicSemester next) async {
    if (next.id == semester?.id) return;
    semester = next;
    notifyListeners();
    await _save();
  }

  Future<void> _save() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final year = value;
      if (year != null) await prefs.setString(_key, year.id);
      final chosen = semester;
      if (chosen != null) {
        await prefs.setString(_semesterKey, chosen.id);
      } else {
        await prefs.remove(_semesterKey);
      }
    } catch (_) {}
  }

  /// Forget the choice (sign out: the next account starts on the current year and semester)
  Future<void> reset() async {
    years = const [];
    _loaded = false;
    semester = null;
    value = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_key);
      await prefs.remove(_semesterKey);
    } catch (_) {}
  }

  /// [uri] with `academicYearId` and `semesterId` added (only the ones chosen)
  Uri withYear(Uri uri) {
    final year = selectedId;
    final semesterId = selectedSemesterId;
    final query = {
      ...uri.queryParameters,
      if (year != null && year.isNotEmpty) 'academicYearId': year,
      if (semesterId != null && semesterId.isNotEmpty) 'semesterId': semesterId,
    };
    return query.isEmpty ? uri : uri.replace(queryParameters: query);
  }

  Future<List<AcademicYear>> _fetch() async {
    final token = AuthService.instance.session?.token;
    final response = await http.get(
      Uri.parse('${AppConfig.apiBaseUrl}/api/academic-year/get-all'),
      headers: {'Accept': 'application/json', if (token != null) 'Authorization': 'Bearer $token'},
    ).timeout(AppConfig.requestTimeout);
    if (response.statusCode != 200) throw Exception('academic years: ${response.statusCode}');
    final body = jsonDecode(utf8.decode(response.bodyBytes));
    final data = body is Map<String, dynamic> ? body['data'] : null;
    return (data is List ? data : const [])
        .whereType<Map<String, dynamic>>()
        .map(AcademicYear.fromJson)
        .where((y) => y.id.isNotEmpty)
        .toList();
  }
}
