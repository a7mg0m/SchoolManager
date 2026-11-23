import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/academic_year_picker.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../schedule/schedule_service.dart';
import 'attendance_service.dart';

const _teal = Color(0xFF0FA5A5);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _purple = Color(0xFF6A3FD8);

Color dayStatusColor(DayStatus s) => switch (s) {
      DayStatus.present => _green,
      DayStatus.absent => _red,
      DayStatus.partiallyPresent => _orange,
      DayStatus.excused => _purple,
    };

Color lessonStatusColor(LessonStatus s) => switch (s) {
      LessonStatus.present => _green,
      LessonStatus.absent => _red,
      LessonStatus.late => _orange,
      LessonStatus.excused => _purple,
    };

IconData lessonStatusIcon(LessonStatus s) => switch (s) {
      LessonStatus.present => Icons.check_circle_rounded,
      LessonStatus.absent => Icons.cancel_rounded,
      LessonStatus.late => Icons.schedule_rounded,
      LessonStatus.excused => Icons.assignment_turned_in_rounded,
    };

List<String> get _dayNames => AppLanguage.instance.isEnglish
    ? const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
    : const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

List<String> get _monthNames => AppLanguage.instance.isEnglish
    ? const ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']
    : const ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

/// 510 -> "8:30 ص" / "8:30 AM"
String _time(int minutes) {
  final h = (minutes ~/ 60) % 24;
  final m = (minutes % 60).toString().padLeft(2, '0');
  final h12 = h % 12 == 0 ? 12 : h % 12;
  return '$h12:$m ${h < 12 ? tr('ص', 'AM') : tr('م', 'PM')}';
}

/// What the list shows
enum _Filter { all, absences, late }

