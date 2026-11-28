import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../homework/homework_screens.dart' show homeworkDate;
import '../homework/homework_service.dart';
import 'exam_service.dart';
import '../../core/live/live_refresh.dart';
import '../../core/files/answer_model.dart';
import '../../core/widgets/paged.dart';

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _purple = Color(0xFF6A3FD8);
const _indigo = Color(0xFF3F51B5);

/// Colors of each exam type
List<Color> examColors(TeacherExamType type) => switch (type) {
      TeacherExamType.quiz => const [Color(0xFF9B6BFF), _purple],
      TeacherExamType.monthly => const [Color(0xFF4C8DF6), _indigo],
    };

IconData examIcon(TeacherExamType type) => switch (type) {
      TeacherExamType.quiz => Icons.bolt_rounded,
      TeacherExamType.monthly => Icons.calendar_month_rounded,
    };

/// The API calls the exam screens use (tests pass their own)
class ExamApi {
  const ExamApi({
    this.loadClasses,
    this.loadExams,
    this.create,
    this.update,
    this.delete,
    this.loadScores,
    this.saveScores,
  });

  final Future<TeacherClasses> Function()? loadClasses;
  final Future<List<TeacherExam>> Function()? loadExams;
  final Future<String> Function(ExamInput input)? create;
  final Future<void> Function(String examId, ExamInput input)? update;
  final Future<void> Function(String examId)? delete;
  final Future<ExamScores> Function(String examId)? loadScores;
  final Future<void> Function(String examId, Map<String, double?> scores)? saveScores;

