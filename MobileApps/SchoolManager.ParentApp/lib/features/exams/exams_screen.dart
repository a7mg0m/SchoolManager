import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../schedule/schedule_format.dart';
import 'exams_service.dart';

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

class _ExamsScreenState extends State<ExamsScreen> {
  static const _muted = Color(0xFF637394);
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

  /// Tab of the next upcoming exam (else the type with the latest result, else the first)
  int _initialTab(List<ChildExam> exams) {
    final upcoming = exams.where((e) => e.isUpcoming(_now)).toList()..sort(_byDateTime);
    if (upcoming.isNotEmpty) return ExamKind.values.indexOf(upcoming.first.kind);
    final graded = exams.where((e) => e.hasResult).toList()..sort((a, b) => _byDateTime(b, a));
    if (graded.isNotEmpty) return ExamKind.values.indexOf(graded.first.kind);
    return 0;
  }

  static int _byDateTime(ChildExam a, ChildExam b) {
    final d = a.date.compareTo(b.date);
    return d != 0 ? d : a.start.compareTo(b.start);
  }

  @override
  Widget build(BuildContext context) {
    final title = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('الاختبارات والدرجات', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        Text(
          [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
          style: const TextStyle(fontSize: 12, color: Colors.white70),
        ),
      ],
    );

    if (_loading || _error != null) {
      return Scaffold(
        appBar: AppBar(title: title),
        body: SafeArea(
          child: _loading
              ? const Center(child: CircularProgressIndicator())
              : _centerMessage(Icons.error_outline_rounded, _error!, retry: true),
        ),
      );
    }

    final exams = _exams!;
    return DefaultTabController(
      length: ExamKind.values.length,
      initialIndex: _initialTab(exams),
      child: Scaffold(
        appBar: AppBar(
          title: title,
          bottom: TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            labelColor: Colors.white,
            unselectedLabelColor: Colors.white70,
            indicatorColor: Colors.white,
            indicatorWeight: 3,
            labelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            tabs: [
              for (final kind in ExamKind.values)
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
              for (final kind in ExamKind.values)
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
            child: _centerMessage(Icons.event_available_rounded, 'لا توجد ${kind.plural} حالياً.'),
          ),
        if (upcoming.isNotEmpty) ...[
          _sectionTitle('الاختبارات القادمة', upcoming.length, Icons.upcoming_rounded, color),
          for (final exam in upcoming) _upcomingCard(exam, color, isNext: exam == upcoming.first),
          const SizedBox(height: 14),
        ],
        if (past.isNotEmpty) ...[
          _sectionTitle('النتائج', past.length, Icons.grading_rounded, color),
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
                    _stat('${exams.length}', 'الإجمالي'),
                    _stat('$upcoming', 'قادمة'),
                    _stat('$passed', 'ناجح'),
                    if (failed > 0) _stat('$failed', 'راسب'),
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
                    const Text('المتوسط', style: TextStyle(color: Colors.white70, fontSize: 10.5)),
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
          backgroundColor: Colors.white,
          side: BorderSide(color: isSelected ? color : const Color(0xFFE3E9F3)),
          labelStyle: TextStyle(color: isSelected ? Colors.white : AppTheme.ink, fontWeight: FontWeight.bold),
          onSelected: (_) => setState(() => _monthFilter = value),
        ),
      );
    }

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          chip('الكل', null),
          for (final m in months) chip(arabicMonths[m - 1], m),
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
          Text('شهر ${exam.monthName}', style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: color)),
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
          Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.ink)),
          const SizedBox(width: 6),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
            decoration: BoxDecoration(color: const Color(0xFFE3E9F3), borderRadius: BorderRadius.circular(10)),
            child: Text('$count', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: _muted)),
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
        color: isNext ? color.withValues(alpha: .07) : Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: isNext ? color.withValues(alpha: .5) : const Color(0xFFE6ECF5), width: isNext ? 1.5 : 1),
      ),
      child: Row(
        children: [
          _dateBlock(exam.date, color),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(exam.subject, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.ink)),
                if (exam.monthName != null) ...[const SizedBox(height: 4), _monthTag(exam, color)],
                const SizedBox(height: 4),
                Wrap(
                  spacing: 10,
                  runSpacing: 2,
                  children: [
                    _meta(Icons.schedule_rounded, '${formatTime(exam.start)} - ${formatTime(exam.end)}'),
                    if (exam.duration > 0) _meta(Icons.timer_outlined, formatDuration(exam.duration)),
                    _meta(Icons.star_outline_rounded, 'الدرجة ${_mark(exam.fullMark)}'),
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
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFE6ECF5)),
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
                    backgroundColor: const Color(0xFFE3E9F3),
                    color: resultColor,
                  ),
                ),
                hasResult
                    ? Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(_mark(exam.score!),
                              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: resultColor)),
                          Text('من ${_mark(exam.fullMark)}', style: const TextStyle(fontSize: 9.5, color: _muted)),
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
                Text(exam.subject, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.ink)),
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
                        !hasResult ? 'بانتظار النتيجة' : (passed ? 'ناجح' : 'راسب'),
                        style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold, color: resultColor),
                      ),
                    ),
                    if (hasResult && exam.percentage != null)
                      Text('${exam.percentage!.round()}%',
                          style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: resultColor)),
                    Text('النجاح من ${_mark(exam.passMark)}', style: const TextStyle(fontSize: 11.5, color: _muted)),
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
          Text(arabicDays[date.weekday % 7], style: TextStyle(fontSize: 10.5, color: color, fontWeight: FontWeight.bold)),
          Text('${date.day}', style: TextStyle(fontSize: 22, height: 1.15, fontWeight: FontWeight.bold, color: color)),
          Text(arabicMonths[date.month - 1], style: TextStyle(fontSize: 10.5, color: color)),
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
        Flexible(child: Text(text, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 12, color: _muted))),
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
      return minutes > 0 ? 'اليوم - تبدأ ${formatIn(minutes)}' : 'اليوم - جارٍ الآن';
    }
    if (days == 1) return 'غداً';
    if (days == 2) return 'بعد يومين';
    if (days <= 10) return 'بعد $days أيام';
    return 'بعد $days يوماً';
  }

  /// "الخميس 20 نوفمبر"
  static String _dateText(DateTime d) => '${arabicDays[d.weekday % 7]} ${d.day} ${arabicMonths[d.month - 1]}';

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
            Text(text, textAlign: TextAlign.center, style: const TextStyle(color: AppTheme.ink)),
            if (retry) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: _load, child: const Text('إعادة المحاولة')),
            ],
          ],
        ),
      ),
    );
  }
}
