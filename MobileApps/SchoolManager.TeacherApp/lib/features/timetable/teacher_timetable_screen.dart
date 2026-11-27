import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'teacher_timetable_service.dart';
import '../../core/live/live_refresh.dart';

const _teal = Color(0xFF13A89E);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);

const _subjectColors = [
  Color(0xFF1677E8),
  Color(0xFF20A66A),
  Color(0xFF8957E5),
  Color(0xFFF08C00),
  Color(0xFFE0457B),
  Color(0xFF0FA5A5),
  Color(0xFF4C5FD5),
  Color(0xFFB7791F),
];

/// Same colour for a subject on every day
Color subjectColorOf(String subject) {
  final hash = subject.codeUnits.fold<int>(0, (sum, c) => (sum * 31 + c) & 0x7fffffff);
  return _subjectColors[hash % _subjectColors.length];
}

List<String> get _dayNames => AppLanguage.instance.isEnglish
    ? const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
    : const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

/// 510 -> "8:30 ص" / "8:30 AM"
String lessonTime(int minutes) {
  final h = (minutes ~/ 60) % 24;
  final m = (minutes % 60).toString().padLeft(2, '0');
  final h12 = h % 12 == 0 ? 12 : h % 12;
  return '$h12:$m ${h < 12 ? tr('ص', 'AM') : tr('م', 'PM')}';
}

/// 75 -> "ساعة و 15 دقيقة" / "1 h 15 min"
String _duration(int minutes) {
  final h = minutes ~/ 60;
  final m = minutes % 60;
  if (AppLanguage.instance.isEnglish) return [if (h > 0) '$h h', if (m > 0 || h == 0) '$m min'].join(' ');
  return [if (h > 0) (h == 1 ? 'ساعة' : '$h ساعات'), if (m > 0 || h == 0) '$m دقيقة'].join(' و ');
}

enum _LessonState { done, now, next, later }

/// The teacher's week for the academic year / semester chosen in the app: today's summary, day chips, lessons
class TeacherTimetableScreen extends StatefulWidget {
  const TeacherTimetableScreen({super.key, this.loadTimetable, this.onSessionExpired});

  /// Defaults to the API (tests pass their own)
  final Future<TeacherTimetable> Function()? loadTimetable;
  final Future<void> Function()? onSessionExpired;

  @override
  State<TeacherTimetableScreen> createState() => _TeacherTimetableScreenState();
}

class _TeacherTimetableScreenState extends State<TeacherTimetableScreen> with ReloadOnAcademicYear, LiveRefreshMixin {
  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _load();

  @override
  Future<void> reloadForAcademicYear() => _load();

  TeacherTimetable? _timetable;
  String? _error;
  bool _loading = true;

