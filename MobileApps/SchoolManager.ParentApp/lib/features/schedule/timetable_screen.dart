import 'dart:async';

import 'package:flutter/material.dart';

import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import 'schedule_format.dart';
import 'schedule_service.dart';
import 'schedule_timeline.dart';
import '../../core/i18n/app_language.dart';

enum _ViewMode { day, week }

/// A child's timetable: one day at a time (today by default, live "now / next"), or the whole week.
class TimetableScreen extends StatefulWidget {
  const TimetableScreen({super.key, required this.child, this.loadTimetable, this.onSessionExpired});

  final Child child;

  /// Defaults to the API (tests pass their own)
  final Future<ChildTimetable> Function(String studentId)? loadTimetable;

  /// Called when the token was rejected (the page closes first)
  final VoidCallback? onSessionExpired;

  @override
  State<TimetableScreen> createState() => _TimetableScreenState();
}

class _TimetableScreenState extends State<TimetableScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  Color get _muted => context.palette.muted;

  ChildTimetable? _timetable;
  String? _error;
  bool _loading = true;
  _ViewMode _mode = _ViewMode.day;
  int? _selectedDay;

  /// "Now" moves on from the server's time by the time passed since loading
  DateTime _loadedAt = DateTime.now();
  Timer? _ticker;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  Future<void> _load() async {
    _ticker?.cancel();
    setState(() {
      _loading = _timetable == null;
      _error = null;
    });
    try {
      final loader = widget.loadTimetable ?? ScheduleService.instance.getChildTimetable;
      final result = await loader(widget.child.id);
      if (!mounted) return;
      setState(() {
        _timetable = result;
        _loadedAt = DateTime.now();
        _loading = false;
        // Keep the chosen day after a refresh; otherwise today (or the next day with lessons)
        if (_selectedDay == null || result.dayOf(_selectedDay!) == null) _selectedDay = _defaultDay(result);
      });
      // Live statuses on today
      _ticker = Timer.periodic(const Duration(seconds: 30), (_) {
        if (mounted) setState(() {});
      });
    } on ScheduleException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired) {
        Navigator.of(context).pop();
        widget.onSessionExpired?.call();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  /// School time now (minutes since midnight)
  int get _now => (_timetable?.now ?? 0) + DateTime.now().difference(_loadedAt).inMinutes;

  bool _isToday(int dayOfWeek) => _timetable?.today == dayOfWeek;

  /// Today when it has lessons left, else the next day with lessons (wrapping around the week)
  int? _defaultDay(ChildTimetable timetable) {
    if (timetable.days.isEmpty) return null;
    final today = timetable.dayOf(timetable.today);
    if (today != null && today.slots.any((s) => s.end > timetable.now)) return timetable.today;
    for (var offset = 1; offset <= 7; offset++) {
      final day = (timetable.today + offset) % 7;
      if (timetable.dayOf(day) != null) return day;
    }
    return timetable.days.first.dayOfWeek;
  }

  // ===== Build =====

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('الجدول الدراسي', 'Timetable'), style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(
              [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _centerMessage(Icons.error_outline_rounded, _error!, retry: true)
                : _content(_timetable!),
      ),
    );
  }

  Widget _content(ChildTimetable timetable) {
    if (timetable.days.isEmpty) {
      return _centerMessage(Icons.event_busy_rounded, tr('لا يوجد جدول دراسي لفصل الطالب حالياً.', 'There is no timetable for the student’s class yet.'));
    }

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 16, 6),
          child: SizedBox(
            width: double.infinity,
            child: SegmentedButton<_ViewMode>(
              segments: [
                ButtonSegment(value: _ViewMode.day, icon: Icon(Icons.today_rounded), label: Text(tr('اليوم', 'Today'))),
                ButtonSegment(value: _ViewMode.week, icon: Icon(Icons.view_week_rounded), label: Text(tr('كل الأيام', 'All days'))),
              ],
              selected: {_mode},
              showSelectedIcon: false,
              onSelectionChanged: (s) => setState(() => _mode = s.first),
            ),
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _load,
            child: _mode == _ViewMode.day ? _dayView(timetable) : _weekView(timetable),
          ),
        ),
      ],
    );
  }

  // ----- One day -----

  Widget _dayView(ChildTimetable timetable) {
    final day = timetable.dayOf(_selectedDay ?? timetable.days.first.dayOfWeek) ?? timetable.days.first;
    final isToday = _isToday(day.dayOfWeek);

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        // Today is not a school day: say so once (the page shows the next school day)
        if (timetable.dayOf(timetable.today) == null)
          Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Row(
              children: [
                Icon(Icons.info_outline_rounded, size: 18, color: _muted),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(tr('لا توجد حصص اليوم (${dayNames[timetable.today]}).', 'No lessons today (${dayNames[timetable.today]}).'), style: TextStyle(color: _muted)),
                ),
              ],
            ),
          ),
        _dayChips(timetable, day.dayOfWeek),
        const SizedBox(height: 14),
        _daySummary(day, isToday),
        const SizedBox(height: 14),
        ScheduleTimeline(
          key: ValueKey('day-${day.dayOfWeek}'),
          slots: day.slots,
          isToday: isToday,
          now: _now,
        ),
      ],
    );
  }

  /// Days of the week that have lessons; today is marked
  Widget _dayChips(ChildTimetable timetable, int selected) {
    return SizedBox(
      // Two lines (day + count) with room for larger system fonts
      height: 76,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: timetable.days.length,
        separatorBuilder: (_, __) => const SizedBox(width: 8),
        itemBuilder: (context, i) {
          final day = timetable.days[i];
          final isSelected = day.dayOfWeek == selected;
          final isToday = _isToday(day.dayOfWeek);
          return InkWell(
            borderRadius: BorderRadius.circular(16),
            onTap: () => setState(() => _selectedDay = day.dayOfWeek),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 180),
              width: 78,
              padding: const EdgeInsets.symmetric(vertical: 6),
              decoration: BoxDecoration(
                color: isSelected ? AppTheme.blue : context.palette.card,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: isSelected ? AppTheme.blue : (isToday ? AppTheme.blue.withValues(alpha: .5) : context.palette.line)),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(dayNames[day.dayOfWeek],
                      style: TextStyle(fontWeight: FontWeight.bold, color: isSelected ? Colors.white : context.palette.ink)),
                  const SizedBox(height: 2),
                  Text(
                    isToday ? tr('اليوم', 'Today') : lessonsCount(day.slots.length),
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: isToday ? FontWeight.bold : null,
                      color: isSelected ? Colors.white70 : (isToday ? AppTheme.blue : _muted),
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  /// Day name, lessons count, school hours and (today) lessons done
  Widget _daySummary(TimetableDay day, bool isToday) {
    final total = day.slots.length;
    final done = isToday ? day.slots.where((s) => s.end <= _now).length : 0;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(color: context.palette.card, borderRadius: BorderRadius.circular(18)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(color: AppTheme.blue.withValues(alpha: .10), borderRadius: BorderRadius.circular(12)),
                child: Icon(isToday ? Icons.today_rounded : Icons.calendar_month_rounded, color: AppTheme.blue, size: 20),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(isToday ? tr('اليوم - ${dayNames[day.dayOfWeek]}', 'Today - ${dayNames[day.dayOfWeek]}') : dayNames[day.dayOfWeek],
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: context.palette.ink)),
                    Text(
                      '${lessonsCount(total)} · ${formatTime(day.slots.first.start)} - ${formatTime(day.slots.last.end)}',
                      style: TextStyle(fontSize: 11.5, color: _muted),
                    ),
                  ],
                ),
              ),
              if (isToday) Text('$done/$total', style: const TextStyle(fontWeight: FontWeight.bold, color: AppTheme.blue)),
            ],
          ),
          if (isToday) ...[
            const SizedBox(height: 10),
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: total == 0 ? 0 : done / total,
                minHeight: 6,
                backgroundColor: context.palette.chip,
                color: AppTheme.blue,
              ),
            ),
          ],
        ],
      ),
    );
  }

  // ----- Whole week -----

  Widget _weekView(ChildTimetable timetable) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        for (final day in timetable.days) ...[
          _weekDayCard(day),
          const SizedBox(height: 12),
        ],
      ],
    );
  }

  /// One day of the week: header (tap = open that day) and compact lesson rows
  Widget _weekDayCard(TimetableDay day) {
    final isToday = _isToday(day.dayOfWeek);
    return Container(
      decoration: BoxDecoration(
        color: context.palette.card,
        borderRadius: BorderRadius.circular(18),
        border: isToday ? Border.all(color: AppTheme.blue.withValues(alpha: .5), width: 1.5) : null,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          InkWell(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(18)),
            onTap: () => setState(() {
              _selectedDay = day.dayOfWeek;
              _mode = _ViewMode.day;
            }),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(14, 12, 10, 8),
              child: Row(
                children: [
                  Text(dayNames[day.dayOfWeek],
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.ink)),
                  if (isToday) ...[
                    const SizedBox(width: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(color: AppTheme.blue, borderRadius: BorderRadius.circular(10)),
                      child: Text(tr('اليوم', 'Today'), style: TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
                    ),
                  ],
                  const Spacer(),
                  Text(lessonsCount(day.slots.length), style: TextStyle(fontSize: 12, color: _muted)),
                  Icon(Icons.chevron_left_rounded, color: _muted),
                ],
              ),
            ),
          ),
          Divider(height: 1, color: context.palette.divider),
          for (var i = 0; i < day.slots.length; i++) _weekLessonRow(day.slots[i], i + 1, isToday),
          const SizedBox(height: 6),
        ],
      ),
    );
  }

  Widget _weekLessonRow(ScheduleSlot slot, int period, bool isToday) {
    final status = slot.statusAt(_now, isToday: isToday);
    final isNow = status == SlotStatus.current;
    final isDone = status == SlotStatus.finished;
    final color = subjectColor(slot);
    final textColor = isDone ? context.palette.faint : context.palette.ink;

    return Container(
      color: isNow ? color.withValues(alpha: .07) : null,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      child: Row(
        children: [
          // Period number in the subject colour
          CircleAvatar(
            radius: 13,
            backgroundColor: color.withValues(alpha: isDone ? .08 : .14),
            child: Text('$period', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: isDone ? textColor : color)),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(slot.title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontWeight: FontWeight.bold, color: textColor)),
                if (slot.teacher.isNotEmpty)
                  Text(slot.teacher,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 11.5, color: isDone ? textColor : _muted)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(formatTime(slot.start), style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: textColor)),
              Text(formatTime(slot.end), style: TextStyle(fontSize: 11, color: isDone ? textColor : _muted)),
            ],
          ),
          if (isNow) ...[
            const SizedBox(width: 6),
            const Icon(Icons.circle, size: 9, color: Color(0xFF13864B)),
          ],
        ],
      ),
    );
  }

  Widget _centerMessage(IconData icon, String text, {bool retry = false}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry ? const Color(0xFFD93025) : AppTheme.blue.withValues(alpha: .6)),
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
}