/// Attendance of the child selected on home in the academic year chosen in the app: rate, days per status,
/// late / missed lessons, then each school day (newest first, by month) with its lessons.
class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({
    super.key,
    required this.child,
    this.loadAttendance,
    this.loadTimetable,
    this.onSessionExpired,
    this.now,
  });

  final Child child;

  /// Default to the API (tests pass their own)
  final Future<List<AttendanceDay>> Function(String studentId, DateTime from, DateTime to)? loadAttendance;

  /// Subject / teacher names of the lessons
  final Future<ChildTimetable> Function(String studentId)? loadTimetable;

  /// Called when the token was rejected
  final VoidCallback? onSessionExpired;

  /// Current time (tests pass a fixed one)
  final DateTime? now;

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  List<AttendanceDay>? _days;
  Map<String, ScheduleSlot> _slots = const {};
  String? _error;
  bool _loading = true;
  _Filter _filter = _Filter.all;
  final Set<DateTime> _expanded = {};

  DateTime get _today {
    final n = widget.now ?? DateTime.now();
    return DateTime(n.year, n.month, n.day);
  }

  /// The chosen year up to today (the current month when no year is known)
  (DateTime, DateTime) get _range {
    final year = AcademicYearController.instance.value;
    if (year == null) return (DateTime(_today.year, _today.month, 1), _today);
    final end = year.endDate.isAfter(_today) ? _today : year.endDate;
    return (year.startDate, end.isBefore(year.startDate) ? year.startDate : end);
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _days == null;
      _error = null;
    });
    final (from, to) = _range;
    try {
      final days = await (widget.loadAttendance ?? AttendanceService.instance.getChildAttendance)(widget.child.id, from, to);
      // Lesson names are a bonus: a failing timetable only shows "lesson"
      var slots = _slots;
      try {
        final timetable = await (widget.loadTimetable ?? ScheduleService.instance.getChildTimetable)(widget.child.id);
        slots = {for (final d in timetable.days) for (final s in d.slots) s.id: s};
      } catch (_) {}
      if (!mounted) return;
      setState(() {
        _days = [...days]..sort((a, b) => b.date.compareTo(a.date));
        _slots = slots;
        _expanded.clear();
        _loading = false;
      });
    } on AttendanceException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        widget.onSessionExpired!();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  String get _yearText => AcademicYearController.instance.value?.name ?? '';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        toolbarHeight: _yearText.isNotEmpty ? 70 : null,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('الحضور والغياب', 'Attendance'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(
              [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
            if (_yearText.isNotEmpty)
              Text(_yearText,
                  key: const ValueKey('attendance-year'),
                  style: const TextStyle(fontSize: 11.5, color: Colors.white70, fontWeight: FontWeight.w600)),
          ],
        ),
        actions: [
          // Change the academic year (the page reloads)
          IconButton(
            key: const ValueKey('attendance-change-year'),
            tooltip: tr('السنة الدراسية', 'Academic year'),
            icon: const Icon(Icons.date_range_rounded),
            onPressed: () => showAcademicYearSheet(context),
          ),
        ],
      ),
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _message(Icons.error_outline_rounded, _error!, retry: true)
                : RefreshIndicator(onRefresh: _load, child: _content(_days!)),
      ),
    );
  }

  Widget _content(List<AttendanceDay> days) {
    final palette = context.palette;
    int count(DayStatus s) => days.where((d) => d.status == s).length;
    final attended = count(DayStatus.present) + count(DayStatus.partiallyPresent);
    final rate = days.isEmpty ? null : (attended * 100 / days.length).round();
    final late = days.fold<int>(0, (sum, d) => sum + d.lateLessons);
    final missed = days.fold<int>(0, (sum, d) => sum + d.absentLessons);

    final shown = days.where((d) => switch (_filter) {
          _Filter.all => true,
          _Filter.absences => d.status != DayStatus.present || d.absentLessons > 0,
          _Filter.late => d.lateLessons > 0,
        });

    // Newest month first
    final byMonth = <DateTime, List<AttendanceDay>>{};
    for (final d in shown) {
      byMonth.putIfAbsent(DateTime(d.date.year, d.date.month), () => []).add(d);
    }

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        // Rate
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [Color(0xFF13A8A8), Color(0xFF0E7C8C)]),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Row(
            children: [
              SizedBox(
                width: 74,
                height: 74,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    CircularProgressIndicator(
                      value: (rate ?? 0) / 100,
                      strokeWidth: 7,
                      color: Colors.white,
                      backgroundColor: Colors.white.withValues(alpha: .25),
                    ),
                    Center(
                      child: Text(rate == null ? '—' : '$rate%',
                          key: const ValueKey('attendance-rate'),
                          style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(tr('نسبة الحضور', 'Attendance rate'),
                        style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text(
                      days.isEmpty
                          ? tr('لم يُسجَّل حضور بعد في هذه السنة.', 'No attendance recorded this year yet.')
                          : tr('${days.length} يوم دراسي مسجل', '${days.length} school days recorded'),
                      style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5),
                    ),
                    if (late > 0 || missed > 0) ...[
                      const SizedBox(height: 2),
                      Text(
                        [
                          if (missed > 0) tr('$missed حصة غياب', '$missed missed lessons'),
                          if (late > 0) tr('$late حصة تأخير', '$late late lessons'),
                        ].join(' · '),
                        style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        // Days per status
        Row(
          children: [
            for (final s in DayStatus.values) ...[
              if (s != DayStatus.present) const SizedBox(width: 8),
              Expanded(child: _statCard(s.label, count(s), dayStatusColor(s))),
            ],
          ],
        ),
        const SizedBox(height: 14),
        if (days.isNotEmpty) ...[
          Wrap(
            runSpacing: 8,
            children: [
              _filterChip(_Filter.all, tr('كل الأيام', 'All days')),
              _filterChip(_Filter.absences, tr('الغياب', 'Absences')),
              _filterChip(_Filter.late, tr('التأخير', 'Late')),
            ],
          ),
          const SizedBox(height: 12),
        ],
        if (days.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: _message(Icons.event_available_rounded,
                tr('لا يوجد حضور مسجل لـ ${widget.child.name} في هذه السنة الدراسية.', 'No attendance recorded for ${widget.child.name} in this academic year.')),
          )
        else if (byMonth.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: Text(
              _filter == _Filter.late ? tr('لا يوجد تأخير. ممتاز!', 'No late lessons. Well done!') : tr('لا يوجد غياب. ممتاز!', 'No absences. Well done!'),
              textAlign: TextAlign.center,
              style: TextStyle(color: palette.muted),
            ),
          )
        else
          for (final entry in byMonth.entries) ...[
            Padding(
              padding: const EdgeInsets.fromLTRB(4, 4, 4, 8),
              child: Text('${_monthNames[entry.key.month - 1]} ${entry.key.year}',
                  style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold, color: palette.ink)),
            ),
            for (final d in entry.value) ...[
              _dayCard(d),
              const SizedBox(height: 8),
            ],
            const SizedBox(height: 6),
          ],
      ],
    );
  }

  Widget _statCard(String label, int value, Color color) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .08),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color.withValues(alpha: .3)),
      ),
      child: Column(
        children: [
          Text('$value', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: color)),
          Text(label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 11, color: palette.muted)),
        ],
      ),
    );
  }

  Widget _filterChip(_Filter filter, String label) {
    final palette = context.palette;
    final selected = _filter == filter;
    return Padding(
      padding: const EdgeInsetsDirectional.only(end: 8),
      child: InkWell(
        key: ValueKey('attendance-filter-${filter.name}'),
        borderRadius: BorderRadius.circular(20),
        onTap: () => setState(() => _filter = filter),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
          decoration: BoxDecoration(
            color: selected ? _teal : palette.card,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: selected ? _teal : palette.line),
          ),
          child: Text(label, style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: selected ? Colors.white : palette.ink)),
        ),
      ),
    );
  }

  Widget _dayCard(AttendanceDay day) {
    final palette = context.palette;
    final color = dayStatusColor(day.status);
    final expanded = _expanded.contains(day.date);
    final canExpand = day.lessons.isNotEmpty || day.notes != null;
    final details = [
      if (day.lessons.isNotEmpty) tr('${day.attendedLessons} من ${day.lessons.length} حصص', '${day.attendedLessons} of ${day.lessons.length} lessons'),
      if (day.lateLessons > 0) tr('${day.lateLessons} تأخير', '${day.lateLessons} late'),
      if (day.checkIn != null) '${tr('حضور', 'In')} ${_time(day.checkIn!)}',
      if (day.checkOut != null) '${tr('انصراف', 'Out')} ${_time(day.checkOut!)}',
    ];

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        key: ValueKey('attendance-day-${attendanceDate(day.date)}'),
        borderRadius: BorderRadius.circular(16),
        onTap: canExpand ? () => setState(() => expanded ? _expanded.remove(day.date) : _expanded.add(day.date)) : null,
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: day.status == DayStatus.present ? palette.line : color.withValues(alpha: .5)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  // Date
                  Container(
                    width: 52,
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    decoration: BoxDecoration(color: color.withValues(alpha: .1), borderRadius: BorderRadius.circular(12)),
                    child: Column(
                      children: [
                        Text('${day.date.day}', style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: color)),
                        Text(_dayNames[day.date.weekday % 7],
                            maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 10, color: color)),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(day.status.label, style: TextStyle(fontWeight: FontWeight.bold, color: color)),
                        if (details.isNotEmpty)
                          Text(details.join(' · '), style: TextStyle(fontSize: 11.5, color: palette.muted)),
                      ],
                    ),
                  ),
                  if (canExpand) Icon(expanded ? Icons.expand_less_rounded : Icons.expand_more_rounded, color: palette.faint),
                ],
              ),
              if (expanded) ...[
                const SizedBox(height: 8),
                Divider(height: 1, color: palette.divider),
                const SizedBox(height: 6),
                for (final lesson in day.lessons) _lessonRow(lesson),
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

  Widget _lessonRow(AttendanceLesson lesson) {
    final palette = context.palette;
    final slot = _slots[lesson.timetableSlotId];
    final color = lessonStatusColor(lesson.status);
    final subtitle = [if ((slot?.teacher ?? '').isNotEmpty) slot!.teacher, if (lesson.absenceReason != null) lesson.absenceReason!];

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Icon(lessonStatusIcon(lesson.status), size: 18, color: color),
          const SizedBox(width: 8),
          SizedBox(width: 64, child: Text(_time(lesson.start), style: TextStyle(fontSize: 12, color: palette.muted))),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(slot?.title.isNotEmpty == true ? slot!.title : tr('حصة', 'Lesson'), style: TextStyle(fontSize: 13, color: palette.ink)),
                if (subtitle.isNotEmpty) Text(subtitle.join(' · '), style: TextStyle(fontSize: 11.5, color: palette.muted)),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
            child: Text(lesson.status.label, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  Widget _message(IconData icon, String text, {bool retry = false}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry ? _red : _teal.withValues(alpha: .6)),
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