  Future<TeacherClasses> classes() async {
    try {
      return await (loadClasses ?? HomeworkService.instance.getMyClasses)();
    } on HomeworkException catch (e) {
      throw ExamException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<TeacherExam>> exams() => (loadExams ?? ExamService.instance.getMyExams)();
  Future<String> createExam(ExamInput input) => (create ?? ExamService.instance.createExam)(input);
  Future<void> updateExam(String examId, ExamInput input) => (update ?? ExamService.instance.updateExam)(examId, input);
  Future<void> deleteExam(String examId) => (delete ?? ExamService.instance.deleteExam)(examId);
  Future<ExamScores> scores(String examId) => (loadScores ?? ExamService.instance.getScores)(examId);
  Future<void> saveExamScores(String examId, Map<String, double?> scores) =>
      (saveScores ?? ExamService.instance.saveScores)(examId, scores);
}

enum _Filter { all, mine, quiz, monthly }

/// The quizzes and monthly exams of the teacher's subjects and classes for the chosen academic year / semester:
/// their own (add, edit, delete) and the ones by the school office / other teachers (view); scores for all
class ExamListScreen extends StatefulWidget {
  const ExamListScreen({super.key, this.api = const ExamApi(), this.onSessionExpired, this.now});

  final ExamApi api;
  final Future<void> Function()? onSessionExpired;

  /// "Now" (tests pass a fixed time)
  final DateTime? now;

  @override
  State<ExamListScreen> createState() => _ExamListScreenState();
}

class _ExamListScreenState extends State<ExamListScreen> with ReloadOnAcademicYear, LiveRefreshMixin {
  /// Long lists: the first items, then "Show more"
  final _pages = PageWindows();

  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _load();

  @override
  Future<void> reloadForAcademicYear() => _load();

  List<TeacherExam> _exams = const [];
  TeacherClasses _classes = const TeacherClasses();
  bool _loading = true;
  String? _error;
  _Filter _filter = _Filter.all;
  String? _busyId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _exams.isEmpty;
      _error = null;
    });
    try {
      final results = await Future.wait([widget.api.exams(), widget.api.classes()]);
      if (!mounted) return;
      setState(() {
        _exams = results[0] as List<TeacherExam>;
        _classes = results[1] as TeacherClasses;
        _loading = false;
      });
    } on ExamException catch (e) {
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

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  /// New exam, or edit [exam]
  Future<void> _openForm([TeacherExam? exam]) async {
    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => ExamFormScreen(api: widget.api, classes: _classes, exam: exam, now: widget.now),
    ));
    if (saved == true) await _load();
  }

  Future<void> _openScores(TeacherExam exam) async {
    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => ExamScoresScreen(api: widget.api, exam: exam, onSessionExpired: widget.onSessionExpired),
    ));
    if (saved == true) await _load();
  }

  Future<void> _delete(TeacherExam exam) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(tr('حذف الاختبار؟', 'Delete the exam?')),
        content: Text(exam.resultCount > 0
            ? tr('سيتم حذف ${exam.title} ودرجات ${exam.resultCount} طالب.', '${exam.title} and the scores of ${exam.resultCount} students will be deleted.')
            : tr('سيتم حذف ${exam.title}.', '${exam.title} will be deleted.')),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: Text(tr('إلغاء', 'Cancel'))),
          FilledButton(
            key: const ValueKey('confirm-delete-exam'),
            style: FilledButton.styleFrom(backgroundColor: _red),
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(tr('حذف', 'Delete')),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;

    setState(() => _busyId = exam.id);
    try {
      await widget.api.deleteExam(exam.id);
      if (!mounted) return;
      _show(tr('تم حذف الاختبار.', 'Exam deleted.'));
      await _load();
    } on ExamException catch (e) {
      if (mounted) _show(e.message);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final list = _exams.where((e) => switch (_filter) {
          _Filter.all => true,
          _Filter.mine => e.isMine,
          _Filter.quiz => e.type == TeacherExamType.quiz,
          _Filter.monthly => e.type == TeacherExamType.monthly,
        }).toList();
    final othersCount = _exams.where((e) => !e.isMine).length;

    return Scaffold(
      appBar: AppBar(title: Text(tr('الاختبارات والدرجات', 'Exams & scores'), style: const TextStyle(fontWeight: FontWeight.bold))),
      floatingActionButton: _loading || _error != null
          ? null
          : FloatingActionButton.extended(
              key: const ValueKey('new-exam'),
              onPressed: _openForm,
              icon: const Icon(Icons.add_rounded),
              label: Text(tr('اختبار جديد', 'New exam')),
            ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(Icons.error_outline_rounded, _error!, retry: true)
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 14, 16, 96),
                    children: [
                      Wrap(
                        spacing: 8,
                        runSpacing: 4,
                        children: [
                          _filterChip(_Filter.all, tr('الكل', 'All'), _exams.length),
                          // Only when others' exams are listed too
                          if (othersCount > 0) _filterChip(_Filter.mine, tr('اختباراتي', 'Mine'), _exams.where((e) => e.isMine).length),
                          _filterChip(_Filter.quiz, tr('اختبارات قصيرة', 'Quizzes'),
                              _exams.where((e) => e.type == TeacherExamType.quiz).length),
                          _filterChip(_Filter.monthly, tr('شهرية', 'Monthly'),
                              _exams.where((e) => e.type == TeacherExamType.monthly).length),
                        ],
                      ),
                      const SizedBox(height: 12),
                      if (list.isEmpty)
                        Padding(
                          padding: const EdgeInsets.only(top: 60),
                          child: _message(Icons.fact_check_rounded,
                              _exams.isEmpty
                                  ? tr('لا توجد اختبارات بعد. اضغط "اختبار جديد" لإضافة أول اختبار.',
                                      'No exams yet. Tap "New exam" to add the first one.')
                                  : tr('لا توجد اختبارات هنا.', 'No exams here.')),
                        )
                      else
                        for (final e in _pages.take('exams', list)) ...[
                          _ExamCard(
                            exam: e,
                            now: widget.now ?? DateTime.now(),
                            busy: _busyId == e.id,
                            onScores: () => _openScores(e),
                            onEdit: () => _openForm(e),
                            onDelete: () => _delete(e),
                          ),
                          const SizedBox(height: 12),
                        ],
                        _pages.more(context, 'exams', list.length, setState),
                    ],
                  ),
                ),
    );
  }

  Widget _filterChip(_Filter value, String label, int count) => ChoiceChip(
        key: ValueKey('exam-filter-${value.name}'),
        label: Text('$label ($count)'),
        selected: _filter == value,
        onSelected: (_) => setState(() => _filter = value),
      );

  Widget _message(IconData icon, String text, {bool retry = false}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry ? _red : _purple.withValues(alpha: .6)),
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

