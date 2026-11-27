import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/academic_year_picker.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../schedule/schedule_format.dart';
import 'exams_service.dart';
import '../../core/i18n/app_language.dart';
import '../../core/live/live_refresh.dart';
import '../../core/files/answer_model.dart';

/// Exams of the child selected on home, one tab per exam type:
/// a summary, the upcoming exams (with a countdown) and the results (score ring, pass / fail).
class ExamsScreen extends StatefulWidget {
  const ExamsScreen({super.key, required this.child, this.loadExams, this.onSessionExpired, this.now});

  final Child child;

  /// Defaults to the API (tests pass their own)
  final Future<List<ChildExam>> Function(String studentId)? loadExams;

  /// Called when the token was rejected (the page closes first)
  final VoidCallback? onSessionExpired;

  /// Current time (tests pass a fixed one)
  final DateTime? now;

  @override
  State<ExamsScreen> createState() => _ExamsScreenState();
}

class _ExamsScreenState extends State<ExamsScreen> with ReloadOnAcademicYear, LiveRefreshMixin {
  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _load();

  @override
  Future<void> reloadForAcademicYear() => _load();

  Color get _muted => context.palette.muted;
  static const _green = Color(0xFF13864B);
  static const _red = Color(0xFFD93025);
  static const _orange = Color(0xFFF08C00);

  List<ChildExam>? _exams;
  String? _error;
  bool _loading = true;

  /// Month chosen in the Monthly tab (null = all months)
  int? _monthFilter;

  DateTime get _now => widget.now ?? DateTime.now();

