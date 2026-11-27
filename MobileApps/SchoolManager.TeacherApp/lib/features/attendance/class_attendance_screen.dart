import 'package:flutter/material.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../classes/classes_screens.dart' show classColors;
import '../classes/classes_service.dart';
import '../timetable/teacher_timetable_screen.dart' show lessonTime;
import 'attendance_screens.dart';
import 'attendance_service.dart';
import '../../core/live/live_refresh.dart';

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _purple = Color(0xFF6A3FD8);
const _grey = Color(0xFF8A94A6);

/// Colour of a day status
Color dayStatusColor(DayAttendanceStatus status) => switch (status) {
      DayAttendanceStatus.present => _green,
      DayAttendanceStatus.absent => _red,
      DayAttendanceStatus.partiallyPresent => _orange,
      DayAttendanceStatus.excused => _purple,
    };

/// Filter of the student list (null status = nothing recorded)
enum _Filter { all, present, absent, partiallyPresent, excused, notRecorded }

/// One class on a day: every student with their day status, lessons attended / missed and each lesson's status
/// (lessons of every teacher of the class). No future days.
class ClassAttendanceScreen extends StatefulWidget {
  const ClassAttendanceScreen({super.key, required this.myClass, this.api = const AttendanceApi(), this.onSessionExpired, this.today});

  final MyClass myClass;
  final AttendanceApi api;
  final Future<void> Function()? onSessionExpired;

  /// "Today" (tests pass their own)
  final DateTime? today;

  @override
  State<ClassAttendanceScreen> createState() => _ClassAttendanceScreenState();
}

class _ClassAttendanceScreenState extends State<ClassAttendanceScreen> with LiveRefreshMixin {
  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _load();

  List<ClassStudent>? _students;
  Map<String, StudentDay> _days = const {};
  bool _loading = true;
  bool _loadingDay = false;
  String? _error;
  late DateTime _date;
  _Filter _filter = _Filter.all;

  /// Students whose lessons are shown
  final Set<String> _expanded = {};

  DateTime get _today {
    final t = widget.today ?? DateTime.now();
    return DateTime(t.year, t.month, t.day);
  }

  String get _sectionId => widget.myClass.section.id;

  @override
  void initState() {
    super.initState();
    _date = _today;
    _load();
  }

  /// The class's students (once), then the chosen day
  Future<void> _load() async {
    setState(() {
      _loading = _students == null;
      _error = null;
    });
    try {
      final students = await widget.api.students(_sectionId);
      if (!mounted) return;
      setState(() => _students = [...students]..sort((a, b) => a.name.compareTo(b.name)));
      await _loadDay();
      if (mounted) setState(() => _loading = false);
    } on AttendanceException catch (e) {
      await _failed(e);
    }
  }

  Future<void> _loadDay() async {
    final date = _date;
    setState(() {
      _loadingDay = true;
      _error = null;
    });
    try {
      final days = await widget.api.classDays(_sectionId, date);
      if (!mounted || date != _date) return;
      setState(() {
        _days = {for (final d in days) d.studentId: d};
        _expanded.clear();
        _loadingDay = false;
      });
    } on AttendanceException catch (e) {
      await _failed(e);
    }
  }

  Future<void> _failed(AttendanceException e) async {
    if (!mounted) return;
    if (e.sessionExpired && widget.onSessionExpired != null) {
      await widget.onSessionExpired!();
      return;
    }
    setState(() {
      _error = e.message;
      _loading = false;
      _loadingDay = false;
    });
  }