/// One exam: type badge, subject / class, date and time, marks and how many scores are entered
class _ExamCard extends StatelessWidget {
  const _ExamCard({
    required this.exam,
    required this.now,
    required this.busy,
    required this.onScores,
    required this.onEdit,
    required this.onDelete,
  });

  final TeacherExam exam;
  final DateTime now;
  final bool busy;
  final VoidCallback onScores;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final colors = examColors(exam.type);
    final upcoming = exam.isUpcoming(now);

    return Container(
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: palette.line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            padding: const EdgeInsets.fromLTRB(14, 12, 6, 12),
            decoration: BoxDecoration(
              gradient: LinearGradient(colors: colors, begin: AlignmentDirectional.centerStart, end: AlignmentDirectional.centerEnd),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(7),
                  decoration: BoxDecoration(color: Colors.white.withValues(alpha: .2), shape: BoxShape.circle),
                  child: Icon(examIcon(exam.type), color: Colors.white, size: 20),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(exam.title,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(color: Colors.white, fontSize: 15.5, fontWeight: FontWeight.bold)),
                      Text(
                        [exam.subjectName, exam.gradeName, exam.classText].where((s) => s.isNotEmpty).join(' · '),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: Colors.white.withValues(alpha: .88), fontSize: 12),
                      ),
                    ],
                  ),
                ),
                if (busy)
                  const Padding(
                    padding: EdgeInsets.all(12),
                    child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white)),
                  )
                else if (!exam.isMine)
                  // Someone else's exam: who added it, no edit / delete
                  Container(
                    key: ValueKey('exam-added-by-${exam.id}'),
                    margin: const EdgeInsetsDirectional.only(end: 8),
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(color: Colors.white.withValues(alpha: .22), borderRadius: BorderRadius.circular(20)),
                    child: Text(exam.addedByText, style: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
                  )
                else ...[
                  IconButton(
                    key: ValueKey('edit-exam-${exam.id}'),
                    tooltip: tr('تعديل', 'Edit'),
                    color: Colors.white,
                    icon: const Icon(Icons.edit_rounded, size: 20),
                    onPressed: onEdit,
                  ),
                  IconButton(
                    key: ValueKey('delete-exam-${exam.id}'),
                    tooltip: tr('حذف', 'Delete'),
                    color: Colors.white,
                    icon: const Icon(Icons.delete_outline_rounded, size: 20),
                    onPressed: onDelete,
                  ),
                ],
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 4),
            child: Wrap(
              spacing: 14,
              runSpacing: 6,
              children: [
                _info(context, Icons.event_rounded, homeworkDate(exam.date)),
                _info(context, Icons.schedule_rounded, '${examClock(exam.startMinutes)} - ${examClock(exam.endMinutes)}'),
                _info(context, Icons.star_rounded, tr('من ${examMark(exam.weight)} · النجاح ${examMark(exam.successScore)}',
                    'Out of ${examMark(exam.weight)} · pass ${examMark(exam.successScore)}')),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 8, 10, 10),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
                  decoration: BoxDecoration(
                    color: (upcoming ? AppTheme.blue : palette.muted).withValues(alpha: .1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(upcoming ? tr('قادم', 'Upcoming') : tr('تم', 'Done'),
                      style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold, color: upcoming ? AppTheme.blue : palette.muted)),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    exam.resultCount == 0
                        ? tr('لم تُرصد درجات بعد', 'No scores yet')
                        : tr('رُصدت ${exam.resultCount} درجة', '${exam.resultCount} scores entered') +
                            (exam.averageScore != null ? ' · ${tr('المتوسط', 'avg')} ${examMark(exam.averageScore!)}' : ''),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontSize: 12, color: exam.resultCount == 0 ? palette.muted : _green, fontWeight: FontWeight.w600),
                  ),
                ),
                // The answer model of the exam (when the school uploaded one)
                if (exam.answerModelUrl != null) ...[
                  IconButton(
                    key: ValueKey('answer-model-${exam.id}'),
                    tooltip: tr('نموذج الإجابة', 'Answer model'),
                    onPressed: () => openAnswerModel(context, exam.answerModelUrl!),
                    icon: const Icon(Icons.task_rounded, color: AppTheme.blue),
                  ),
                  const SizedBox(width: 2),
                ],
                FilledButton.tonalIcon(
                  key: ValueKey('scores-${exam.id}'),
                  onPressed: busy ? null : onScores,
                  icon: const Icon(Icons.edit_note_rounded, size: 20),
                  label: Text(tr('الدرجات', 'Scores')),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _info(BuildContext context, IconData icon, String text) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: context.palette.muted),
          const SizedBox(width: 4),
          Text(text, style: TextStyle(fontSize: 12.5, color: context.palette.ink)),
        ],
      );
}

