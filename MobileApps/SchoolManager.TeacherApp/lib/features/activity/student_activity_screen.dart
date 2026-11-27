import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/academic_year_picker.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'student_activity_service.dart';
import '../../core/live/live_refresh.dart';

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _amber = Color(0xFFF5A623);
const _purple = Color(0xFF6A3FD8);
const _girl = Color(0xFFD9427A);
const _boy = Color(0xFF1B4FC4);

List<String> get _dayNames => AppLanguage.instance.isEnglish
    ? const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
    : const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

List<String> get _monthShort => AppLanguage.instance.isEnglish
    ? const ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
    : const ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

String _time(int minutes) {
  final h = (minutes ~/ 60) % 24;
  final m = (minutes % 60).toString().padLeft(2, '0');
  final h12 = h % 12 == 0 ? 12 : h % 12;
  return '$h12:$m ${h < 12 ? tr('ص', 'AM') : tr('م', 'PM')}';
}

String _dayMonth(DateTime d) => '${d.day} ${_monthShort[d.month - 1]}';

/// The Saturday that starts [d]'s school week
DateTime _weekStart(DateTime d) {
  final day = DateTime(d.year, d.month, d.day);
  return day.subtract(Duration(days: (day.weekday - DateTime.saturday) % 7));
}

bool _sameDay(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;

Color _scoreColor(double score) => score >= 4 ? _green : (score >= 3 ? _amber : _red);

Color _dayColor(ActivityDayStatus s) => switch (s) {
      ActivityDayStatus.present => _green,
      ActivityDayStatus.absent => _red,
      ActivityDayStatus.partiallyPresent => _orange,
      ActivityDayStatus.excused => _purple,
    };

Color _lessonColor(ActivityLessonStatus s) => switch (s) {
      ActivityLessonStatus.present => _green,
      ActivityLessonStatus.absent => _red,
      ActivityLessonStatus.late => _orange,
      ActivityLessonStatus.excused => _purple,
    };

IconData _lessonIcon(ActivityLessonStatus s) => switch (s) {
      ActivityLessonStatus.present => Icons.check_circle_rounded,
      ActivityLessonStatus.absent => Icons.cancel_rounded,
      ActivityLessonStatus.late => Icons.schedule_rounded,
      ActivityLessonStatus.excused => Icons.assignment_turned_in_rounded,
    };

/// The API calls of the page (tests pass their own)
class StudentActivityApi {
  const StudentActivityApi({this.loadInfo, this.loadAttendance, this.loadReviews, this.loadExams});

  final Future<StudentActivityInfo> Function(String studentId)? loadInfo;
  final Future<List<ActivityDay>> Function(String studentId, DateTime from, DateTime to)? loadAttendance;
  final Future<List<ActivityReview>> Function(String studentId, DateTime from, DateTime to)? loadReviews;
  final Future<List<ActivityExam>> Function(String studentId)? loadExams;

  Future<StudentActivityInfo> info(String id) => (loadInfo ?? StudentActivityService.instance.getInfo)(id);
  Future<List<ActivityDay>> attendance(String id, DateTime from, DateTime to) =>
      (loadAttendance ?? StudentActivityService.instance.getAttendance)(id, from, to);
  Future<List<ActivityReview>> reviews(String id, DateTime from, DateTime to) =>
      (loadReviews ?? StudentActivityService.instance.getReviews)(id, from, to);
  Future<List<ActivityExam>> exams(String id) => (loadExams ?? StudentActivityService.instance.getExams)(id);
}

class _LessonEntry {
  _LessonEntry({this.lesson, this.slot, this.review, required this.subject});

  final ActivityLesson? lesson;
  final ActivitySlot? slot;
  final ActivityReview? review;
  final String subject;

  int get start => lesson?.start ?? slot?.start ?? 24 * 60;
}

/// Teacher app: one student of the teacher's class — their profile, the week's attendance with every teacher's
/// review of each lesson (move week by week), and their exams with the results. The academic year chosen in the app.
class StudentActivityScreen extends StatefulWidget {
  const StudentActivityScreen({
    super.key,
    required this.studentId,
    this.studentName = '',
    this.classLabel = '',
    this.api = const StudentActivityApi(),
    this.onSessionExpired,
    this.now,
  });

  final String studentId;

  /// Shown until the profile is loaded
  final String studentName;
  final String classLabel;

  final StudentActivityApi api;
  final Future<void> Function()? onSessionExpired;
  final DateTime? now;

  @override
  State<StudentActivityScreen> createState() => _StudentActivityScreenState();
}

class _StudentActivityScreenState extends State<StudentActivityScreen> with ReloadOnAcademicYear, LiveRefreshMixin {
  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _loadAll();

  @override
  Future<void> reloadForAcademicYear() => _loadAll();

  DateTime get _now => widget.now ?? DateTime.now();
  DateTime get _today => DateTime(_now.year, _now.month, _now.day);

  late DateTime _week = _weekStart(_today);

  StudentActivityInfo? _info;
  String? _infoError;

  List<ActivityDay> _days = const [];
  List<ActivityReview> _reviews = const [];
  bool _weekLoading = true;
  String? _weekError;

  List<ActivityExam>? _exams;
  String? _examsError;
  bool _allResults = false;

  Map<String, ActivitySlot> get _slots => {for (final d in _info?.days ?? const <ActivityTimetableDay>[]) for (final s in d.slots) s.id: s};

  @override
  void initState() {
    super.initState();
    _loadAll();
  }

  /// True when the session ended (the app signs out)
  bool _expired(Object e) {
    if (e is ActivityException && e.sessionExpired) {
      widget.onSessionExpired?.call();
      return true;
    }
    return false;
  }

  String _message(Object e) => e is ActivityException ? e.message : tr('تعذر تحميل البيانات.', 'Could not load the data.');

  Future<void> _loadAll() => Future.wait([_loadInfo(), _loadExams(), _loadWeek()]);

  Future<void> _loadInfo() async {
    try {
      final info = await widget.api.info(widget.studentId);
      if (mounted) {
        setState(() {
          _info = info;
          _infoError = null;
        });
      }
    } catch (e) {
      if (_expired(e) || !mounted) return;
      setState(() => _infoError = _message(e));
    }
  }

  Future<void> _loadExams() async {
    try {
      final exams = await widget.api.exams(widget.studentId);
      if (mounted) {
        setState(() {
          _exams = exams;
          _examsError = null;
        });
      }
    } catch (e) {
      if (_expired(e) || !mounted) return;
      setState(() => _examsError = _message(e));
    }
  }

  Future<void> _loadWeek() async {
    final from = _week;
    final to = _week.add(const Duration(days: 6));
    setState(() {
      _weekLoading = true;
      _weekError = null;
    });
    try {
      final results = await Future.wait<Object>([
        widget.api.attendance(widget.studentId, from, to),
        widget.api.reviews(widget.studentId, from, to),
      ]);
      if (!mounted || from != _week) return;
      setState(() {
        _days = results[0] as List<ActivityDay>;
        _reviews = results[1] as List<ActivityReview>;
        _weekLoading = false;
      });
    } catch (e) {
      if (_expired(e) || !mounted || from != _week) return;
      setState(() {
        _weekLoading = false;
        _weekError = _message(e);
      });
    }
  }

  void _moveWeek(int weeks) {
    setState(() => _week = _week.add(Duration(days: 7 * weeks)));
    _loadWeek();
  }

  String _subjectName(String subjectId, {ActivitySlot? slot}) {
    if (slot != null && slot.subject.isNotEmpty) return slot.subject;
    for (final t in _info?.teachers ?? const <ActivityTeacher>[]) {
      final name = t.subjects[subjectId];
      if (name != null && name.isNotEmpty) return name;
    }
    return tr('حصة', 'Lesson');
  }

  String? _teacherName(String teacherId) {
    for (final t in _info?.teachers ?? const <ActivityTeacher>[]) {
      if (t.id == teacherId) return t.name;
    }
    return null;
  }

  List<(DateTime, ActivityDay?, List<_LessonEntry>)> _weekDays() {
    final schoolDays = (_info?.days ?? const <ActivityTimetableDay>[]).map((d) => d.dayOfWeek).toSet();
    final slots = _slots;
    final result = <(DateTime, ActivityDay?, List<_LessonEntry>)>[];
    for (var i = 0; i < 7; i++) {
      final date = _week.add(Duration(days: i));
      final day = _days.where((d) => _sameDay(d.date, date)).firstOrNull;
      final reviews = _reviews.where((r) => _sameDay(r.date, date)).toList();
      final dow = date.weekday % 7;
      if (!schoolDays.contains(dow) && day == null && reviews.isEmpty) continue;

      final used = <ActivityReview>{};
      final entries = <_LessonEntry>[];
      if (day != null && day.lessons.isNotEmpty) {
        for (final lesson in day.lessons) {
          final slot = slots[lesson.timetableSlotId];
          final review = reviews.where((r) => r.subjectId == lesson.subjectId && !used.contains(r)).firstOrNull;
          if (review != null) used.add(review);
          entries.add(_LessonEntry(lesson: lesson, slot: slot, review: review, subject: _subjectName(lesson.subjectId, slot: slot)));
        }
      } else if (day == null) {
        for (final slot in _info?.dayOf(dow)?.slots ?? const <ActivitySlot>[]) {
          entries.add(_LessonEntry(slot: slot, subject: slot.subject.isNotEmpty ? slot.subject : tr('حصة', 'Lesson')));
        }
      }
      for (final r in reviews.where((r) => !used.contains(r))) {
        final i = entries.indexWhere((e) => e.review == null && e.lesson == null && e.subject == _subjectName(r.subjectId));
        if (i >= 0) {
          entries[i] = _LessonEntry(slot: entries[i].slot, review: r, subject: entries[i].subject);
        } else {
          entries.add(_LessonEntry(review: r, subject: _subjectName(r.subjectId)));
        }
      }
      entries.sort((a, b) => a.start.compareTo(b.start));
      result.add((date, day, entries));
    }
    return result;
  }

  @override
  Widget build(BuildContext context) {
    final name = _info?.student.name ?? widget.studentName;
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('نشاط الطالب', 'Student activity'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            if (name.isNotEmpty) Text(name, style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
        actions: [
          IconButton(
            key: const ValueKey('activity-change-year'),
            tooltip: tr('السنة الدراسية', 'Academic year'),
            icon: const Icon(Icons.date_range_rounded),
            onPressed: () => showAcademicYearSheet(context),
          ),
        ],
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _loadAll,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
            children: [
              _profileCard(),
              const SizedBox(height: 16),
              _weekHeader(),
              const SizedBox(height: 10),
              ..._weekBody(),
              const SizedBox(height: 18),
              _examsCard(),
            ],
          ),
        ),
      ),
    );
  }

  // ---------------- Profile ----------------

  Widget _profileCard() {
    final s = _info?.student;
    final name = s?.name ?? widget.studentName;
    final female = s?.isFemale ?? false;
    final colors = female ? const [Color(0xFFFF6B9A), _girl] : const [Color(0xFF2F80ED), _boy];
    final initial = name.trim().isEmpty ? '?' : name.trim().characters.first;
    final year = AcademicYearController.instance.value?.name ?? '';
    Widget chip(IconData icon, String text) => Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
          decoration: BoxDecoration(color: Colors.white.withValues(alpha: .18), borderRadius: BorderRadius.circular(20)),
          child: Row(mainAxisSize: MainAxisSize.min, children: [
            Icon(icon, size: 14, color: Colors.white),
            const SizedBox(width: 5),
            Text(text, style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w600)),
          ]),
        );

    return Container(
      key: const ValueKey('activity-profile'),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: LinearGradient(colors: colors, begin: AlignmentDirectional.topStart, end: AlignmentDirectional.bottomEnd),
        borderRadius: BorderRadius.circular(20),
        boxShadow: [BoxShadow(color: colors.last.withValues(alpha: .3), blurRadius: 16, offset: const Offset(0, 6))],
      ),
      child: Row(children: [
        CircleAvatar(
          radius: 32,
          backgroundColor: Colors.white,
          child: Text(initial, style: TextStyle(fontSize: 26, fontWeight: FontWeight.bold, color: colors.last)),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(name, style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            if (_infoError != null)
              Text(_infoError!, style: const TextStyle(color: Colors.white, fontSize: 12))
            else
              Wrap(spacing: 6, runSpacing: 6, children: [
                if ((s?.stageName ?? '').isNotEmpty) chip(Icons.account_balance_rounded, s!.stageName),
                if ((s?.gradeName ?? '').isNotEmpty) chip(Icons.school_rounded, s!.gradeName),
                if ((s?.sectionName ?? '').isNotEmpty)
                  chip(Icons.groups_rounded, s!.sectionName)
                else if (s == null && widget.classLabel.isNotEmpty)
                  chip(Icons.groups_rounded, widget.classLabel),
                if (year.isNotEmpty) chip(Icons.event_note_rounded, year),
              ]),
          ]),
        ),
      ]),
    );
  }

  // ---------------- Week ----------------

  Widget _weekHeader() {
    final palette = context.palette;
    final end = _week.add(const Duration(days: 6));
    final isThisWeek = _sameDay(_week, _weekStart(_today));
    final rtl = Directionality.of(context) == TextDirection.rtl;
    return Row(children: [
      const Icon(Icons.view_week_rounded, color: AppTheme.blue),
      const SizedBox(width: 8),
      Expanded(
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(tr('الحضور والتقييمات', 'Attendance & reviews'),
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: palette.ink)),
          Text('${_dayMonth(_week)} – ${_dayMonth(end)}',
              key: const ValueKey('activity-week-range'), style: TextStyle(fontSize: 12, color: palette.muted)),
        ]),
      ),
      if (!isThisWeek)
        TextButton(
          key: const ValueKey('activity-this-week'),
          onPressed: () {
            setState(() => _week = _weekStart(_today));
            _loadWeek();
          },
          child: Text(tr('هذا الأسبوع', 'This week')),
        ),
      IconButton(
        key: const ValueKey('activity-prev-week'),
        tooltip: tr('الأسبوع السابق', 'Previous week'),
        onPressed: () => _moveWeek(-1),
        icon: Icon(rtl ? Icons.chevron_right_rounded : Icons.chevron_left_rounded),
      ),
      IconButton(
        key: const ValueKey('activity-next-week'),
        tooltip: tr('الأسبوع التالي', 'Next week'),
        onPressed: () => _moveWeek(1),
        icon: Icon(rtl ? Icons.chevron_left_rounded : Icons.chevron_right_rounded),
      ),
    ]);
  }

  List<Widget> _weekBody() {
    if (_weekLoading) return const [Padding(padding: EdgeInsets.all(30), child: Center(child: CircularProgressIndicator()))];
    if (_weekError != null) return [_note(Icons.error_outline_rounded, _weekError!, retry: _loadWeek)];

    final days = _weekDays();
    final present = _days.where((d) => d.status == ActivityDayStatus.present || d.status == ActivityDayStatus.partiallyPresent).length;
    final absentLessons = _days.fold<int>(0, (s, d) => s + d.absentLessons);
    final late = _days.fold<int>(0, (s, d) => s + d.lateLessons);
    final avg = _reviews.isEmpty ? null : _reviews.map((r) => r.score).reduce((a, b) => a + b) / _reviews.length;

    return [
      Row(children: [
        _stat(Icons.how_to_reg_rounded, _green, '$present/${_days.length}', tr('أيام الحضور', 'Days present')),
        const SizedBox(width: 8),
        _stat(Icons.cancel_rounded, _red, '$absentLessons', tr('حصص غياب', 'Missed lessons')),
        const SizedBox(width: 8),
        _stat(Icons.schedule_rounded, _orange, '$late', tr('تأخير', 'Late')),
        const SizedBox(width: 8),
        _stat(Icons.star_rounded, _amber, avg == null ? '-' : avg.toStringAsFixed(1), tr('التقييم', 'Reviews'),
            key: const ValueKey('activity-review-average')),
      ]),
      const SizedBox(height: 12),
      if (days.isEmpty)
        _note(Icons.event_busy_rounded, tr('لا يوجد حضور أو تقييمات في هذا الأسبوع.', 'No attendance or reviews this week.'))
      else
        for (final (date, day, entries) in days) _dayCard(date, day, entries),
    ];
  }

  Widget _stat(IconData icon, Color color, String value, String label, {Key? key}) {
    final palette = context.palette;
    return Expanded(
      child: Container(
        key: key,
        padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 6),
        decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(14), border: Border.all(color: palette.line)),
        child: Column(children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(height: 4),
          Text(value, style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: palette.ink)),
          Text(label, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 10.5, color: palette.muted)),
        ]),
      ),
    );
  }

  Widget _dayCard(DateTime date, ActivityDay? day, List<_LessonEntry> entries) {
    final palette = context.palette;
    final isToday = _sameDay(date, _today);
    final statusColor = day == null ? palette.muted : _dayColor(day.status);
    final statusText = day?.status.label ?? (date.isAfter(_today) ? tr('لم يبدأ', 'Upcoming') : tr('لم يُسجل', 'Not recorded'));

    return Container(
      key: ValueKey('activity-day-${date.day}'),
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: isToday ? AppTheme.blue : palette.line, width: isToday ? 1.5 : 1),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(14, 12, 14, 8),
          child: Row(children: [
            Expanded(
              child: Text.rich(TextSpan(children: [
                TextSpan(text: _dayNames[date.weekday % 7], style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                TextSpan(text: '  ${_dayMonth(date)}', style: TextStyle(fontSize: 12, color: palette.muted)),
                if (isToday) TextSpan(text: '  · ${tr('اليوم', 'Today')}', style: const TextStyle(fontSize: 12, color: AppTheme.blue)),
              ])),
            ),
            if (day?.checkIn != null)
              Padding(
                padding: const EdgeInsetsDirectional.only(end: 8),
                child: Text('${tr('دخول', 'In')} ${_time(day!.checkIn!)}', style: TextStyle(fontSize: 11.5, color: palette.muted)),
              ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
              decoration: BoxDecoration(color: statusColor.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
              child: Text(statusText, style: TextStyle(color: statusColor, fontSize: 11.5, fontWeight: FontWeight.w600)),
            ),
          ]),
        ),
        if (entries.isEmpty)
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 0, 14, 12),
            child: Text(tr('لا توجد حصص مسجلة.', 'No lessons recorded.'), style: TextStyle(fontSize: 12.5, color: palette.muted)),
          )
        else
          for (final e in entries) _lessonRow(e),
        const SizedBox(height: 4),
      ]),
    );
  }

  Widget _lessonRow(_LessonEntry e) {
    final palette = context.palette;
    final lesson = e.lesson;
    final review = e.review;
    final time = e.start < 24 * 60 ? _time(e.start) : '';
    return InkWell(
      onTap: review == null && lesson?.absenceReason == null ? null : () => _showLesson(e),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
        child: Row(children: [
          Icon(lesson == null ? Icons.radio_button_unchecked_rounded : _lessonIcon(lesson.status),
              size: 18, color: lesson == null ? palette.faint : _lessonColor(lesson.status)),
          const SizedBox(width: 10),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(e.subject, maxLines: 1, overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13.5, color: palette.ink)),
              Text(
                [if (time.isNotEmpty) time, if (lesson != null) lesson.status.label, if (e.slot?.teacher.isNotEmpty ?? false) e.slot!.teacher]
                    .join(' · '),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(fontSize: 11.5, color: palette.muted),
              ),
            ]),
          ),
          if (review != null) _scoreBadge(review.score),
        ]),
      ),
    );
  }

  Widget _scoreBadge(double score) {
    final color = _scoreColor(score);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Icon(Icons.star_rounded, size: 15, color: color),
        const SizedBox(width: 3),
        Text(score.toStringAsFixed(1), style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 12)),
      ]),
    );
  }

  void _showLesson(_LessonEntry e) {
    final review = e.review;
    final lesson = e.lesson;
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (context) {
        final palette = context.palette;
        Widget rating(String label, int value) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 5),
              child: Row(children: [
                Expanded(child: Text(label, style: TextStyle(color: palette.ink))),
                for (var i = 1; i <= 5; i++) Icon(i <= value ? Icons.star_rounded : Icons.star_border_rounded, size: 20, color: _amber),
                const SizedBox(width: 8),
                SizedBox(width: 70, child: Text(activityRatingLabel(value), style: TextStyle(fontSize: 12, color: palette.muted))),
              ]),
            );
        final teacher = review == null ? null : _teacherName(review.teacherId);
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(e.subject, style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: palette.ink)),
              if (teacher != null || (e.slot?.teacher.isNotEmpty ?? false))
                Text(teacher ?? e.slot!.teacher, style: TextStyle(color: palette.muted)),
              if (lesson != null) ...[
                const SizedBox(height: 10),
                Row(children: [
                  Icon(_lessonIcon(lesson.status), color: _lessonColor(lesson.status), size: 18),
                  const SizedBox(width: 6),
                  Text(lesson.status.label, style: TextStyle(color: _lessonColor(lesson.status), fontWeight: FontWeight.w600)),
                  if (lesson.absenceReason != null) ...[
                    const SizedBox(width: 8),
                    Expanded(child: Text(lesson.absenceReason!, style: TextStyle(color: palette.muted))),
                  ],
                ]),
              ],
              if (review != null) ...[
                const Divider(height: 24),
                rating(tr('التركيز', 'Focus'), review.focus),
                rating(tr('التفاعل', 'Interaction'), review.interaction),
                rating(tr('السلوك', 'Behaviour'), review.behavior),
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 5),
                  child: Row(children: [
                    Expanded(child: Text(tr('الحالة الصحية', 'Health'), style: TextStyle(color: palette.ink))),
                    Text(review.health.label,
                        style: TextStyle(fontWeight: FontWeight.w600, color: review.health == ActivityHealth.good ? _green : _orange)),
                  ]),
                ),
                if (review.comment != null) ...[
                  const SizedBox(height: 8),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(12)),
                    child: Text(review.comment!, style: TextStyle(color: palette.ink, height: 1.4)),
                  ),
                ],
              ],
            ]),
          ),
        );
      },
    );
  }

  // ---------------- Exams ----------------

  Widget _examsCard() {
    final palette = context.palette;
    final exams = _exams;
    Widget body;
    if (_examsError != null) {
      body = _note(Icons.error_outline_rounded, _examsError!, retry: _loadExams);
    } else if (exams == null) {
      body = const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()));
    } else if (exams.isEmpty) {
      body = _note(Icons.fact_check_outlined, tr('لا توجد اختبارات في هذا العام.', 'No exams this year.'));
    } else {
      final upcoming = exams.where((e) => e.isUpcoming(_now)).toList()..sort((a, b) => a.date.compareTo(b.date));
      final graded = exams.where((e) => e.hasResult).toList()..sort((a, b) => b.date.compareTo(a.date));
      final passed = graded.where((e) => e.isPassed == true).length;
      final percents = graded.map((e) => e.percent!).toList();
      final average = percents.isEmpty ? null : percents.reduce((a, b) => a + b) / percents.length;
      final shownResults = _allResults ? graded : graded.take(5).toList();
      body = Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(children: [
          _stat(Icons.fact_check_rounded, _purple, '${exams.length}', tr('اختبار', 'Exams')),
          const SizedBox(width: 8),
          _stat(Icons.event_rounded, AppTheme.blue, '${upcoming.length}', tr('قادمة', 'Upcoming')),
          const SizedBox(width: 8),
          _stat(Icons.verified_rounded, _green, graded.isEmpty ? '-' : '$passed/${graded.length}', tr('ناجح', 'Passed')),
          const SizedBox(width: 8),
          _stat(Icons.percent_rounded, _amber, average == null ? '-' : '${average.round()}%', tr('المتوسط', 'Average'),
              key: const ValueKey('activity-exam-average')),
        ]),
        if (upcoming.isNotEmpty) ...[
          const SizedBox(height: 12),
          _subTitle(tr('القادمة', 'Upcoming')),
          for (final e in upcoming.take(3)) _examRow(e),
        ],
        if (graded.isNotEmpty) ...[
          const SizedBox(height: 12),
          _subTitle(tr('النتائج', 'Results')),
          for (final e in shownResults) _examRow(e),
          if (graded.length > 5)
            TextButton(
              key: const ValueKey('activity-more-results'),
              onPressed: () => setState(() => _allResults = !_allResults),
              child: Text(_allResults ? tr('عرض أقل', 'Show less') : tr('عرض كل النتائج (${graded.length})', 'Show all results (${graded.length})')),
            ),
        ],
      ]);
    }

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Row(children: [
        const Icon(Icons.fact_check_rounded, color: _purple),
        const SizedBox(width: 8),
        Text(tr('الاختبارات والنتائج', 'Exams & results'), style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: palette.ink)),
      ]),
      const SizedBox(height: 10),
      body,
    ]);
  }

  Widget _subTitle(String text) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Text(text, style: TextStyle(fontWeight: FontWeight.w700, color: context.palette.muted, fontSize: 12.5)),
      );

  Widget _examRow(ActivityExam e) {
    final palette = context.palette;
    final percent = e.percent;
    final color = e.isPassed == false ? _red : _green;
    String mark(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toStringAsFixed(1);
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(14), border: Border.all(color: palette.line)),
      child: Row(children: [
        Container(
          width: 46,
          padding: const EdgeInsets.symmetric(vertical: 6),
          decoration: BoxDecoration(color: _purple.withValues(alpha: .1), borderRadius: BorderRadius.circular(10)),
          child: Column(children: [
            Text('${e.date.day}', style: const TextStyle(fontWeight: FontWeight.bold, color: _purple, fontSize: 16)),
            Text(_monthShort[e.date.month - 1], style: const TextStyle(color: _purple, fontSize: 10)),
          ]),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(e.subject, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
            Text([e.kind.label, if (e.monthName != null) e.monthName!, _time(e.start)].join(' · '),
                style: TextStyle(fontSize: 11.5, color: palette.muted)),
            if (percent != null) ...[
              const SizedBox(height: 6),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: (percent / 100).clamp(0, 1).toDouble(),
                  minHeight: 5,
                  backgroundColor: color.withValues(alpha: .15),
                  color: color,
                ),
              ),
            ],
          ]),
        ),
        const SizedBox(width: 10),
        if (e.hasResult)
          Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
            Text('${mark(e.score!)}/${mark(e.fullMark)}', style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
            Text(e.isPassed == false ? tr('راسب', 'Failed') : tr('ناجح', 'Passed'),
                style: TextStyle(fontSize: 11.5, color: color, fontWeight: FontWeight.w600)),
          ])
        else
          Text(e.isUpcoming(_now) ? tr('قادم', 'Upcoming') : tr('بلا نتيجة', 'No result'), style: TextStyle(fontSize: 11.5, color: palette.muted)),
      ]),
    );
  }

  Widget _note(IconData icon, String text, {Future<void> Function()? retry}) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(16), border: Border.all(color: palette.line)),
      child: Column(children: [
        Icon(icon, color: palette.muted, size: 30),
        const SizedBox(height: 8),
        Text(text, textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
        if (retry != null) TextButton(onPressed: retry, child: Text(tr('إعادة المحاولة', 'Try again'))),
      ]),
    );
  }
}
