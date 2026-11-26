import 'package:flutter/material.dart';

import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../attendance/attendance_screens.dart' show AttendanceApi;
import '../attendance/class_attendance_screen.dart';
import '../homework/homework_screens.dart' show homeworkDate;
import '../activity/student_activity_screen.dart';
import 'classes_service.dart';

const _teal = Color(0xFF0FA5A5);
const _red = Color(0xFFD93025);
const _boy = Color(0xFF1677E8);
const _girl = Color(0xFFE0457B);

/// Gradient of each class card (picked from its id, so a class keeps its colour)
const _gradients = [
  [Color(0xFF13A89E), Color(0xFF0E7C8C)],
  [Color(0xFF4C8DF6), Color(0xFF3F51B5)],
  [Color(0xFF9B6BFF), Color(0xFF6A3FD8)],
  [Color(0xFFF7A440), Color(0xFFE36414)],
  [Color(0xFFEF6A9A), Color(0xFFC2185B)],
  [Color(0xFF2BC48A), Color(0xFF14916A)],
];

List<Color> classColors(String id) =>
    _gradients[id.codeUnits.fold<int>(0, (sum, c) => (sum * 31 + c) & 0x7fffffff) % _gradients.length];

/// The teacher's classes for the chosen academic year / semester, grouped by grade; tap one for its students
class MyClassesScreen extends StatefulWidget {
  const MyClassesScreen({super.key, this.loadClasses, this.loadStudents, this.onSessionExpired, this.now});

  /// Defaults to the API (tests pass their own)
  final Future<List<MyClass>> Function()? loadClasses;
  final Future<List<ClassStudent>> Function(String sectionId)? loadStudents;
  final Future<void> Function()? onSessionExpired;
  final DateTime? now;

  @override
  State<MyClassesScreen> createState() => _MyClassesScreenState();
}

class _MyClassesScreenState extends State<MyClassesScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  List<MyClass>? _classes;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _classes == null;
      _error = null;
    });
    try {
      final result = await (widget.loadClasses ?? ClassesService.instance.getMyClasses)();
      if (!mounted) return;
      setState(() {
        _classes = result;
        _loading = false;
      });
    } on ClassesException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  void _open(MyClass myClass) {
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => ClassStudentsScreen(
        myClass: myClass,
        loadStudents: widget.loadStudents,
        onSessionExpired: widget.onSessionExpired,
        now: widget.now,
      ),
    ));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(tr('فصولي', 'My classes'), style: const TextStyle(fontWeight: FontWeight.bold))),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(context, Icons.error_outline_rounded, _error!, retry: _load)
              : RefreshIndicator(onRefresh: _load, child: _content(_classes!)),
    );
  }

  Widget _content(List<MyClass> classes) {
    final palette = context.palette;
    if (classes.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 100),
          _message(context, Icons.groups_rounded,
              tr('لا توجد فصول مسندة إليك في هذا العام / الفصل الدراسي.', 'No classes are assigned to you in this year / semester.')),
        ],
      );
    }

    final grades = <String, List<MyClass>>{};
    for (final c in classes) {
      grades.putIfAbsent(c.gradeId, () => []).add(c);
    }
    final subjectCount = classes.expand((c) => c.subjects.map((s) => s.subjectId)).toSet().length;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        // Summary
        Container(
          padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [Color(0xFF13A89E), Color(0xFF0E7C8C)]),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Row(
            children: [
              _stat(Icons.meeting_room_rounded, '${classes.length}', tr('فصول', 'Classes')),
              _stat(Icons.school_rounded, '${grades.length}', tr('صفوف', 'Grades')),
              _stat(Icons.menu_book_rounded, '$subjectCount', tr('مواد', 'Subjects')),
            ],
          ),
        ),
        for (final entry in grades.entries) ...[
          const SizedBox(height: 18),
          Row(
            children: [
              const Icon(Icons.school_rounded, size: 18, color: _teal),
              const SizedBox(width: 6),
              Text(entry.value.first.gradeName, style: TextStyle(fontSize: 15.5, fontWeight: FontWeight.bold, color: palette.ink)),
              const SizedBox(width: 6),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
                decoration: BoxDecoration(color: palette.chip, borderRadius: BorderRadius.circular(10)),
                child: Text('${entry.value.length}', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: palette.muted)),
              ),
            ],
          ),
          const SizedBox(height: 10),
          for (final c in entry.value) ...[
            _classCard(c),
            const SizedBox(height: 10),
          ],
        ],
      ],
    );
  }

  Widget _stat(IconData icon, String value, String label) => Expanded(
        child: Column(
          children: [
            Icon(icon, color: Colors.white, size: 22),
            const SizedBox(height: 4),
            Text(value, style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold)),
            Text(label, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 12)),
          ],
        ),
      );

  Widget _classCard(MyClass c) {
    final palette = context.palette;
    final colors = classColors(c.section.id);
    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        key: ValueKey('class-${c.section.id}'),
        borderRadius: BorderRadius.circular(18),
        onTap: () => _open(c),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(borderRadius: BorderRadius.circular(18), border: Border.all(color: palette.line)),
          child: Row(
            children: [
              Container(
                width: 52,
                height: 52,
                alignment: Alignment.center,
                decoration: BoxDecoration(gradient: LinearGradient(colors: colors), borderRadius: BorderRadius.circular(16)),
                child: const Icon(Icons.groups_rounded, color: Colors.white, size: 26),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(c.name, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: palette.ink)),
                    const SizedBox(height: 6),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        for (final s in c.subjects)
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                            decoration: BoxDecoration(color: colors.last.withValues(alpha: .1), borderRadius: BorderRadius.circular(20)),
                            child: Text(s.subjectName,
                                style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold, color: colors.last)),
                          ),
                      ],
                    ),
                  ],
                ),
              ),
              Icon(Directionality.of(context) == TextDirection.rtl ? Icons.chevron_left_rounded : Icons.chevron_right_rounded,
                  color: palette.muted),
            ],
          ),
        ),
      ),
    );
  }
}