  /// Colour and icon of each exam type
  static (Color, IconData) lookOf(ExamKind kind) => switch (kind) {
        ExamKind.quiz => (const Color(0xFF0FA5A5), Icons.bolt_rounded),
        ExamKind.monthly => (const Color(0xFF1677E8), Icons.calendar_month_rounded),
        ExamKind.midTerm => (const Color(0xFF8957E5), Icons.event_note_rounded),
        ExamKind.finalExam => (const Color(0xFFE0457B), Icons.workspace_premium_rounded),
        ExamKind.secondFinal => (const Color(0xFFD9822B), Icons.replay_rounded),
      };

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _exams == null;
      _error = null;
    });
    try {
      final loader = widget.loadExams ?? ExamsService.instance.getChildExams;
      final result = await loader(widget.child.id);
      if (!mounted) return;
      setState(() {
        _exams = result;
        _loading = false;
      });
    } on ExamsException catch (e) {
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

  /// Tabs: every type, but Second final only when the child has one (only children who failed a Final do)
  static List<ExamKind> _kindsOf(List<ChildExam> exams) => [
        for (final kind in ExamKind.values)
          if (kind != ExamKind.secondFinal || exams.any((e) => e.kind == kind)) kind,
      ];

  /// Tab of the next upcoming exam (else the type with the latest result, else the first)
  int _initialTab(List<ChildExam> exams, List<ExamKind> kinds) {
    final upcoming = exams.where((e) => e.isUpcoming(_now)).toList()..sort(_byDateTime);
    if (upcoming.isNotEmpty) return kinds.indexOf(upcoming.first.kind);
    final graded = exams.where((e) => e.hasResult).toList()..sort((a, b) => _byDateTime(b, a));
    if (graded.isNotEmpty) return kinds.indexOf(graded.first.kind);
    return 0;
  }

  static int _byDateTime(ChildExam a, ChildExam b) {
    final d = a.date.compareTo(b.date);
    return d != 0 ? d : a.start.compareTo(b.start);
  }

  /// "2026-2027 · التيرم الاول" (empty while the years are not loaded)
  String get _periodText {
    final year = AcademicYearController.instance.value;
    final semester = AcademicYearController.instance.semester;
    return [year?.name, semester?.name].whereType<String>().where((s) => s.isNotEmpty).join(' · ');
  }

  @override
  Widget build(BuildContext context) {
    final title = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(tr('الاختبارات والدرجات', 'Exams & Grades'), style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        Text(
          [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
          style: const TextStyle(fontSize: 12, color: Colors.white70),
        ),
        // The academic year / semester the exams are shown for
        if (_periodText.isNotEmpty)
          Text(_periodText,
              key: const ValueKey('exams-period'),
              style: const TextStyle(fontSize: 11.5, color: Colors.white70, fontWeight: FontWeight.w600)),
      ],
    );
    // Change the academic year / semester (the page reloads)
    final actions = [
      IconButton(
        key: const ValueKey('exams-change-period'),
        tooltip: tr('السنة والفصل الدراسي', 'Academic year and semester'),
        icon: const Icon(Icons.date_range_rounded),
        onPressed: () => showAcademicYearSheet(context),
      ),
    ];

    if (_loading || _error != null) {
      return Scaffold(
        appBar: AppBar(title: title, actions: actions),
        body: SafeArea(
          child: _loading
              ? const Center(child: CircularProgressIndicator())
              : _centerMessage(Icons.error_outline_rounded, _error!, retry: true),
        ),
      );
    }

    final exams = _exams!;
    final kinds = _kindsOf(exams);
    return DefaultTabController(
      // A new key when the tabs change (a second final appears), so the controller is rebuilt
      key: ValueKey(kinds.length),
      length: kinds.length,
      initialIndex: _initialTab(exams, kinds),
      child: Scaffold(
        appBar: AppBar(
          title: title,
          actions: actions,
          toolbarHeight: _periodText.isNotEmpty ? 70 : null,
          bottom: TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            labelColor: Colors.white,
            unselectedLabelColor: Colors.white70,
            indicatorColor: Colors.white,
            indicatorWeight: 3,
            labelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            tabs: [
              for (final kind in kinds)
                Tab(
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(kind.label),
                      const SizedBox(width: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                        decoration: BoxDecoration(color: Colors.white24, borderRadius: BorderRadius.circular(10)),
                        child: Text('${exams.where((e) => e.kind == kind).length}', style: const TextStyle(fontSize: 11)),
                      ),
                    ],
                  ),
                ),
            ],
          ),
        ),
        body: SafeArea(
          child: TabBarView(
            children: [
              for (final kind in kinds)
                RefreshIndicator(
                  onRefresh: _load,
                  child: _kindView(kind, exams.where((e) => e.kind == kind).toList()),
                ),
            ],
          ),
        ),
      ),
    );
  }

  // ----- One exam type -----

  Widget _kindView(ExamKind kind, List<ChildExam> allExams) {
    final (color, _) = lookOf(kind);

    // Monthly: months that have exams (in exam-date order) and the chosen one
    final months = <int>[];
    if (kind == ExamKind.monthly) {
      for (final e in [...allExams]..sort(_byDateTime)) {
        if (e.month != null && !months.contains(e.month)) months.add(e.month!);
      }
    }
    final month = months.contains(_monthFilter) ? _monthFilter : null;
    final exams = month == null ? allExams : allExams.where((e) => e.month == month).toList();
    final upcoming = exams.where((e) => e.isUpcoming(_now)).toList()..sort(_byDateTime);
    final past = exams.where((e) => !e.isUpcoming(_now)).toList()..sort((a, b) => _byDateTime(b, a));

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        _summary(kind, exams),
        if (months.length > 1) ...[
          const SizedBox(height: 14),
          _monthChips(months, month, color),
        ],
        const SizedBox(height: 20),
        if (exams.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: _centerMessage(Icons.event_available_rounded, tr('لا توجد ${kind.plural} حالياً.', 'No ${kind.plural} right now.')),
          ),
        if (upcoming.isNotEmpty) ...[
          _sectionTitle(tr('الاختبارات القادمة', 'Upcoming exams'), upcoming.length, Icons.upcoming_rounded, color),
          for (final exam in upcoming) _upcomingCard(exam, color, isNext: exam == upcoming.first),
          const SizedBox(height: 14),
        ],
        if (past.isNotEmpty) ...[
          _sectionTitle(tr('النتائج', 'Results'), past.length, Icons.grading_rounded, color),
          for (final exam in past) _resultCard(exam, color),
        ],
      ],
    );
  }

  /// Type header: counts, upcoming, average score and passed / failed
  Widget _summary(ExamKind kind, List<ChildExam> exams) {
    final (color, icon) = lookOf(kind);
    final graded = exams.where((e) => e.percentage != null).toList();
    final average = graded.isEmpty ? null : graded.fold<double>(0, (s, e) => s + e.percentage!) / graded.length;
    final passed = graded.where((e) => e.isPassed == true).length;
    final failed = graded.where((e) => e.isPassed == false).length;
    final upcoming = exams.where((e) => e.isUpcoming(_now)).length;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [color, Color.lerp(color, Colors.black, .25)!],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(22),
        boxShadow: [BoxShadow(color: color.withValues(alpha: .25), blurRadius: 14, offset: const Offset(0, 6))],
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(color: Colors.white24, borderRadius: BorderRadius.circular(12)),
                      child: Icon(icon, color: Colors.white, size: 20),
                    ),
                    const SizedBox(width: 10),
                    Flexible(
                      child: Text(kind.plural,
                          style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
                    ),
                  ],
                ),
                const SizedBox(height: 14),
                Wrap(
                  spacing: 14,
                  runSpacing: 8,
                  children: [
                    _stat('${exams.length}', tr('الإجمالي', 'Total')),
                    _stat('$upcoming', tr('قادمة', 'Upcoming')),
                    _stat('$passed', tr('ناجح', 'Passed')),
                    if (failed > 0) _stat('$failed', tr('راسب', 'Failed')),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          // Average score ring
          SizedBox(
            width: 84,
            height: 84,
            child: Stack(
              alignment: Alignment.center,
              children: [
                SizedBox(
                  width: 84,
                  height: 84,
                  child: CircularProgressIndicator(
                    value: (average ?? 0) / 100,
                    strokeWidth: 7,
                    backgroundColor: Colors.white24,
                    color: Colors.white,
                  ),
                ),
                Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(average == null ? '—' : '${average.round()}%',
                        style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
                    Text(tr('المتوسط', 'Average'), style: TextStyle(color: Colors.white70, fontSize: 10.5)),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Monthly tab: "All" plus one chip per month that has exams
  Widget _monthChips(List<int> months, int? selected, Color color) {
    Widget chip(String label, int? value) {
      final isSelected = selected == value;
      return Padding(
        padding: const EdgeInsetsDirectional.only(end: 8),
        child: ChoiceChip(
          label: Text(label),
          selected: isSelected,
          showCheckmark: false,
          selectedColor: color,
          backgroundColor: context.palette.card,
          side: BorderSide(color: isSelected ? color : context.palette.chip),
          labelStyle: TextStyle(color: isSelected ? Colors.white : context.palette.ink, fontWeight: FontWeight.bold),
          onSelected: (_) => setState(() => _monthFilter = value),
        ),
      );
    }

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          chip(tr('الكل', 'All'), null),
          for (final m in months) chip(monthNames[m - 1], m),
        ],
      ),
    );
  }

  /// "شهر نوفمبر" tag on a Monthly exam card
  Widget _monthTag(ChildExam exam, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.calendar_month_rounded, size: 13, color: color),
          const SizedBox(width: 3),
          Text(tr('شهر ${exam.monthName}', '${exam.monthName}'), style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: color)),
        ],
      ),
    );
  }

  Widget _stat(String value, String label) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(value, style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
        Text(label, style: const TextStyle(color: Colors.white70, fontSize: 11)),
      ],
    );
  }

  Widget _sectionTitle(String title, int count, IconData icon, Color color) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        children: [
          Icon(icon, size: 20, color: color),
          const SizedBox(width: 6),
          Text(title, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.ink)),
          const SizedBox(width: 6),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
            decoration: BoxDecoration(color: context.palette.chip, borderRadius: BorderRadius.circular(10)),
            child: Text('$count', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: _muted)),
          ),
        ],
      ),
    );
  }

  /// Upcoming exam: date block, subject, time, duration, marks and a countdown
  Widget _upcomingCard(ChildExam exam, Color color, {required bool isNext}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isNext ? color.withValues(alpha: .07) : context.palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: isNext ? color.withValues(alpha: .5) : context.palette.line, width: isNext ? 1.5 : 1),
      ),
      child: Row(
        children: [
          _dateBlock(exam.date, color),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(exam.subject, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.ink)),
                if (exam.monthName != null) ...[const SizedBox(height: 4), _monthTag(exam, color)],
                const SizedBox(height: 4),
                Wrap(
                  spacing: 10,
                  runSpacing: 2,
                  children: [
                    _meta(Icons.schedule_rounded, '${formatTime(exam.start)} - ${formatTime(exam.end)}'),
                    if (exam.duration > 0) _meta(Icons.timer_outlined, formatDuration(exam.duration)),
                    _meta(Icons.star_outline_rounded, tr('الدرجة ${_mark(exam.fullMark)}', 'Mark ${_mark(exam.fullMark)}')),
                  ],
                ),
                const SizedBox(height: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                  decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(20)),
                  child: Text(_countdown(exam),
                      style: const TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.bold)),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Past exam: score ring with "score / full mark", pass / fail (or waiting for the result)
  Widget _resultCard(ChildExam exam, Color color) {
    final hasResult = exam.hasResult;
    final passed = exam.isPassed == true;
    final resultColor = !hasResult ? _orange : (passed ? _green : _red);
    final percentage = (exam.percentage ?? 0) / 100;

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: context.palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: context.palette.line),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 64,
            height: 64,
            child: Stack(
              alignment: Alignment.center,
              children: [
                SizedBox(
                  width: 64,
                  height: 64,
                  child: CircularProgressIndicator(
                    value: hasResult ? percentage : 0,
                    strokeWidth: 6,
                    backgroundColor: context.palette.chip,
                    color: resultColor,
                  ),
                ),
                hasResult
                    ? Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(_mark(exam.score!),
                              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: resultColor)),
                          Text(tr('من ${_mark(exam.fullMark)}', 'of ${_mark(exam.fullMark)}'), style: TextStyle(fontSize: 9.5, color: _muted)),
                        ],
                      )
                    : const Icon(Icons.hourglass_top_rounded, color: _orange, size: 22),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(exam.subject, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.ink)),
                if (exam.monthName != null) ...[const SizedBox(height: 4), _monthTag(exam, color)],
                const SizedBox(height: 4),
                _meta(Icons.event_rounded, _dateText(exam.date)),
                const SizedBox(height: 6),
                Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                      decoration: BoxDecoration(color: resultColor.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                      child: Text(
                        !hasResult ? tr('بانتظار النتيجة', 'Awaiting result') : (passed ? tr('ناجح', 'Passed') : tr('راسب', 'Failed')),
                        style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold, color: resultColor),
                      ),
                    ),
                    if (hasResult && exam.percentage != null)
                      Text('${exam.percentage!.round()}%',
                          style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: resultColor)),
                    Text(tr('النجاح من ${_mark(exam.passMark)}', 'Pass mark ${_mark(exam.passMark)}'), style: TextStyle(fontSize: 11.5, color: _muted)),
                    // The answer model, once the result is out
                    if (hasResult && exam.answerModelUrl != null)
                      InkWell(
                        key: ValueKey('answer-model-${exam.id}'),
                        borderRadius: BorderRadius.circular(20),
                        onTap: () => openAnswerModel(context, exam.answerModelUrl!),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                          decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(Icons.task_rounded, size: 14, color: color),
                              const SizedBox(width: 4),
                              Text(tr('نموذج الإجابة', 'Answer model'),
                                  style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold, color: color)),
                            ],
                          ),
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Day and month in a coloured block
  Widget _dateBlock(DateTime date, Color color) {
    return Container(
      width: 58,
      padding: const EdgeInsets.symmetric(vertical: 8),
      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(14)),
      child: Column(
        children: [
          Text(dayNames[date.weekday % 7], style: TextStyle(fontSize: 10.5, color: color, fontWeight: FontWeight.bold)),
          Text('${date.day}', style: TextStyle(fontSize: 22, height: 1.15, fontWeight: FontWeight.bold, color: color)),
          Text(monthNames[date.month - 1], style: TextStyle(fontSize: 10.5, color: color)),
        ],
      ),
    );
  }

  Widget _meta(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: _muted),
        const SizedBox(width: 3),
        Flexible(child: Text(text, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 12, color: _muted))),
      ],
    );
  }

  /// "اليوم - تبدأ بعد ساعة" / "غداً" / "بعد 5 أيام"
  String _countdown(ChildExam exam) {
    final now = _now;
    final today = DateTime(now.year, now.month, now.day);
    final days = exam.date.difference(today).inDays;
    if (days == 0) {
      final minutes = exam.start - (now.hour * 60 + now.minute);
      return minutes > 0 ? tr('اليوم - تبدأ ${formatIn(minutes)}', 'Today - starts ${formatIn(minutes)}') : tr('اليوم - جارٍ الآن', 'Today - in progress');
    }
    if (days == 1) return tr('غداً', 'Tomorrow');
    if (days == 2) return tr('بعد يومين', 'In 2 days');
    if (days <= 10) return tr('بعد $days أيام', 'In $days days');
    return tr('بعد $days يوماً', 'In $days days');
  }

  /// "الخميس 20 نوفمبر"
  static String _dateText(DateTime d) => '${dayNames[d.weekday % 7]} ${d.day} ${monthNames[d.month - 1]}';

  /// 18.0 -> "18", 17.5 -> "17.5"
  static String _mark(double value) =>
      value == value.roundToDouble() ? value.toInt().toString() : value.toStringAsFixed(1);

  Widget _centerMessage(IconData icon, String text, {bool retry = false}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry ? _red : AppTheme.blue.withValues(alpha: .6)),
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
