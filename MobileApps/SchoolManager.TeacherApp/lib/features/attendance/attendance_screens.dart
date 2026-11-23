import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../classes/classes_service.dart';
import '../timetable/teacher_timetable_screen.dart' show lessonTime, subjectColorOf;
import '../timetable/teacher_timetable_service.dart';
import 'attendance_service.dart';

const _blue = Color(0xFF2F80ED);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _purple = Color(0xFF6A3FD8);

/// Colour of each status
Color attendanceColor(LessonAttendanceStatus status) => switch (status) {
      LessonAttendanceStatus.present => _green,
      LessonAttendanceStatus.absent => _red,
      LessonAttendanceStatus.late => _orange,
      LessonAttendanceStatus.excused => _purple,
    };

IconData attendanceIcon(LessonAttendanceStatus status) => switch (status) {
      LessonAttendanceStatus.present => Icons.check_circle_rounded,
      LessonAttendanceStatus.absent => Icons.cancel_rounded,
      LessonAttendanceStatus.late => Icons.schedule_rounded,
      LessonAttendanceStatus.excused => Icons.assignment_turned_in_rounded,
    };

List<String> get _dayNames => AppLanguage.instance.isEnglish
    ? const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
    : const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

List<String> get _monthNames => AppLanguage.instance.isEnglish
    ? const ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
    : const ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

/// 0 = Sunday … 6 = Saturday (as the timetable)
int _dayOfWeek(DateTime date) => date.weekday % 7;

DateTime _dateOnly(DateTime d) => DateTime(d.year, d.month, d.day);

/// "الأحد 21 نوفمبر" / "Sunday 21 Nov"
String attendanceDayLabel(DateTime date) => '${_dayNames[_dayOfWeek(date)]} ${date.day} ${_monthNames[date.month - 1]}';

/// The API calls the attendance screens use (tests pass their own)
class AttendanceApi {
  const AttendanceApi({this.loadTimetable, this.loadStudents, this.loadRecorded, this.save, this.loadClassDays});

  final Future<TeacherTimetable> Function()? loadTimetable;
  final Future<List<ClassStudent>> Function(String sectionId)? loadStudents;
  final Future<List<RecordedAttendance>> Function(TeacherLesson lesson, DateTime date)? loadRecorded;
  final Future<void> Function(TeacherLesson lesson, DateTime date, String academicYearId, List<StudentAttendanceInput> students)? save;
  final Future<List<StudentDay>> Function(String sectionId, DateTime date)? loadClassDays;

  Future<List<StudentDay>> classDays(String sectionId, DateTime date) =>
      (loadClassDays ?? AttendanceService.instance.getClassAttendance)(sectionId, date);