  /// Day shown (0 = Sunday … 6 = Saturday)
  int? _day;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _timetable == null;
      _error = null;
    });
    try {
      final loader = widget.loadTimetable ?? TeacherTimetableService.instance.getMyTimetable;
      final result = await loader();
      if (!mounted) return;
      setState(() {
        _timetable = result;
        _day = _initialDay(result);
        _loading = false;
      });
    } on TeacherTimetableException catch (e) {
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

  /// Today when it has lessons, else the next day that has
  static int _initialDay(TeacherTimetable t) {
    for (var i = 0; i < 7; i++) {
      final d = (t.today + i) % 7;
      if (t.dayOf(d) != null) return d;
    }
    return t.today;
  }

  _LessonState _stateOf(TeacherLesson lesson, List<TeacherLesson> lessons, bool isToday, int now) {
    if (!isToday) return _LessonState.later;
    if (lesson.end <= now) return _LessonState.done;
    if (lesson.start <= now) return _LessonState.now;
    final next = lessons.where((l) => l.start > now).firstOrNull;
    return next?.id == lesson.id ? _LessonState.next : _LessonState.later;
  }

  @override
  Widget build(BuildContext context) {
    final year = AcademicYearController.instance.value;
    final semester = AcademicYearController.instance.semester;
    final subtitle = [year?.name, semester?.name].whereType<String>().where((s) => s.isNotEmpty).join(' · ');

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('جدول حصصي', 'My timetable'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            if (subtitle.isNotEmpty) Text(subtitle, style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(Icons.error_outline_rounded, _error!, retry: true)
              : RefreshIndicator(onRefresh: _load, child: _content(_timetable!)),
    );
  }

  Widget _content(TeacherTimetable t) {
    if (t.days.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 80),
          _message(Icons.event_busy_rounded,
              tr('لا توجد حصص لك في هذه السنة / الفصل الدراسي.', 'You have no lessons in this academic year / semester.')),
        ],
      );
    }

    final day = _day ?? t.today;
    final lessons = t.dayOf(day)?.merged ?? const <TeacherLesson>[];
    final isToday = day == t.today;
    // Saturday first
    final weekOrder = [6, 0, 1, 2, 3, 4, 5];

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        _todayCard(t),
        const SizedBox(height: 16),
        // Day chips: days with lessons (and today)
        SizedBox(
          height: 64,
          child: ListView(
            scrollDirection: Axis.horizontal,
            children: [
              for (final d in weekOrder)
                if (t.dayOf(d) != null || d == t.today)
                  Padding(
                    padding: const EdgeInsetsDirectional.only(end: 8),
                    child: _dayChip(d, t.dayOf(d)?.merged.length ?? 0, selected: d == day, isToday: d == t.today),
                  ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        Row(
          children: [
            Text(isToday ? tr('اليوم - ${_dayNames[day]}', 'Today - ${_dayNames[day]}') : _dayNames[day],
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: context.palette.ink)),
            const Spacer(),
            Text(tr('${lessons.length} حصص', '${lessons.length} lessons'),
                style: TextStyle(fontSize: 12.5, color: context.palette.muted, fontWeight: FontWeight.w600)),
          ],
        ),
        const SizedBox(height: 10),
        if (lessons.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: _message(Icons.free_breakfast_rounded, tr('لا توجد حصص في هذا اليوم.', 'No lessons on this day.')),
          )
        else
          for (final lesson in lessons) ...[
            _lessonCard(lesson, _stateOf(lesson, lessons, isToday, t.now), t.now),
            const SizedBox(height: 10),
          ],
      ],
    );
  }

  /// Today: the lesson now (with progress) or the next one, or "done for today"
  Widget _todayCard(TeacherTimetable t) {
    final lessons = t.dayOf(t.today)?.merged ?? const <TeacherLesson>[];
    final now = t.now;
    final current = lessons.where((l) => l.start <= now && now < l.end).firstOrNull;
    final next = lessons.where((l) => l.start > now).firstOrNull;
    final done = lessons.where((l) => l.end <= now).length;

    final String badge;
    final String title;
    final String subtitle;
    String? footer;
    double? progress;

    if (lessons.isEmpty) {
      badge = tr('اليوم', 'Today');
      title = tr('لا توجد حصص اليوم', 'No lessons today');
      subtitle = tr('${t.lessonsCount} حصص في الأسبوع', '${t.lessonsCount} lessons this week');
    } else if (current != null) {
      badge = tr('الآن', 'Now');
      title = current.subject;
      subtitle = '${current.classLabel}  •  ${lessonTime(current.start)} - ${lessonTime(current.end)}';
      progress = ((now - current.start) / (current.end - current.start)).clamp(0, 1).toDouble();
      footer = tr('تنتهي بعد ${_duration(current.end - now)}', 'Ends in ${_duration(current.end - now)}');
      if (next != null) footer = '$footer  ·  ${tr('التالية', 'Next')}: ${next.subject} (${next.classLabel})';
    } else if (next != null) {
      badge = done == 0 ? tr('أول حصة', 'First lesson') : tr('التالية', 'Up next');
      title = next.subject;
      subtitle = '${next.classLabel}  •  ${lessonTime(next.start)} - ${lessonTime(next.end)}';
      footer = tr('تبدأ بعد ${_duration(next.start - now)}', 'Starts in ${_duration(next.start - now)}');
    } else {
      badge = tr('اليوم', 'Today');
      title = tr('انتهت حصص اليوم 🎉', 'Done for today 🎉');
      subtitle = tr('${lessons.length} حصص اليوم', '${lessons.length} lessons today');
      progress = 1;
    }

    return Container(
      key: const ValueKey('teacher-today-card'),
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        gradient: const LinearGradient(
          colors: [_teal, Color(0xFF1B5FAA)],
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
        ),
        boxShadow: [BoxShadow(color: _teal.withValues(alpha: .3), blurRadius: 16, offset: const Offset(0, 6))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(color: Colors.white.withValues(alpha: .22), borderRadius: BorderRadius.circular(20)),
                child: Text(badge, style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.bold)),
              ),
              const Spacer(),
              if (lessons.isNotEmpty)
                Text('$done/${lessons.length}',
                    style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 12, fontWeight: FontWeight.bold)),
            ],
          ),
          const SizedBox(height: 12),
          Text(title, style: const TextStyle(color: Colors.white, fontSize: 21, fontWeight: FontWeight.bold)),
          const SizedBox(height: 3),
          Text(subtitle, style: TextStyle(color: Colors.white.withValues(alpha: .88), fontSize: 13)),
          if (progress != null) ...[
            const SizedBox(height: 12),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: LinearProgressIndicator(
                value: progress,
                minHeight: 7,
                color: Colors.white,
                backgroundColor: Colors.white.withValues(alpha: .25),
              ),
            ),
          ],
          if (footer != null) ...[
            const SizedBox(height: 10),
            Text(footer, style: const TextStyle(color: Colors.white, fontSize: 12.5, fontWeight: FontWeight.w600)),
          ],
        ],
      ),
    );
  }

  Widget _dayChip(int day, int count, {required bool selected, required bool isToday}) {
    final palette = context.palette;
    return InkWell(
      key: ValueKey('day-$day'),
      borderRadius: BorderRadius.circular(16),
      onTap: () => setState(() => _day = day),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: 84,
        padding: const EdgeInsets.symmetric(vertical: 8),
        decoration: BoxDecoration(
          color: selected ? _teal : palette.card,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: selected ? _teal : (isToday ? _teal.withValues(alpha: .5) : palette.line)),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(_dayNames[day],
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12.5, color: selected ? Colors.white : palette.ink)),
            const SizedBox(height: 2),
            Text(isToday ? tr('اليوم', 'Today') : tr('$count حصص', '$count lessons'),
                style: TextStyle(fontSize: 10.5, color: selected ? Colors.white70 : palette.muted)),
          ],
        ),
      ),
    );
  }

  Widget _lessonCard(TeacherLesson lesson, _LessonState state, int now) {
    final palette = context.palette;
    final color = state == _LessonState.done ? palette.faint : subjectColorOf(lesson.subject);
    final (String? chip, Color chipColor) = switch (state) {
      _LessonState.now => (tr('الآن', 'Now'), _green),
      _LessonState.next => (tr('التالية', 'Next'), AppTheme.blue),
      _LessonState.done => (tr('انتهت', 'Done'), palette.faint),
      _LessonState.later => (null, palette.faint),
    };

    return Container(
      decoration: BoxDecoration(
        color: state == _LessonState.now ? _green.withValues(alpha: .07) : palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: state == _LessonState.now ? _green.withValues(alpha: .5) : palette.line),
      ),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Times
            Container(
              width: 74,
              padding: const EdgeInsets.symmetric(vertical: 12),
              decoration: BoxDecoration(
                color: color.withValues(alpha: .1),
                borderRadius: const BorderRadiusDirectional.horizontal(start: Radius.circular(18)),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(lessonTime(lesson.start), style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: color)),
                  Container(width: 2, height: 10, margin: const EdgeInsets.symmetric(vertical: 3), color: color.withValues(alpha: .4)),
                  Text(lessonTime(lesson.end), style: TextStyle(fontSize: 11.5, color: color)),
                ],
              ),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(lesson.subject,
                              style: TextStyle(
                                fontSize: 15.5,
                                fontWeight: FontWeight.bold,
                                color: state == _LessonState.done ? palette.muted : palette.ink,
                              )),
                        ),
                        if (chip != null)
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                            decoration: BoxDecoration(color: chipColor.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                            child: Text(chip, style: TextStyle(color: chipColor, fontSize: 11, fontWeight: FontWeight.bold)),
                          ),
                      ],
                    ),
                    if (lesson.classLabel.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(Icons.groups_rounded, size: 15, color: palette.muted),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(lesson.classLabel, style: TextStyle(fontSize: 12.5, color: palette.muted)),
                          ),
                        ],
                      ),
                    ],
                    if (state == _LessonState.now) ...[
                      const SizedBox(height: 8),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(6),
                        child: LinearProgressIndicator(
                          value: ((now - lesson.start) / (lesson.end - lesson.start)).clamp(0, 1).toDouble(),
                          minHeight: 5,
                          color: _green,
                          backgroundColor: palette.chip,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
          ],
        ),
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