  void _setDate(DateTime date) {
    final d = DateTime(date.year, date.month, date.day);
    if (d.isAfter(_today) || d == _date) return;
    setState(() => _date = d);
    _loadDay();
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

  int _countOf(DayAttendanceStatus? status) {
    final students = _students ?? const <ClassStudent>[];
    return students.where((s) => _days[s.id]?.status == status).length;
  }

  bool _matches(ClassStudent s) {
    final day = _days[s.id];
    return switch (_filter) {
      _Filter.all => true,
      _Filter.notRecorded => day == null,
      _Filter.present => day?.status == DayAttendanceStatus.present,
      _Filter.absent => day?.status == DayAttendanceStatus.absent,
      _Filter.partiallyPresent => day?.status == DayAttendanceStatus.partiallyPresent,
      _Filter.excused => day?.status == DayAttendanceStatus.excused,
    };
  }

  /// Subject name of a lesson when the teacher teaches it, else "lesson"
  String _subjectName(String subjectId) {
    for (final s in widget.myClass.subjects) {
      if (s.subjectId == subjectId && s.subjectName.isNotEmpty) return s.subjectName;
    }
    return tr('حصة', 'Lesson');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('حضور الفصل', 'Class attendance'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(widget.myClass.label, style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && _students == null
              ? _message(Icons.error_outline_rounded, _error!, retry: _load)
              : RefreshIndicator(onRefresh: _loadDay, child: _content()),
    );
  }

  Widget _content() {
    final palette = context.palette;
    final students = _students ?? const <ClassStudent>[];
    final recorded = students.where((s) => _days.containsKey(s.id)).length;
    final attended = _countOf(DayAttendanceStatus.present) + _countOf(DayAttendanceStatus.partiallyPresent);
    final rate = recorded == 0 ? null : (attended * 100 / recorded).round();
    final shown = students.where(_matches).toList();
    final isToday = _date == _today;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        // Day + summary
        Container(
          padding: const EdgeInsets.fromLTRB(8, 12, 8, 14),
          decoration: BoxDecoration(
            gradient: LinearGradient(colors: classColors(_sectionId)),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Column(
            children: [
              Row(
                children: [
                  IconButton(
                    key: const ValueKey('class-previous-day'),
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
                    key: const ValueKey('class-next-day'),
                    tooltip: tr('اليوم التالي', 'Next day'),
                    onPressed: isToday ? null : () => _setDate(_date.add(const Duration(days: 1))),
                    icon: Icon(Icons.chevron_right_rounded, color: isToday ? Colors.white38 : Colors.white),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  _stat('${students.length}', tr('طالب', 'Students')),
                  _stat('$recorded', tr('سُجّل', 'Recorded')),
                  _stat('${_countOf(DayAttendanceStatus.absent)}', tr('غائب', 'Absent')),
                  _stat(rate == null ? '—' : '$rate%', tr('نسبة الحضور', 'Attendance')),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(color: palette.errorTint, borderRadius: BorderRadius.circular(14)),
              child: Row(
                children: [
                  const Icon(Icons.error_outline_rounded, color: _red),
                  const SizedBox(width: 8),
                  Expanded(child: Text(_error!, style: TextStyle(color: palette.ink))),
                  TextButton(onPressed: _loadDay, child: Text(tr('إعادة', 'Retry'))),
                ],
              ),
            ),
          ),
        // Filters with counts
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              _filterChip(_Filter.all, tr('الكل', 'All'), students.length, AppTheme.blue),
              for (final s in DayAttendanceStatus.values)
                _filterChip(_Filter.values.byName(s.name), s.label, _countOf(s), dayStatusColor(s)),
              _filterChip(_Filter.notRecorded, tr('لم يُسجَّل', 'Not recorded'), _countOf(null), _grey),
            ],
          ),
        ),
        const SizedBox(height: 12),
        if (_loadingDay)
          const Padding(padding: EdgeInsets.only(top: 40), child: Center(child: CircularProgressIndicator()))
        else if (students.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 40),
            child: _message(Icons.person_off_rounded, tr('لا يوجد طلاب في هذا الفصل.', 'There are no students in this class.')),
          )
        else if (recorded == 0 && _filter == _Filter.all)
          ...[
            _hint(tr('لم يُسجَّل حضور هذا الفصل في هذا اليوم بعد.', 'No attendance recorded for this class on this day yet.')),
            const SizedBox(height: 10),
            for (var i = 0; i < students.length; i++) ...[
              _studentTile(i + 1, students[i]),
              const SizedBox(height: 8),
            ],
          ]
        else if (shown.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: Text(tr('لا يوجد طلاب بهذه الحالة.', 'No students with this status.'),
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

  Widget _stat(String value, String label) => Expanded(
        child: Column(
          children: [
            Text(value, style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
            Text(label, textAlign: TextAlign.center, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 11)),
          ],
        ),
      );

  Widget _filterChip(_Filter filter, String label, int count, Color color) {
    final palette = context.palette;
    final selected = _filter == filter;
    return Padding(
      padding: const EdgeInsetsDirectional.only(end: 8),
      child: InkWell(
        key: ValueKey('class-filter-${filter.name}'),
        borderRadius: BorderRadius.circular(20),
        onTap: () => setState(() => _filter = filter),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
          decoration: BoxDecoration(
            color: selected ? color : palette.card,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: selected ? color : palette.line),
          ),
          child: Text('$label ($count)',
              style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: selected ? Colors.white : color)),
        ),
      ),
    );
  }

  Widget _hint(String text) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: palette.infoTint, borderRadius: BorderRadius.circular(14)),
      child: Row(
        children: [
          const Icon(Icons.info_outline_rounded, color: AppTheme.blue),
          const SizedBox(width: 8),
          Expanded(child: Text(text, style: TextStyle(color: palette.ink, fontSize: 13))),
        ],
      ),
    );
  }