  Future<TeacherTimetable> timetable() async {
    try {
      return await (loadTimetable ?? TeacherTimetableService.instance.getMyTimetable)();
    } on TeacherTimetableException catch (e) {
      throw AttendanceException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<ClassStudent>> students(String sectionId) async {
    try {
      return await (loadStudents ?? ClassesService.instance.getStudents)(sectionId);
    } on ClassesException catch (e) {
      throw AttendanceException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<RecordedAttendance>> recorded(TeacherLesson lesson, DateTime date) =>
      (loadRecorded ?? AttendanceService.instance.getLessonAttendance)(lesson, date);

  Future<void> record(TeacherLesson lesson, DateTime date, String academicYearId, List<StudentAttendanceInput> students) =>
      save != null
          ? save!(lesson, date, academicYearId, students)
          : AttendanceService.instance.recordLesson(lesson: lesson, date: date, academicYearId: academicYearId, students: students);
}

// ---------------------------------------------------------------------------
// The day's lessons
// ---------------------------------------------------------------------------

/// The teacher's lessons on a day (today by default, no future days) and whether their attendance is taken
class AttendanceLessonsScreen extends StatefulWidget {
  const AttendanceLessonsScreen({super.key, this.api = const AttendanceApi(), this.onSessionExpired, this.today});

  final AttendanceApi api;
  final Future<void> Function()? onSessionExpired;

  /// "Today" (tests pass their own)
  final DateTime? today;

  @override
  State<AttendanceLessonsScreen> createState() => _AttendanceLessonsScreenState();
}

class _AttendanceLessonsScreenState extends State<AttendanceLessonsScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  TeacherTimetable? _timetable;
  bool _loading = true;
  String? _error;
  late DateTime _date;

  /// Lesson id -> saved rows on [_date] (missing: still loading / failed)
  final Map<String, List<RecordedAttendance>> _recorded = {};

  DateTime get _today => _dateOnly(widget.today ?? DateTime.now());

  @override
  void initState() {
    super.initState();
    _date = _today;
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _timetable == null;
      _error = null;
    });
    try {
      final timetable = await widget.api.timetable();
      if (!mounted) return;
      setState(() {
        _timetable = timetable;
        _loading = false;
      });
      await _loadRecorded();
    } on AttendanceException catch (e) {
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

  List<TeacherLesson> get _lessons => _timetable?.dayOf(_dayOfWeek(_date))?.lessons ?? const [];

  /// What was saved for each lesson of the day (a failing lesson just shows no status)
  Future<void> _loadRecorded() async {
    final date = _date;
    _recorded.clear();
    await Future.wait(_lessons.map((lesson) async {
      try {
        final rows = await widget.api.recorded(lesson, date);
        if (mounted && date == _date) setState(() => _recorded[lesson.id] = rows);
      } on AttendanceException catch (e) {
        if (e.sessionExpired && widget.onSessionExpired != null && mounted) await widget.onSessionExpired!();
      }
    }));
  }

  void _setDate(DateTime date) {
    final d = _dateOnly(date);
    if (d.isAfter(_today) || d == _date) return;
    setState(() => _date = d);
    _loadRecorded();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: _today.subtract(const Duration(days: 365)),
      lastDate: _today,
    );
    if (picked != null) _setDate(picked);
  }

  Future<void> _open(TeacherLesson lesson) async {
    final yearId = _timetable?.academicYearId ?? AcademicYearController.instance.selectedId;
    if (yearId == null || yearId.isEmpty) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(tr('لا توجد سنة دراسية.', 'There is no academic year.'))));
      return;
    }
    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => TakeAttendanceScreen(
        api: widget.api,
        lesson: lesson,
        date: _date,
        academicYearId: yearId,
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (saved == true && mounted) _loadRecorded();
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final lessons = _lessons;
    final done = lessons.where((l) => (_recorded[l.id] ?? const []).isNotEmpty).length;
    final isToday = _date == _today;

    return Scaffold(
      appBar: AppBar(title: Text(tr('الحضور والغياب', 'Attendance'), style: const TextStyle(fontWeight: FontWeight.bold))),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(Icons.error_outline_rounded, _error!, retry: true)
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
                    children: [
                      // Day + summary
                      Container(
                        padding: const EdgeInsets.fromLTRB(8, 12, 8, 14),
                        decoration: BoxDecoration(
                          gradient: const LinearGradient(colors: [_blue, Color(0xFF1B4FC4)]),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Column(
                          children: [
                            Row(
                              children: [
                                IconButton(
                                  key: const ValueKey('previous-day'),
                                  tooltip: tr('اليوم السابق', 'Previous day'),
                                  onPressed: () => _setDate(_date.subtract(const Duration(days: 1))),
                                  icon: const Icon(Icons.chevron_left_rounded, color: Colors.white),
                                ),
                                Expanded(
                                  child: InkWell(
                                    borderRadius: BorderRadius.circular(12),
                                    onTap: _pickDate,
                                    child: Padding(
                                      padding: const EdgeInsets.symmetric(vertical: 4),
                                      child: Column(
                                        children: [
                                          Text(attendanceDayLabel(_date),
                                              textAlign: TextAlign.center,
                                              style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
                                          Text(isToday ? tr('اليوم · اضغط لاختيار يوم آخر', 'Today · tap to choose another day') : tr('اضغط لاختيار يوم آخر', 'Tap to choose another day'),
                                              style: TextStyle(color: Colors.white.withValues(alpha: .8), fontSize: 11.5)),
                                        ],
                                      ),
                                    ),
                                  ),
                                ),
                                IconButton(
                                  key: const ValueKey('next-day'),
                                  tooltip: tr('اليوم التالي', 'Next day'),
                                  onPressed: isToday ? null : () => _setDate(_date.add(const Duration(days: 1))),
                                  icon: Icon(Icons.chevron_right_rounded, color: isToday ? Colors.white38 : Colors.white),
                                ),
                              ],
                            ),
                            if (lessons.isNotEmpty) ...[
                              const SizedBox(height: 10),
                              Padding(
                                padding: const EdgeInsets.symmetric(horizontal: 12),
                                child: ClipRRect(
                                  borderRadius: BorderRadius.circular(6),
                                  child: LinearProgressIndicator(
                                    value: done / lessons.length,
                                    minHeight: 6,
                                    color: Colors.white,
                                    backgroundColor: Colors.white.withValues(alpha: .25),
                                  ),
                                ),
                              ),
                              const SizedBox(height: 8),
                              Text(tr('تم أخذ الحضور في $done من ${lessons.length} حصص', 'Attendance taken in $done of ${lessons.length} lessons'),
                                  style: const TextStyle(color: Colors.white, fontSize: 12.5, fontWeight: FontWeight.w600)),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      if (lessons.isEmpty)
                        Padding(
                          padding: const EdgeInsets.only(top: 40),
                          child: Column(
                            children: [
                              Icon(Icons.event_available_rounded, size: 56, color: _blue.withValues(alpha: .5)),
                              const SizedBox(height: 12),
                              Text(tr('لا توجد لديك حصص في هذا اليوم.', 'You have no lessons on this day.'),
                                  textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
                            ],
                          ),
                        )
                      else
                        for (final lesson in lessons) ...[
                          _lessonCard(lesson),
                          const SizedBox(height: 10),
                        ],
                    ],
                  ),
                ),
    );
  }

  Widget _lessonCard(TeacherLesson lesson) {
    final palette = context.palette;
    final color = subjectColorOf(lesson.subject);
    final rows = _recorded[lesson.id];
    final taken = rows != null && rows.isNotEmpty;
    final absent = (rows ?? const []).where((r) => r.status == LessonAttendanceStatus.absent).length;
    final late = (rows ?? const []).where((r) => r.status == LessonAttendanceStatus.late).length;

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        key: ValueKey('lesson-${lesson.id}'),
        borderRadius: BorderRadius.circular(18),
        onTap: () => _open(lesson),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: taken ? _green.withValues(alpha: .45) : palette.line),
          ),
          child: Row(
            children: [
              Container(
                width: 64,
                padding: const EdgeInsets.symmetric(vertical: 10),
                decoration: BoxDecoration(color: color.withValues(alpha: .1), borderRadius: BorderRadius.circular(14)),
                child: Column(
                  children: [
                    Text(lessonTime(lesson.start), style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: color)),
                    Text(lessonTime(lesson.end), style: TextStyle(fontSize: 11, color: color)),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(lesson.subject, style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: palette.ink)),
                    if (lesson.classLabel.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 2),
                        child: Text(lesson.classLabel, style: TextStyle(fontSize: 12.5, color: palette.muted)),
                      ),
                    const SizedBox(height: 6),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        if (rows == null)
                          _pill(tr('...', '...'), palette.faint)
                        else if (!taken)
                          _pill(tr('لم يُؤخذ الحضور', 'Not taken'), _orange)
                        else ...[
                          _pill(tr('تم أخذ الحضور', 'Taken'), _green),
                          if (absent > 0) _pill(tr('$absent غائب', '$absent absent'), _red),
                          if (late > 0) _pill(tr('$late متأخر', '$late late'), _orange),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              Icon(AppLanguage.instance.isEnglish ? Icons.chevron_right_rounded : Icons.chevron_left_rounded, color: palette.faint),
            ],
          ),
        ),
      ),
    );
  }

  Widget _pill(String text, Color color) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
        child: Text(text, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold)),
      );

  Widget _message(IconData icon, String text, {bool retry = false}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry ? _red : _blue.withValues(alpha: .6)),
              const SizedBox(height: 12),
              Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.ink)),
              if (retry) ...[
                const SizedBox(height: 12),
                FilledButton(onPressed: _load, child: Text(tr('إعادة المحاولة', 'Try again'))),
              ],
            ],
          ),
        ),
      );
}