/// Students of one class: boys / girls count, search, and each student's details
class ClassStudentsScreen extends StatefulWidget {
  const ClassStudentsScreen({
    super.key,
    required this.myClass,
    this.loadStudents,
    this.onSessionExpired,
    this.now,
    this.attendanceApi,
    this.activityApi = const StudentActivityApi(),
  });

  final MyClass myClass;

  /// Defaults to the API (tests pass their own)
  final Future<List<ClassStudent>> Function(String sectionId)? loadStudents;
  final Future<void> Function()? onSessionExpired;
  final DateTime? now;

  /// The class attendance page's API calls; defaults to the API with [loadStudents] (tests pass their own)
  final AttendanceApi? attendanceApi;

  /// A student's activity page (profile, weekly attendance and reviews, exams); tests pass their own
  final StudentActivityApi activityApi;

  @override
  State<ClassStudentsScreen> createState() => _ClassStudentsScreenState();
}

class _ClassStudentsScreenState extends State<ClassStudentsScreen> {
  List<ClassStudent>? _students;
  String? _error;
  bool _loading = true;
  String _query = '';

  DateTime get _now => widget.now ?? DateTime.now();

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _students == null;
      _error = null;
    });
    try {
      final result = await (widget.loadStudents ?? ClassesService.instance.getStudents)(widget.myClass.section.id);
      if (!mounted) return;
      setState(() {
        _students = result;
        _loading = false;
      });
    } on ClassesException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.myClass.label, style: const TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          // The class's attendance on a day
          TextButton.icon(
            key: const ValueKey('class-attendance'),
            onPressed: _openAttendance,
            icon: const Icon(Icons.how_to_reg_rounded, color: Colors.white),
            label: Text(tr('الحضور', 'Attendance'), style: const TextStyle(color: Colors.white)),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(context, Icons.error_outline_rounded, _error!, retry: _load)
              : RefreshIndicator(onRefresh: _load, child: _content(_students!)),
    );
  }

  void _openAttendance() {
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => ClassAttendanceScreen(
        myClass: widget.myClass,
        api: widget.attendanceApi ?? AttendanceApi(loadStudents: widget.loadStudents),
        onSessionExpired: widget.onSessionExpired,
        today: widget.now,
      ),
    ));
  }

  Widget _content(List<ClassStudent> students) {
    final palette = context.palette;
    final colors = classColors(widget.myClass.section.id);
    final q = _query.trim().toLowerCase();
    final shown = q.isEmpty
        ? students
        : students.where((s) => s.nameAr.toLowerCase().contains(q) || s.nameEn.toLowerCase().contains(q)).toList();

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(gradient: LinearGradient(colors: colors), borderRadius: BorderRadius.circular(22)),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(widget.myClass.subjects.map((s) => s.subjectName).join(' · '),
                  style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5)),
              const SizedBox(height: 12),
              Row(
                children: [
                  _stat(Icons.groups_rounded, '${students.length}', tr('طالب', 'Students')),
                  _stat(Icons.boy_rounded, '${students.where((s) => s.isMale).length}', tr('بنين', 'Boys')),
                  _stat(Icons.girl_rounded, '${students.where((s) => s.isFemale).length}', tr('بنات', 'Girls')),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        if (students.isNotEmpty) ...[
          TextField(
            key: const ValueKey('students-search'),
            onChanged: (v) => setState(() => _query = v),
            decoration: InputDecoration(
              hintText: tr('ابحث باسم الطالب', 'Search by student name'),
              prefixIcon: const Icon(Icons.search_rounded),
              isDense: true,
            ),
          ),
          const SizedBox(height: 12),
        ],
        if (students.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 50),
            child: _message(context, Icons.person_off_rounded, tr('لا يوجد طلاب في هذا الفصل.', 'There are no students in this class.')),
          )
        else if (shown.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: Text(tr('لا يوجد طالب بهذا الاسم.', 'No student with this name.'),
                textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
          )
        else
          for (final s in shown) ...[
            _studentTile(students.indexOf(s) + 1, s),
            const SizedBox(height: 8),
          ],
      ],
    );
  }

  Widget _stat(IconData icon, String value, String label) => Expanded(
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: Colors.white, size: 22),
            const SizedBox(width: 4),
            Column(
              children: [
                Text(value, style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
                Text(label, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 11)),
              ],
            ),
          ],
        ),
      );

  Color _genderColor(ClassStudent s) => s.isFemale ? _girl : s.isMale ? _boy : AppTheme.blue;

  String _initial(String name) => name.trim().isEmpty ? '?' : name.trim().characters.first;

  Widget _studentTile(int number, ClassStudent s) {
    final palette = context.palette;
    final color = _genderColor(s);
    final age = s.ageOn(_now);
    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        key: ValueKey('student-${s.id}'),
        borderRadius: BorderRadius.circular(16),
        onTap: () => _details(s),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          decoration: BoxDecoration(borderRadius: BorderRadius.circular(16), border: Border.all(color: palette.line)),
          child: Row(
            children: [
              SizedBox(
                width: 22,
                child: Text('$number', textAlign: TextAlign.center, style: TextStyle(fontSize: 12, color: palette.faint)),
              ),
              const SizedBox(width: 6),
              CircleAvatar(
                radius: 20,
                backgroundColor: color.withValues(alpha: .14),
                child: Text(_initial(s.name), style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 16)),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(s.name, maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14.5, color: palette.ink)),
                    if (age != null)
                      Text(tr('$age سنة', '$age years old'), style: TextStyle(fontSize: 12, color: palette.muted)),
                  ],
                ),
              ),
              Icon(s.isFemale ? Icons.girl_rounded : s.isMale ? Icons.boy_rounded : Icons.person_rounded, color: color),
              // The student's attendance, reviews and exams
              IconButton(
                key: ValueKey('activity-${s.id}'),
                tooltip: tr('نشاط الطالب', 'Student activity'),
                visualDensity: VisualDensity.compact,
                onPressed: () => _openActivity(s),
                icon: const Icon(Icons.insights_rounded, color: AppTheme.blue),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// The student's activity: profile, the week's attendance with the teachers' reviews, exams
  void _openActivity(ClassStudent s) {
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => StudentActivityScreen(
        studentId: s.id,
        studentName: s.name,
        classLabel: widget.myClass.label,
        api: widget.activityApi,
        onSessionExpired: widget.onSessionExpired,
        now: widget.now,
      ),
    ));
  }

  void _details(ClassStudent s) {
    final color = _genderColor(s);
    final age = s.ageOn(_now);
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) {
        final palette = sheetContext.palette;
        Widget row(IconData icon, String label, String value) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Row(
                children: [
                  Icon(icon, size: 20, color: color),
                  const SizedBox(width: 10),
                  Text(label, style: TextStyle(color: palette.muted, fontSize: 13)),
                  const Spacer(),
                  Flexible(child: Text(value, textAlign: TextAlign.end, style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink))),
                ],
              ),
            );
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircleAvatar(
                  radius: 34,
                  backgroundColor: color.withValues(alpha: .14),
                  child: Text(_initial(s.name), style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 26)),
                ),
                const SizedBox(height: 10),
                Text(s.name, textAlign: TextAlign.center, style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: palette.ink)),
                // The name in the other language
                if (pickLang(s.nameEn, s.nameAr).isNotEmpty && pickLang(s.nameEn, s.nameAr) != s.name)
                  Text(pickLang(s.nameEn, s.nameAr), style: TextStyle(color: palette.muted)),
                const SizedBox(height: 18),
                row(Icons.meeting_room_rounded, tr('الفصل', 'Class'), widget.myClass.label),
                if (s.isMale || s.isFemale)
                  row(s.isFemale ? Icons.girl_rounded : Icons.boy_rounded, tr('النوع', 'Gender'),
                      s.isFemale ? tr('أنثى', 'Female') : tr('ذكر', 'Male')),
                if (s.dateOfBirth != null) row(Icons.cake_rounded, tr('تاريخ الميلاد', 'Date of birth'), '${homeworkDate(s.dateOfBirth!)} ${s.dateOfBirth!.year}'),
                if (age != null) row(Icons.hourglass_bottom_rounded, tr('العمر', 'Age'), tr('$age سنة', '$age years')),
                const SizedBox(height: 4),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    key: const ValueKey('student-activity-button'),
                    onPressed: () {
                      Navigator.of(sheetContext).pop();
                      _openActivity(s);
                    },
                    icon: const Icon(Icons.insights_rounded),
                    label: Text(tr('نشاط الطالب: الحضور والتقييمات والاختبارات', 'Student activity: attendance, reviews, exams')),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

Widget _message(BuildContext context, IconData icon, String text, {Future<void> Function()? retry}) => Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry != null ? _red : _teal.withValues(alpha: .6)),
            const SizedBox(height: 12),
            Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.ink)),
            if (retry != null) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: retry, child: Text(tr('إعادة المحاولة', 'Try again'))),
            ],
          ],
        ),
      ),
    );