/// New / edit exam: subject, class, type (quiz or monthly + month), date, time and marks
class ExamFormScreen extends StatefulWidget {
  const ExamFormScreen({super.key, this.api = const ExamApi(), required this.classes, this.exam, this.now});

  final ExamApi api;
  final TeacherClasses classes;

  /// The exam to edit (null: new exam)
  final TeacherExam? exam;
  final DateTime? now;

  @override
  State<ExamFormScreen> createState() => _ExamFormScreenState();
}

class _ExamFormScreenState extends State<ExamFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _weight = TextEditingController();
  final _pass = TextEditingController();

  TeacherSubject? _subject;
  String? _sectionId;
  TeacherExamType _type = TeacherExamType.quiz;
  int? _month;
  late DateTime _date;
  int _start = 8 * 60;
  int _end = 8 * 60 + 45;
  bool _saving = false;
  String? _sectionError;

  bool get _editing => widget.exam != null;

  @override
  void initState() {
    super.initState();
    final now = widget.now ?? DateTime.now();
    final exam = widget.exam;
    if (exam != null) {
      _subject = widget.classes.subjects
          .where((s) => s.subjectId == exam.subjectId && s.gradeId == exam.gradeId)
          .firstOrNull;
      _sectionId = exam.sectionId;
      _type = exam.type;
      _month = exam.month;
      _date = exam.date;
      _start = exam.startMinutes;
      _end = exam.endMinutes;
      _weight.text = examMark(exam.weight);
      _pass.text = examMark(exam.successScore);
    } else {
      if (widget.classes.subjects.length == 1) _subject = widget.classes.subjects.first;
      if (_subject?.sections.length == 1) _sectionId = _subject!.sections.first.id;
      _date = DateTime(now.year, now.month, now.day);
      _month = now.month;
      _weight.text = '10';
      _pass.text = '5';
    }
  }

  @override
  void dispose() {
    _weight.dispose();
    _pass.dispose();
    super.dispose();
  }

  void _setType(TeacherExamType type) {
    setState(() {
      _type = type;
      // Usual marks of each type when the teacher has not changed them
      if (!_editing && _weight.text == (type == TeacherExamType.monthly ? '10' : '20')) {
        _weight.text = type == TeacherExamType.monthly ? '20' : '10';
        _pass.text = type == TeacherExamType.monthly ? '10' : '5';
      }
      _month ??= _date.month;
    });
  }

  Future<void> _pickDate() async {
    final now = widget.now ?? DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year - 1),
      lastDate: DateTime(now.year + 2),
    );
    if (picked != null) {
      setState(() {
        // A monthly exam usually follows its date's month
        if (_month == _date.month) _month = picked.month;
        _date = picked;
      });
    }
  }

  Future<void> _pickTime(bool start) async {
    final current = start ? _start : _end;
    final picked = await showTimePicker(context: context, initialTime: TimeOfDay(hour: current ~/ 60, minute: current % 60));
    if (picked == null) return;
    setState(() {
      final minutes = picked.hour * 60 + picked.minute;
      if (start) {
        final length = _end - _start;
        _start = minutes;
        _end = (minutes + (length > 0 ? length : 45)).clamp(0, 24 * 60 - 1);
      } else {
        _end = minutes;
      }
    });
  }

  double? _number(String text) => double.tryParse(text.trim().replaceAll(',', '.').replaceAll('٫', '.'));

  Future<void> _save() async {
    setState(() => _sectionError = _sectionId == null ? tr('اختر الفصل', 'Choose the class') : null);
    final valid = _formKey.currentState!.validate();
    if (!valid || _sectionError != null || _subject == null) return;
    if (_end <= _start) {
      _show(tr('وقت النهاية يجب أن يكون بعد وقت البداية.', 'The end time must be after the start time.'));
      return;
    }

    final input = ExamInput(
      subjectId: _subject!.subjectId,
      gradeId: _subject!.gradeId,
      sectionId: _sectionId!,
      type: _type,
      month: _type == TeacherExamType.monthly ? _month : null,
      date: _date,
      startMinutes: _start,
      endMinutes: _end,
      weight: _number(_weight.text)!,
      successScore: _number(_pass.text)!,
    );

    setState(() => _saving = true);
    try {
      if (_editing) {
        await widget.api.updateExam(widget.exam!.id, input);
      } else {
        await widget.api.createExam(input);
      }
      if (!mounted) return;
      _show(_editing ? tr('تم حفظ التعديلات.', 'Changes saved.') : tr('تمت إضافة الاختبار.', 'Exam added.'));
      Navigator.of(context).pop(true);
    } on ExamException catch (e) {
      if (mounted) _show(e.message);
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
    final subjects = widget.classes.subjects;
    final sections = _subject?.sections ?? const <TeacherSection>[];

    return Scaffold(
      appBar: AppBar(
        title: Text(_editing ? tr('تعديل الاختبار', 'Edit exam') : tr('اختبار جديد', 'New exam'),
            style: const TextStyle(fontWeight: FontWeight.bold)),
      ),
      body: !widget.classes.hasActiveSemester && !_editing
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(tr('لا يوجد فصل دراسي حالي، تواصل مع الإدارة.', 'There is no current semester. Please contact the school office.'),
                    textAlign: TextAlign.center),
              ),
            )
          : Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
                children: [
                  // Type: quiz or monthly
                  _label(tr('نوع الاختبار', 'Exam type')),
                  Row(
                    children: [
                      for (final t in TeacherExamType.creatable) ...[
                        if (t != TeacherExamType.creatable.first) const SizedBox(width: 10),
                        Expanded(child: _typeCard(t)),
                      ],
                    ],
                  ),
                  if (_type == TeacherExamType.monthly) ...[
                    const SizedBox(height: 14),
                    DropdownButtonFormField<int>(
                      key: const ValueKey('exam-month'),
                      initialValue: _month,
                      decoration: InputDecoration(labelText: tr('الشهر', 'Month'), prefixIcon: const Icon(Icons.calendar_month_rounded)),
                      items: [for (var m = 1; m <= 12; m++) DropdownMenuItem(value: m, child: Text(examMonthName(m)))],
                      onChanged: (m) => setState(() => _month = m),
                      validator: (m) => m == null ? tr('اختر الشهر', 'Choose the month') : null,
                    ),
                  ],
                  const SizedBox(height: 18),

                  // Subject and class
                  _label(tr('المادة والفصل', 'Subject and class')),
                  if (_editing || subjects.length == 1)
                    Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(14)),
                      child: Row(
                        children: [
                          Icon(Icons.book_rounded, color: palette.muted, size: 20),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              _subject?.label ??
                                  [widget.exam?.subjectName, widget.exam?.gradeName].whereType<String>().where((s) => s.isNotEmpty).join(' - '),
                              style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink),
                            ),
                          ),
                        ],
                      ),
                    )
                  else
                    DropdownButtonFormField<TeacherSubject>(
                      key: const ValueKey('exam-subject'),
                      initialValue: _subject,
                      isExpanded: true,
                      decoration: InputDecoration(labelText: tr('المادة', 'Subject'), prefixIcon: const Icon(Icons.book_rounded)),
                      items: [for (final s in subjects) DropdownMenuItem(value: s, child: Text(s.label, overflow: TextOverflow.ellipsis))],
                      onChanged: (s) => setState(() {
                        _subject = s;
                        _sectionId = s?.sections.length == 1 ? s!.sections.first.id : null;
                      }),
                      validator: (s) => s == null ? tr('اختر المادة', 'Choose the subject') : null,
                    ),
                  const SizedBox(height: 10),
                  if (_subject != null)
                    sections.isEmpty
                        ? Text(tr('لا توجد فصول لهذا الصف.', 'This grade has no classes.'), style: TextStyle(color: palette.muted))
                        : Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children: [
                              for (final s in sections)
                                ChoiceChip(
                                  key: ValueKey('exam-section-${s.id}'),
                                  label: Text(s.name.isEmpty ? tr('فصل', 'Class') : s.name),
                                  selected: _sectionId == s.id,
                                  onSelected: (_) => setState(() {
                                    _sectionId = s.id;
                                    _sectionError = null;
                                  }),
                                ),
                            ],
                          ),
                  if (_sectionError != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Text(_sectionError!, style: const TextStyle(color: _red, fontSize: 12)),
                    ),
                  const SizedBox(height: 18),

                  // Date and time
                  _label(tr('الموعد', 'When')),
                  _pickerTile(const ValueKey('exam-date'), Icons.event_rounded, tr('التاريخ', 'Date'), homeworkDate(_date), _pickDate),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: _pickerTile(const ValueKey('exam-start'), Icons.play_circle_outline_rounded, tr('يبدأ', 'Starts'),
                            examClock(_start), () => _pickTime(true)),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: _pickerTile(const ValueKey('exam-end'), Icons.stop_circle_outlined, tr('ينتهي', 'Ends'),
                            examClock(_end), () => _pickTime(false)),
                      ),
                    ],
                  ),
                  if (_end > _start)
                    Padding(
                      padding: const EdgeInsetsDirectional.only(start: 4, top: 6),
                      child: Text(tr('المدة ${_end - _start} دقيقة', 'Duration ${_end - _start} minutes'),
                          style: TextStyle(fontSize: 12, color: palette.muted)),
                    ),
                  const SizedBox(height: 18),

                  // Marks
                  _label(tr('الدرجات', 'Marks')),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: TextFormField(
                          key: const ValueKey('exam-weight'),
                          controller: _weight,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,٫]'))],
                          decoration: InputDecoration(labelText: tr('الدرجة الكاملة', 'Full mark'), prefixIcon: const Icon(Icons.star_rounded)),
                          validator: (v) {
                            final n = _number(v ?? '');
                            if (n == null || n <= 0) return tr('أدخل رقماً أكبر من صفر', 'Enter a number above zero');
                            if (n > 1000) return tr('رقم كبير جداً', 'Too large');
                            return null;
                          },
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: TextFormField(
                          key: const ValueKey('exam-pass'),
                          controller: _pass,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,٫]'))],
                          decoration: InputDecoration(labelText: tr('درجة النجاح', 'Pass mark'), prefixIcon: const Icon(Icons.check_circle_outline_rounded)),
                          validator: (v) {
                            final n = _number(v ?? '');
                            final full = _number(_weight.text);
                            if (n == null || n < 0) return tr('أدخل رقماً', 'Enter a number');
                            if (full != null && n > full) return tr('أكبر من الدرجة الكاملة', 'Above the full mark');
                            return null;
                          },
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 26),
                  FilledButton.icon(
                    key: const ValueKey('save-exam'),
                    onPressed: _saving ? null : _save,
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(50)),
                    icon: _saving
                        ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : const Icon(Icons.check_rounded),
                    label: Text(_editing ? tr('حفظ التعديلات', 'Save changes') : tr('إضافة الاختبار', 'Add exam')),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _label(String text) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(text, style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold, color: context.palette.ink)),
      );

  Widget _typeCard(TeacherExamType type) {
    final selected = _type == type;
    final colors = examColors(type);
    return InkWell(
      key: ValueKey('exam-type-${type.name}'),
      borderRadius: BorderRadius.circular(16),
      onTap: () => _setType(type),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 10),
        decoration: BoxDecoration(
          gradient: selected ? LinearGradient(colors: colors) : null,
          color: selected ? null : context.palette.card,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: selected ? Colors.transparent : context.palette.line),
        ),
        child: Column(
          children: [
            Icon(examIcon(type), color: selected ? Colors.white : colors.last, size: 26),
            const SizedBox(height: 6),
            Text(type.label,
                style: TextStyle(fontWeight: FontWeight.bold, color: selected ? Colors.white : context.palette.ink)),
          ],
        ),
      ),
    );
  }

  Widget _pickerTile(Key key, IconData icon, String label, String value, VoidCallback onTap) => InkWell(
        key: key,
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          decoration: BoxDecoration(
            color: context.palette.card,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: context.palette.line),
          ),
          child: Row(
            children: [
              Icon(icon, size: 20, color: AppTheme.blue),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(label, style: TextStyle(fontSize: 11, color: context.palette.muted)),
                    Text(value, style: TextStyle(fontWeight: FontWeight.w600, color: context.palette.ink)),
                  ],
                ),
              ),
            ],
          ),
        ),
      );
}