// ---------------------------------------------------------------------------
// Taking one lesson's attendance
// ---------------------------------------------------------------------------

/// The students of the lesson's class: present / absent / late / excused (with a reason), then save
class TakeAttendanceScreen extends StatefulWidget {
  const TakeAttendanceScreen({
    super.key,
    this.api = const AttendanceApi(),
    required this.lesson,
    required this.date,
    required this.academicYearId,
    this.onSessionExpired,
  });

  final AttendanceApi api;
  final TeacherLesson lesson;
  final DateTime date;
  final String academicYearId;
  final Future<void> Function()? onSessionExpired;

  @override
  State<TakeAttendanceScreen> createState() => _TakeAttendanceScreenState();
}

class _TakeAttendanceScreenState extends State<TakeAttendanceScreen> {
  List<ClassStudent> _students = const [];
  final Map<String, LessonAttendanceStatus> _status = {};
  final Map<String, TextEditingController> _reasons = {};

  /// True when this lesson was already saved on the date (saving again updates it)
  bool _alreadyTaken = false;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final c in _reasons.values) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait([
        widget.api.students(widget.lesson.sectionId),
        widget.api.recorded(widget.lesson, widget.date),
      ]);
      if (!mounted) return;
      final students = [...results[0] as List<ClassStudent>]..sort((a, b) => a.name.compareTo(b.name));
      final recorded = {for (final r in results[1] as List<RecordedAttendance>) r.studentId: r};

      for (final c in _reasons.values) {
        c.dispose();
      }
      _reasons.clear();
      _status.clear();
      for (final s in students) {
        // Not saved yet: present (the teacher only marks the others)
        _status[s.id] = recorded[s.id]?.status ?? LessonAttendanceStatus.present;
        _reasons[s.id] = TextEditingController(text: recorded[s.id]?.absenceReason ?? '');
      }
      setState(() {
        _students = students;
        _alreadyTaken = recorded.isNotEmpty;
        _loading = false;
      });
    } on AttendanceException catch (e) {
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

  void _setStatus(String studentId, LessonAttendanceStatus status) => setState(() => _status[studentId] = status);

  void _allPresent() => setState(() {
        for (final s in _students) {
          _status[s.id] = LessonAttendanceStatus.present;
        }
      });

  int _count(LessonAttendanceStatus status) => _status.values.where((s) => s == status).length;

  Future<void> _save() async {
    if (_students.isEmpty) return;
    final tooLong = _students.any((s) => (_status[s.id]?.hasReason ?? false) && _reasons[s.id]!.text.trim().length > maxAbsenceReasonLength);
    if (tooLong) {
      _show(tr('سبب الغياب طويل جداً (الحد $maxAbsenceReasonLength حرف).', 'The absence reason is too long (max $maxAbsenceReasonLength characters).'));
      return;
    }

    setState(() => _saving = true);
    try {
      await widget.api.record(
        widget.lesson,
        widget.date,
        widget.academicYearId,
        [
          for (final s in _students)
            StudentAttendanceInput(studentId: s.id, status: _status[s.id]!, absenceReason: _reasons[s.id]!.text),
        ],
      );
      if (!mounted) return;
      _show(tr('تم حفظ الحضور.', 'Attendance saved.'));
      Navigator.of(context).pop(true);
    } on AttendanceException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      _show(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final lesson = widget.lesson;

    return Scaffold(
      appBar: AppBar(
        title: Text(tr('أخذ الحضور', 'Take attendance'), style: const TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          if (!_loading && _error == null && _students.isNotEmpty)
            TextButton.icon(
              key: const ValueKey('all-present'),
              onPressed: _saving ? null : _allPresent,
              icon: const Icon(Icons.done_all_rounded, color: Colors.white),
              label: Text(tr('الكل حاضر', 'All present'), style: const TextStyle(color: Colors.white)),
            ),
        ],
      ),
      bottomNavigationBar: _loading || _error != null || _students.isEmpty
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: FilledButton.icon(
                  key: const ValueKey('save-attendance'),
                  onPressed: _saving ? null : _save,
                  style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(50)),
                  icon: _saving
                      ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.save_rounded),
                  label: Text(_alreadyTaken ? tr('تحديث الحضور', 'Update attendance') : tr('حفظ الحضور', 'Save attendance')),
                ),
              ),
            ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.error_outline_rounded, size: 56, color: _red),
                        const SizedBox(height: 12),
                        Text(_error!, textAlign: TextAlign.center),
                        const SizedBox(height: 12),
                        FilledButton(onPressed: _load, child: Text(tr('إعادة المحاولة', 'Try again'))),
                      ],
                    ),
                  ),
                )
              : ListView(
                  padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
                  children: [
                    // Lesson + counts
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(colors: [_blue, Color(0xFF1B4FC4)]),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(lesson.subject, style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 2),
                          Text(
                            [lesson.classLabel, attendanceDayLabel(widget.date), '${lessonTime(lesson.start)} - ${lessonTime(lesson.end)}']
                                .where((s) => s.isNotEmpty)
                                .join(' · '),
                            style: TextStyle(color: Colors.white.withValues(alpha: .88), fontSize: 12.5),
                          ),
                          if (_alreadyTaken) ...[
                            const SizedBox(height: 8),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                              decoration: BoxDecoration(color: Colors.white.withValues(alpha: .2), borderRadius: BorderRadius.circular(20)),
                              child: Text(tr('تم أخذ الحضور من قبل — يمكنك تعديله', 'Already taken — you can change it'),
                                  style: const TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.w600)),
                            ),
                          ],
                          const SizedBox(height: 14),
                          Row(
                            children: [
                              for (final s in LessonAttendanceStatus.values) _stat(s.label, '${_count(s)}'),
                            ],
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    if (_students.isEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 40),
                        child: Text(tr('لا يوجد طلاب في هذا الفصل.', 'There are no students in this class.'),
                            textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
                      )
                    else
                      for (var i = 0; i < _students.length; i++) ...[
                        _studentRow(i + 1, _students[i]),
                        const SizedBox(height: 8),
                      ],
                  ],
                ),
    );
  }

  Widget _stat(String label, String value) => Expanded(
        child: Column(
          children: [
            Text(value, style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
            Text(label, textAlign: TextAlign.center, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 11)),
          ],
        ),
      );

  Widget _studentRow(int number, ClassStudent student) {
    final palette = context.palette;
    final status = _status[student.id] ?? LessonAttendanceStatus.present;
    final color = attendanceColor(status);

    return Container(
      key: ValueKey('student-${student.id}'),
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
      decoration: BoxDecoration(
        color: status == LessonAttendanceStatus.present ? palette.card : color.withValues(alpha: .06),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: status == LessonAttendanceStatus.present ? palette.line : color.withValues(alpha: .5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 16,
                backgroundColor: palette.chip,
                child: Text('$number', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: palette.muted)),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(student.name,
                    maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
              ),
              Icon(attendanceIcon(status), color: color, size: 20),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              for (final s in LessonAttendanceStatus.values) ...[
                if (s != LessonAttendanceStatus.present) const SizedBox(width: 6),
                Expanded(child: _statusButton(student.id, s, selected: s == status)),
              ],
            ],
          ),
          if (status.hasReason) ...[
            const SizedBox(height: 8),
            TextField(
              key: ValueKey('reason-${student.id}'),
              controller: _reasons[student.id],
              maxLength: maxAbsenceReasonLength,
              textInputAction: TextInputAction.next,
              decoration: InputDecoration(
                isDense: true,
                counterText: '',
                prefixIcon: const Icon(Icons.edit_note_rounded, size: 20),
                hintText: tr('سبب الغياب (اختياري)', 'Absence reason (optional)'),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _statusButton(String studentId, LessonAttendanceStatus status, {required bool selected}) {
    final palette = context.palette;
    final color = attendanceColor(status);
    return InkWell(
      key: ValueKey('status-$studentId-${status.name}'),
      borderRadius: BorderRadius.circular(12),
      onTap: _saving ? null : () => _setStatus(studentId, status),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(vertical: 7),
        decoration: BoxDecoration(
          color: selected ? color : palette.card,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: selected ? color : palette.line),
        ),
        child: Text(
          status.label,
          textAlign: TextAlign.center,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: selected ? Colors.white : color),
        ),
      ),
    );
  }
}