  Widget _studentTile(int number, ClassStudent student) {
    final palette = context.palette;
    final day = _days[student.id];
    final color = day == null ? _grey : dayStatusColor(day.status);
    final expanded = _expanded.contains(student.id);
    final canExpand = day != null && (day.lessons.isNotEmpty || day.notes != null);
    final details = [
      if (day != null && day.lessons.isNotEmpty) tr('${day.attendedLessons} من ${day.lessons.length} حصص', '${day.attendedLessons} of ${day.lessons.length} lessons'),
      if (day?.checkIn != null) '${tr('حضور', 'In')} ${lessonTime(day!.checkIn!)}',
      if (day?.checkOut != null) '${tr('انصراف', 'Out')} ${lessonTime(day!.checkOut!)}',
    ];

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        key: ValueKey('class-student-${student.id}'),
        borderRadius: BorderRadius.circular(16),
        onTap: canExpand
            ? () => setState(() => expanded ? _expanded.remove(student.id) : _expanded.add(student.id))
            : null,
        child: Container(
          padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: day == null || day.status == DayAttendanceStatus.present ? palette.line : color.withValues(alpha: .5)),
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
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(student.name,
                            maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
                        if (details.isNotEmpty)
                          Text(details.join(' · '), style: TextStyle(fontSize: 11.5, color: palette.muted)),
                      ],
                    ),
                  ),
                  _pill(day?.status.label ?? tr('لم يُسجَّل', 'Not recorded'), color),
                  if (canExpand)
                    Icon(expanded ? Icons.expand_less_rounded : Icons.expand_more_rounded, color: palette.faint),
                ],
              ),
              if (expanded && day != null) ...[
                const SizedBox(height: 8),
                Divider(height: 1, color: palette.divider),
                const SizedBox(height: 6),
                for (final lesson in day.lessons)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Icon(attendanceIcon(lesson.status), size: 18, color: attendanceColor(lesson.status)),
                        const SizedBox(width: 8),
                        SizedBox(
                          width: 64,
                          child: Text(lessonTime(lesson.start), style: TextStyle(fontSize: 12, color: palette.muted)),
                        ),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(_subjectName(lesson.subjectId), style: TextStyle(fontSize: 13, color: palette.ink)),
                              if ((lesson.absenceReason ?? '').isNotEmpty)
                                Text(lesson.absenceReason!, style: TextStyle(fontSize: 11.5, color: palette.muted)),
                            ],
                          ),
                        ),
                        _pill(lesson.status.label, attendanceColor(lesson.status)),
                      ],
                    ),
                  ),
                if (day.notes != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Row(
                      children: [
                        Icon(Icons.sticky_note_2_outlined, size: 18, color: palette.muted),
                        const SizedBox(width: 8),
                        Expanded(child: Text(day.notes!, style: TextStyle(fontSize: 12.5, color: palette.ink))),
                      ],
                    ),
                  ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _pill(String text, Color color) => Container(
        margin: const EdgeInsetsDirectional.only(start: 6),
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
        child: Text(text, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold)),
      );

  Widget _message(IconData icon, String text, {VoidCallback? retry}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry != null ? _red : _grey),
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
}