/// Students of the exam's class with a score field each; pass / fail shows as the teacher types
class ExamScoresScreen extends StatefulWidget {
  const ExamScoresScreen({super.key, this.api = const ExamApi(), required this.exam, this.onSessionExpired});

  final ExamApi api;
  final TeacherExam exam;
  final Future<void> Function()? onSessionExpired;

  @override
  State<ExamScoresScreen> createState() => _ExamScoresScreenState();
}

class _ExamScoresScreenState extends State<ExamScoresScreen> {
  ExamScores? _scores;
  final Map<String, TextEditingController> _fields = {};
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
    for (final c in _fields.values) {
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
      final scores = await widget.api.scores(widget.exam.id);
      if (!mounted) return;
      for (final c in _fields.values) {
        c.dispose();
      }
      _fields
        ..clear()
        ..addAll({
          for (final s in scores.students)
            s.studentId: TextEditingController(text: s.score == null ? '' : examMark(s.score!))..addListener(_changed),
        });
      setState(() {
        _scores = scores;
        _loading = false;
      });
    } on ExamException catch (e) {
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

  void _changed() => setState(() {});

  double get _weight => _scores?.weight ?? widget.exam.weight;
  double get _pass => _scores?.successScore ?? widget.exam.successScore;

  double? _value(String studentId) =>
      double.tryParse((_fields[studentId]?.text ?? '').trim().replaceAll(',', '.').replaceAll('٫', '.'));

  /// Text typed but not a valid score (0..full mark)
  bool _invalid(String studentId) {
    final text = (_fields[studentId]?.text ?? '').trim();
    if (text.isEmpty) return false;
    final v = _value(studentId);
    return v == null || v < 0 || v > _weight;
  }

  Future<void> _save() async {
    final students = _scores?.students ?? const <ExamStudentScore>[];
    if (students.any((s) => _invalid(s.studentId))) {
      _show(tr('صحّح الدرجات المظللة بالأحمر (من 0 إلى ${examMark(_weight)}).',
          'Fix the scores marked in red (0 to ${examMark(_weight)}).'));
      return;
    }
    // Only what changed: new / edited scores, and cleared ones (null removes the score)
    final changes = <String, double?>{
      for (final s in students)
        if (_value(s.studentId) != s.score) s.studentId: _value(s.studentId),
    };
    if (changes.isEmpty) {
      _show(tr('لا توجد تغييرات للحفظ.', 'Nothing changed.'));
      return;
    }

    setState(() => _saving = true);
    try {
      await widget.api.saveExamScores(widget.exam.id, changes);
      if (!mounted) return;
      _show(tr('تم حفظ الدرجات.', 'Scores saved.'));
      Navigator.of(context).pop(true);
    } on ExamException catch (e) {
      if (mounted) _show(e.message);
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
    final exam = widget.exam;
    final students = _scores?.students ?? const <ExamStudentScore>[];
    final entered = students.where((s) => _value(s.studentId) != null && !_invalid(s.studentId)).toList();
    final passed = entered.where((s) => _value(s.studentId)! >= _pass).length;

    return Scaffold(
      appBar: AppBar(title: Text(tr('رصد الدرجات', 'Enter scores'), style: const TextStyle(fontWeight: FontWeight.bold))),
      bottomNavigationBar: _loading || _error != null || students.isEmpty
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: FilledButton.icon(
                  key: const ValueKey('save-scores'),
                  onPressed: _saving ? null : _save,
                  style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(50)),
                  icon: _saving
                      ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.save_rounded),
                  label: Text(tr('حفظ الدرجات', 'Save scores')),
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
                    // Summary
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(colors: examColors(exam.type)),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(exam.title, style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
                          const SizedBox(height: 2),
                          Text(
                            [exam.subjectName, exam.classText, homeworkDate(exam.date)].where((s) => s.isNotEmpty).join(' · '),
                            style: TextStyle(color: Colors.white.withValues(alpha: .88), fontSize: 12.5),
                          ),
                          const SizedBox(height: 14),
                          Row(
                            children: [
                              _stat(tr('الدرجة الكاملة', 'Full mark'), examMark(_weight)),
                              _stat(tr('النجاح', 'Pass'), examMark(_pass)),
                              _stat(tr('رُصدت', 'Entered'), '${entered.length}/${students.length}'),
                              _stat(tr('ناجح', 'Passed'), '$passed'),
                            ],
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    if (students.isEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 40),
                        child: Text(tr('لا يوجد طلاب في هذا الفصل.', 'There are no students in this class.'),
                            textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
                      )
                    else
                      for (var i = 0; i < students.length; i++) ...[
                        _studentRow(i + 1, students[i]),
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

  Widget _studentRow(int number, ExamStudentScore student) {
    final palette = context.palette;
    final invalid = _invalid(student.studentId);
    final value = invalid ? null : _value(student.studentId);
    final status = value == null ? null : value >= _pass;
    final color = invalid ? _red : status == null ? palette.line : (status ? _green : _red);

    return Container(
      padding: const EdgeInsets.fromLTRB(12, 8, 10, 8),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: status == null && !invalid ? palette.line : color.withValues(alpha: .5)),
      ),
      child: Row(
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
                Text(student.name, maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
                Text(
                  invalid
                      ? tr('من 0 إلى ${examMark(_weight)}', '0 to ${examMark(_weight)}')
                      : status == null
                          ? tr('لم تُرصد', 'Not entered')
                          : status
                              ? tr('ناجح', 'Passed')
                              : tr('راسب', 'Failed'),
                  style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w600, color: status == null && !invalid ? palette.muted : color),
                ),
              ],
            ),
          ),
          SizedBox(
            width: 92,
            child: TextField(
              key: ValueKey('score-${student.studentId}'),
              controller: _fields[student.studentId],
              textAlign: TextAlign.center,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,٫]'))],
              textInputAction: TextInputAction.next,
              style: const TextStyle(fontWeight: FontWeight.bold),
              decoration: InputDecoration(
                isDense: true,
                hintText: '—',
                suffixText: '/${examMark(_weight)}',
                errorText: invalid ? '' : null,
                errorStyle: const TextStyle(height: 0, fontSize: 0),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
