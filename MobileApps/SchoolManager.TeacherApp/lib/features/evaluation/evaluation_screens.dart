import 'package:flutter/material.dart';

import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/auth/auth_service.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../attendance/attendance_screens.dart' show attendanceDayLabel;
import '../classes/classes_service.dart';
import '../timetable/teacher_timetable_screen.dart' show lessonTime, subjectColorOf;
import '../timetable/teacher_timetable_service.dart';
import 'evaluation_service.dart';

const _indigo = Color(0xFF4C5FD5);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _star = Color(0xFFF7B924);

List<String> get _dayNames => AppLanguage.instance.isEnglish
    ? const ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
    : const ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

Color healthColor(StudentHealth h) => switch (h) {
      StudentHealth.good => _green,
      StudentHealth.tired => _orange,
      StudentHealth.sick => _red,
    };

DateTime _dateOnly(DateTime d) => DateTime(d.year, d.month, d.day);

/// The latest day on or before [today] that falls on [dayOfWeek] (0 = Sunday … 6 = Saturday)
DateTime lastDateOn(int dayOfWeek, DateTime today) {
  final t = _dateOnly(today);
  final back = (t.weekday % 7 - dayOfWeek + 7) % 7;
  return t.subtract(Duration(days: back));
}

/// "منذ 3 أيام" / "3 days ago"
String _ago(DateTime when, DateTime now) {
  final days = _dateOnly(now).difference(_dateOnly(when)).inDays;
  if (days <= 0) return tr('اليوم', 'Today');
  if (days == 1) return tr('أمس', 'Yesterday');
  return tr('منذ $days أيام', '$days days ago');
}

/// A lesson with its day of the week
class _DayLesson {
  const _DayLesson(this.dayOfWeek, this.lesson);
  final int dayOfWeek;
  final TeacherLesson lesson;
}

/// The API calls of the evaluation pages (tests pass their own)
class EvaluationApi {
  const EvaluationApi({
    this.loadTimetable,
    this.loadClasses,
    this.loadStudents,
    this.loadPending,
    this.loadLessonEvaluations,
    this.loadStudentEvaluations,
    this.save,
    this.teacherId,
  });

  final Future<TeacherTimetable> Function()? loadTimetable;
  final Future<List<MyClass>> Function()? loadClasses;
  final Future<List<ClassStudent>> Function(String sectionId)? loadStudents;
  final Future<List<PendingEvaluationRequest>> Function()? loadPending;
  final Future<List<StudentEvaluation>> Function(TeacherLesson lesson, DateTime date)? loadLessonEvaluations;
  final Future<List<StudentEvaluation>> Function(String studentId, DateTime from, DateTime to)? loadStudentEvaluations;
  final Future<void> Function(TeacherLesson lesson, DateTime date, EvaluationInput input)? save;

  /// The logged-in teacher (whose evaluations can be changed); defaults to the session
  final String? teacherId;

  String? get myTeacherId => teacherId ?? AuthService.instance.session?.teacherId;

  Future<TeacherTimetable> timetable() async {
    try {
      return await (loadTimetable ?? TeacherTimetableService.instance.getMyTimetable)();
    } on TeacherTimetableException catch (e) {
      throw EvaluationException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<MyClass>> classes() async {
    try {
      return await (loadClasses ?? ClassesService.instance.getMyClasses)();
    } on ClassesException catch (e) {
      throw EvaluationException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<ClassStudent>> students(String sectionId) async {
    try {
      return await (loadStudents ?? ClassesService.instance.getStudents)(sectionId);
    } on ClassesException catch (e) {
      throw EvaluationException(e.message, sessionExpired: e.sessionExpired);
    }
  }

  Future<List<PendingEvaluationRequest>> pending() => (loadPending ?? EvaluationService.instance.getPendingRequests)();

  Future<List<StudentEvaluation>> lessonEvaluations(TeacherLesson lesson, DateTime date) =>
      (loadLessonEvaluations ?? EvaluationService.instance.getLessonEvaluations)(lesson, date);

  Future<List<StudentEvaluation>> studentEvaluations(String studentId, DateTime from, DateTime to) =>
      (loadStudentEvaluations ?? EvaluationService.instance.getStudentEvaluations)(studentId, from, to);

  Future<void> evaluate(TeacherLesson lesson, DateTime date, EvaluationInput input) =>
      (save ?? EvaluationService.instance.evaluate)(lesson, date, input);
}

// ---------------------------------------------------------------------------
// Requests + lessons
// ---------------------------------------------------------------------------

/// Student evaluations: the parents' waiting requests (answer them) and the lessons of a day (evaluate the class)
class EvaluationsScreen extends StatefulWidget {
  const EvaluationsScreen({super.key, this.api = const EvaluationApi(), this.onSessionExpired, this.today});

  final EvaluationApi api;
  final Future<void> Function()? onSessionExpired;

  /// "Today" (tests pass their own)
  final DateTime? today;

  @override
  State<EvaluationsScreen> createState() => _EvaluationsScreenState();
}

class _EvaluationsScreenState extends State<EvaluationsScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  TeacherTimetable? _timetable;
  List<MyClass> _classes = const [];

  /// Section id -> its students
  Map<String, List<ClassStudent>> _students = const {};
  List<PendingEvaluationRequest> _pending = const [];

  /// "All subjects" request id -> the subjects already evaluated for it (subject id -> the evaluation)
  Map<String, Map<String, StudentEvaluation>> _answered = const {};
  bool _loading = true;
  String? _error;
  late DateTime _date;

  DateTime get _today => _dateOnly(widget.today ?? DateTime.now());

  /// The subjects the teacher teaches the class (from the timetable), by name
  List<(String, String)> _subjectsOfSection(String sectionId) {
    final byId = <String, String>{};
    for (final l in _allLessons) {
      if (l.lesson.sectionId == sectionId) byId.putIfAbsent(l.lesson.subjectId, () => _subjectName(l.lesson.subjectId) ?? l.lesson.subject);
    }
    return byId.entries.map((e) => (e.key, e.value)).toList()..sort((a, b) => a.$2.compareTo(b.$2));
  }

  /// The subjects of an "all subjects" request still to evaluate (empty for a one-subject request)
  List<(String, String)> _remainingOf(PendingEvaluationRequest r) {
    if (r.subjectId != null) return const [];
    final found = _studentOf(r.studentId);
    if (found == null) return const [];
    final done = _answered[r.id] ?? const {};
    return _subjectsOfSection(found.$2.section.id).where((s) => !done.containsKey(s.$1)).toList();
  }

  /// For every "all subjects" request: the subjects already evaluated for it. A request with every subject
  /// evaluated (e.g. the last one from the lesson tab) is closed here, so it leaves the list.
  Future<List<PendingEvaluationRequest>> _withAnswers(List<PendingEvaluationRequest> pending, TeacherTimetable timetable) async {
    final me = widget.api.myTeacherId;
    final answered = <String, Map<String, StudentEvaluation>>{};
    await Future.wait(pending.where((r) => r.subjectId == null).map((r) async {
      try {
        final asked = _dateOnly(r.requestedAt).subtract(const Duration(days: 1));
        final list = await widget.api.studentEvaluations(r.studentId, asked, _today);
        answered[r.id] = {
          for (final e in list)
            if (e.evaluationRequestId == r.id && (me == null || e.teacherId == me)) e.subjectId: e,
        };
      } on EvaluationException catch (e) {
        if (e.sessionExpired) rethrow;
      }
    }));
    _answered = answered;

    // Close the ones with nothing left: save one of their evaluations again, as the last subject
    final closed = <String>{};
    for (final r in pending.where((r) => r.subjectId == null)) {
      final done = answered[r.id];
      if (done == null || done.isEmpty || _remainingOf(r).isNotEmpty) continue;
      final last = done.values.first;
      final lesson = [for (final d in timetable.days) ...d.lessons].where((l) => l.id == last.timetableSlotId).firstOrNull;
      if (lesson == null || last.date == null) continue;
      try {
        await widget.api.evaluate(
          lesson,
          last.date!,
          EvaluationInput(
            studentId: r.studentId,
            focus: last.focus,
            interaction: last.interaction,
            behavior: last.behavior,
            health: last.health,
            comment: last.comment,
            evaluationRequestId: r.id,
          ),
        );
        closed.add(r.id);
      } on EvaluationException catch (e) {
        if (e.sessionExpired) rethrow;
      }
    }
    return pending.where((r) => !closed.contains(r.id)).toList();
  }

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
      final results = await Future.wait([widget.api.timetable(), widget.api.classes(), widget.api.pending()]);
      final classes = results[1] as List<MyClass>;
      // Students of every class (for names); a failing class just has none
      final students = <String, List<ClassStudent>>{};
      await Future.wait(classes.map((c) async {
        try {
          students[c.section.id] = [...await widget.api.students(c.section.id)]..sort((a, b) => a.name.compareTo(b.name));
        } on EvaluationException catch (e) {
          if (e.sessionExpired) rethrow;
        }
      }));
      if (!mounted) return;
      final timetable = results[0] as TeacherTimetable;
      _timetable = timetable;
      _classes = classes;
      _students = students;
      final pending = await _withAnswers(results[2] as List<PendingEvaluationRequest>, timetable);
      if (!mounted) return;
      setState(() {
        _pending = pending;
        _loading = false;
      });
    } on EvaluationException catch (e) {
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

  Future<void> _reloadPending() async {
    try {
      final timetable = _timetable;
      var pending = await widget.api.pending();
      if (timetable != null) pending = await _withAnswers(pending, timetable);
      if (mounted) setState(() => _pending = pending);
    } on EvaluationException catch (_) {}
  }

  List<_DayLesson> get _allLessons => [
        for (final d in _timetable?.days ?? const <TeacherTimetableDay>[])
          for (final l in d.lessons) _DayLesson(d.dayOfWeek, l),
      ];

  /// The student and their class
  (ClassStudent, MyClass)? _studentOf(String studentId) {
    for (final c in _classes) {
      final s = (_students[c.section.id] ?? const <ClassStudent>[]).where((s) => s.id == studentId).firstOrNull;
      if (s != null) return (s, c);
    }
    return null;
  }

  String? _subjectName(String? subjectId) {
    if (subjectId == null) return null;
    for (final c in _classes) {
      for (final s in c.subjects) {
        if (s.subjectId == subjectId && s.subjectName.isNotEmpty) return s.subjectName;
      }
    }
    return _allLessons.where((l) => l.lesson.subjectId == subjectId).firstOrNull?.lesson.subject;
  }

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  /// Answers a request: a lesson of the student's class (of the subject asked about), on its last day
  Future<void> _answer(PendingEvaluationRequest request) async {
    final found = _studentOf(request.studentId);
    if (found == null) {
      _show(tr('هذا الطالب ليس في فصولك هذا الفصل الدراسي.', 'This student is not in your classes this semester.'));
      return;
    }
    final (student, myClass) = found;
    final lessons = _allLessons
        .where((l) => l.lesson.sectionId == myClass.section.id && (request.subjectId == null || l.lesson.subjectId == request.subjectId))
        .toList();
    if (lessons.isEmpty) {
      _show(tr('لا توجد لديك حصة مع هذا الفصل في الجدول.', 'You have no lesson with this class in the timetable.'));
      return;
    }

    // All subjects and the teacher teaches the class more than one: evaluate each subject, the request closes
    // with the last one
    final bySubject = _latestLessonPerSubject(lessons);
    if (request.subjectId == null && bySubject.length > 1) {
      final done = await Navigator.of(context).push<bool>(MaterialPageRoute(
        builder: (_) => RequestSubjectsScreen(
          api: widget.api,
          request: request,
          student: student,
          classLabel: myClass.label,
          subjectLessons: [for (final l in bySubject) (l.lesson, lastDateOn(l.dayOfWeek, _today))],
          today: _today,
          onSessionExpired: widget.onSessionExpired,
        ),
      ));
      if (mounted) {
        if (done == true) _show(tr('تم تقييم كل المواد وإرسالها لولي الأمر.', 'All subjects evaluated and sent to the parent.'));
        _reloadPending();
      }
      return;
    }

    final chosen = lessons.length == 1 ? lessons.first : await _pickLesson(lessons);
    if (chosen == null || !mounted) return;

    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => EvaluateStudentScreen(
        api: widget.api,
        student: student,
        classLabel: myClass.label,
        lesson: chosen.lesson,
        date: lastDateOn(chosen.dayOfWeek, _today),
        requestId: request.id,
        requestNote: request.note,
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (saved == true && mounted) {
      _show(tr('تم حفظ التقييم وإرساله لولي الأمر.', 'Evaluation saved and sent to the parent.'));
      _reloadPending();
    }
  }

  /// One lesson per subject: the one that took place most recently (on or before today)
  List<_DayLesson> _latestLessonPerSubject(List<_DayLesson> lessons) {
    final best = <String, _DayLesson>{};
    for (final l in lessons) {
      final current = best[l.lesson.subjectId];
      if (current == null) {
        best[l.lesson.subjectId] = l;
        continue;
      }
      final a = lastDateOn(l.dayOfWeek, _today);
      final b = lastDateOn(current.dayOfWeek, _today);
      if (a.isAfter(b) || (a == b && l.lesson.start > current.lesson.start)) best[l.lesson.subjectId] = l;
    }
    return best.values.toList()..sort((a, b) => a.lesson.subject.compareTo(b.lesson.subject));
  }

  Future<_DayLesson?> _pickLesson(List<_DayLesson> lessons) => showModalBottomSheet<_DayLesson>(
        context: context,
        showDragHandle: true,
        builder: (sheetContext) => SafeArea(
          child: ListView(
            shrinkWrap: true,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
                child: Text(tr('اختر الحصة', 'Choose the lesson'), style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
              ),
              for (final l in lessons)
                ListTile(
                  key: ValueKey('pick-lesson-${l.lesson.id}'),
                  leading: CircleAvatar(
                    backgroundColor: subjectColorOf(l.lesson.subject).withValues(alpha: .12),
                    child: Icon(Icons.menu_book_rounded, color: subjectColorOf(l.lesson.subject)),
                  ),
                  title: Text(l.lesson.subject),
                  subtitle: Text('${_dayNames[l.dayOfWeek]} · ${lessonTime(l.lesson.start)} · ${attendanceDayLabel(lastDateOn(l.dayOfWeek, _today))}'),
                  onTap: () => Navigator.of(sheetContext).pop(l),
                ),
            ],
          ),
        ),
      );

  Future<void> _openLesson(TeacherLesson lesson) async {
    final myClass = _classes.where((c) => c.section.id == lesson.sectionId).firstOrNull;
    await Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => LessonEvaluationScreen(
        api: widget.api,
        lesson: lesson,
        date: _date,
        students: _students[lesson.sectionId] ?? const [],
        classLabel: myClass?.label ?? lesson.classLabel,
        pendingRequests: _pending,
        remainingSubjects: {
          for (final r in _pending)
            if (r.subjectId == null) r.id: {for (final s in _remainingOf(r)) s.$1},
        },
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (mounted) _reloadPending();
  }

  void _setDate(DateTime date) {
    final d = _dateOnly(date);
    if (d.isAfter(_today)) return;
    setState(() => _date = d);
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

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      initialIndex: 0,
      child: Scaffold(
        appBar: AppBar(
          title: Text(tr('تقييم الطلاب', 'Student evaluations'), style: const TextStyle(fontWeight: FontWeight.bold)),
          bottom: TabBar(
            labelColor: Colors.white,
            unselectedLabelColor: Colors.white70,
            indicatorColor: Colors.white,
            indicatorWeight: 3,
            labelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            tabs: [
              Tab(
                key: const ValueKey('tab-requests'),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Flexible(child: Text(tr('طلبات الأهل', 'Requests'), overflow: TextOverflow.ellipsis)),
                    if (_pending.isNotEmpty) ...[
                      const SizedBox(width: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
                        decoration: BoxDecoration(color: _red, borderRadius: BorderRadius.circular(10)),
                        child: Text('${_pending.length}', style: const TextStyle(color: Colors.white, fontSize: 11.5)),
                      ),
                    ],
                  ],
                ),
              ),
              Tab(key: const ValueKey('tab-lessons'), text: tr('تقييم حصة', 'Evaluate a lesson')),
            ],
          ),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _message(context, Icons.error_outline_rounded, _error!, retry: _load)
                : TabBarView(children: [_requestsTab(), _lessonsTab()]),
      ),
    );
  }

  Widget _requestsTab() {
    final palette = context.palette;
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
        children: [
          if (_pending.isEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 60),
              child: _message(context, Icons.mark_email_read_rounded,
                  tr('لا توجد طلبات تقييم بانتظارك.', 'No evaluation requests are waiting for you.')),
            )
          else ...[
            Text(tr('يطلب أولياء الأمور تقييم أبنائهم. اختر "قيّم الآن" للرد.', 'Parents ask how their children are doing. Tap "Evaluate" to answer.'),
                style: TextStyle(fontSize: 12.5, color: palette.muted)),
            const SizedBox(height: 10),
            for (final r in _pending) ...[
              _requestCard(r),
              const SizedBox(height: 10),
            ],
          ],
        ],
      ),
    );
  }

  Widget _requestCard(PendingEvaluationRequest r) {
    final palette = context.palette;
    final found = _studentOf(r.studentId);
    final subject = _subjectName(r.subjectId) ?? (r.subjectId == null ? tr('كل المواد', 'All subjects') : null);
    final allSubjects = r.subjectId == null && found != null ? _subjectsOfSection(found.$2.section.id) : const <(String, String)>[];
    final remaining = _remainingOf(r);

    return Container(
      key: ValueKey('request-${r.id}'),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: _orange.withValues(alpha: .45)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: _indigo.withValues(alpha: .12),
                child: Text(found == null || found.$1.name.isEmpty ? '?' : found.$1.name.characters.first,
                    style: const TextStyle(color: _indigo, fontWeight: FontWeight.bold)),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(found?.$1.name ?? tr('طالب', 'Student'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                    Text([found?.$2.label, subject].whereType<String>().where((s) => s.isNotEmpty).join(' · '),
                        style: TextStyle(fontSize: 12, color: palette.muted)),
                  ],
                ),
              ),
              Text(_ago(r.requestedAt, _today), style: TextStyle(fontSize: 11.5, color: palette.muted)),
            ],
          ),
          if (r.note != null) ...[
            const SizedBox(height: 10),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(12)),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.format_quote_rounded, size: 18, color: palette.muted),
                  const SizedBox(width: 6),
                  Expanded(child: Text(r.note!, style: TextStyle(fontSize: 13, color: palette.ink))),
                ],
              ),
            ),
          ],
          // "All subjects": the ones left (evaluated subjects drop off)
          if (allSubjects.length > 1) ...[
            const SizedBox(height: 10),
            Text(tr('تم ${allSubjects.length - remaining.length} من ${allSubjects.length} مواد · المتبقي:',
                    '${allSubjects.length - remaining.length} of ${allSubjects.length} subjects done · left:'),
                key: ValueKey('request-progress-${r.id}'),
                style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: palette.muted)),
            const SizedBox(height: 6),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                for (final s in remaining)
                  Container(
                    key: ValueKey('remaining-${r.id}-${s.$1}'),
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                    decoration: BoxDecoration(color: _orange.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                    child: Text(s.$2, style: const TextStyle(color: _orange, fontSize: 12, fontWeight: FontWeight.bold)),
                  ),
              ],
            ),
          ],
          const SizedBox(height: 10),
          Align(
            alignment: AlignmentDirectional.centerEnd,
            child: FilledButton.icon(
              key: ValueKey('answer-${r.id}'),
              onPressed: () => _answer(r),
              style: FilledButton.styleFrom(backgroundColor: _indigo),
              icon: const Icon(Icons.star_rounded, size: 18),
              label: Text(tr('قيّم الآن', 'Evaluate')),
            ),
          ),
        ],
      ),
    );
  }

  Widget _lessonsTab() {
    final palette = context.palette;
    final lessons = _timetable?.dayOf(_date.weekday % 7)?.lessons ?? const <TeacherLesson>[];
    final isToday = _date == _today;

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 10),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [Color(0xFF6D7DF2), _indigo]),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Row(
            children: [
              IconButton(
                key: const ValueKey('eval-previous-day'),
                onPressed: () => _setDate(_date.subtract(const Duration(days: 1))),
                icon: const Icon(Icons.chevron_left_rounded, color: Colors.white),
              ),
              Expanded(
                child: InkWell(
                  borderRadius: BorderRadius.circular(12),
                  onTap: _pickDate,
                  child: Column(
                    children: [
                      Text(attendanceDayLabel(_date),
                          style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
                      Text(isToday ? tr('اليوم · اضغط لاختيار يوم آخر', 'Today · tap to choose another day') : tr('اضغط لاختيار يوم آخر', 'Tap to choose another day'),
                          style: TextStyle(color: Colors.white.withValues(alpha: .8), fontSize: 11.5)),
                    ],
                  ),
                ),
              ),
              IconButton(
                key: const ValueKey('eval-next-day'),
                onPressed: isToday ? null : () => _setDate(_date.add(const Duration(days: 1))),
                icon: Icon(Icons.chevron_right_rounded, color: isToday ? Colors.white38 : Colors.white),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        if (lessons.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 40),
            child: Text(tr('لا توجد لديك حصص في هذا اليوم.', 'You have no lessons on this day.'),
                textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
          )
        else
          for (final lesson in lessons) ...[
            _lessonCard(lesson),
            const SizedBox(height: 10),
          ],
      ],
    );
  }

  Widget _lessonCard(TeacherLesson lesson) {
    final palette = context.palette;
    final color = subjectColorOf(lesson.subject);
    final students = _students[lesson.sectionId] ?? const <ClassStudent>[];
    final requests = _pending.where((r) => students.any((s) => s.id == r.studentId)).length;

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        key: ValueKey('eval-lesson-${lesson.id}'),
        borderRadius: BorderRadius.circular(18),
        onTap: () => _openLesson(lesson),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(borderRadius: BorderRadius.circular(18), border: Border.all(color: palette.line)),
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
                    if (lesson.classLabel.isNotEmpty) Text(lesson.classLabel, style: TextStyle(fontSize: 12.5, color: palette.muted)),
                    Text(tr('${students.length} طالب', '${students.length} students'), style: TextStyle(fontSize: 11.5, color: palette.muted)),
                  ],
                ),
              ),
              if (requests > 0)
                Container(
                  margin: const EdgeInsetsDirectional.only(end: 6),
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(color: _orange.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                  child: Text(tr('$requests طلب', '$requests requests'),
                      style: const TextStyle(color: _orange, fontSize: 11, fontWeight: FontWeight.bold)),
                ),
              Icon(AppLanguage.instance.isEnglish ? Icons.chevron_right_rounded : Icons.chevron_left_rounded, color: palette.faint),
            ],
          ),
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// An "all subjects" request: one evaluation per subject the teacher teaches the class
// ---------------------------------------------------------------------------

/// Each subject the teacher teaches the student's class, evaluated for this request or not yet. The request is
/// closed (sent to the parent) with the last subject.
class RequestSubjectsScreen extends StatefulWidget {
  const RequestSubjectsScreen({
    super.key,
    this.api = const EvaluationApi(),
    required this.request,
    required this.student,
    required this.classLabel,
    required this.subjectLessons,
    required this.today,
    this.onSessionExpired,
  });

  final EvaluationApi api;
  final PendingEvaluationRequest request;
  final ClassStudent student;
  final String classLabel;

  /// One lesson per subject with the date it is evaluated on
  final List<(TeacherLesson, DateTime)> subjectLessons;
  final DateTime today;
  final Future<void> Function()? onSessionExpired;

  @override
  State<RequestSubjectsScreen> createState() => _RequestSubjectsScreenState();
}

class _RequestSubjectsScreenState extends State<RequestSubjectsScreen> {
  /// Subject id -> its evaluation for this request
  Map<String, StudentEvaluation> _done = const {};
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final asked = widget.request.requestedAt;
      final from = DateTime(asked.year, asked.month, asked.day).subtract(const Duration(days: 1));
      final list = await widget.api.studentEvaluations(widget.student.id, from, widget.today);
      final me = widget.api.myTeacherId;
      if (!mounted) return;
      setState(() {
        _done = {
          for (final e in list)
            if (e.evaluationRequestId == widget.request.id && (me == null || e.teacherId == me)) e.subjectId: e,
        };
        _loading = false;
      });
    } on EvaluationException catch (e) {
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

  Future<void> _open(TeacherLesson lesson, DateTime date) async {
    final existing = _done[lesson.subjectId];
    // The request closes when every other subject is already evaluated
    final others = widget.subjectLessons.where((s) => s.$1.subjectId != lesson.subjectId);
    final last = others.every((s) => _done.containsKey(s.$1.subjectId));

    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => EvaluateStudentScreen(
        api: widget.api,
        student: widget.student,
        classLabel: widget.classLabel,
        lesson: lesson,
        date: existing?.date ?? date,
        existing: existing,
        requestId: widget.request.id,
        requestNote: widget.request.note,
        completeRequest: last,
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (saved != true || !mounted) return;
    if (last) {
      Navigator.of(context).pop(true);
      return;
    }
    await _load();
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final total = widget.subjectLessons.length;
    final done = widget.subjectLessons.where((s) => _done.containsKey(s.$1.subjectId)).length;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.student.name, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(widget.classLabel, style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(context, Icons.error_outline_rounded, _error!, retry: _load)
              : ListView(
                  padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
                  children: [
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(colors: [Color(0xFF6D7DF2), _indigo]),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(tr('طلب تقييم لكل المواد', 'Evaluation request for all subjects'),
                              style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
                          if (widget.request.note != null) ...[
                            const SizedBox(height: 4),
                            Text('${tr('ولي الأمر:', 'Parent:')} ${widget.request.note}',
                                style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5)),
                          ],
                          const SizedBox(height: 12),
                          ClipRRect(
                            borderRadius: BorderRadius.circular(6),
                            child: LinearProgressIndicator(
                              value: total == 0 ? 0 : done / total,
                              minHeight: 6,
                              color: Colors.white,
                              backgroundColor: Colors.white.withValues(alpha: .25),
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(tr('تم تقييم $done من $total مواد', '$done of $total subjects evaluated'),
                              key: const ValueKey('request-subjects-progress'),
                              style: const TextStyle(color: Colors.white, fontSize: 12.5, fontWeight: FontWeight.w600)),
                        ],
                      ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      tr('قيّم الطالب في كل مادة تدرّسها له. يُرسل الطلب لولي الأمر بعد آخر مادة.',
                          'Evaluate the student in each subject you teach them. The request goes to the parent after the last one.'),
                      style: TextStyle(fontSize: 12.5, color: palette.muted),
                    ),
                    const SizedBox(height: 10),
                    for (final (lesson, date) in widget.subjectLessons) ...[
                      _subjectTile(lesson, date),
                      const SizedBox(height: 8),
                    ],
                  ],
                ),
    );
  }

  Widget _subjectTile(TeacherLesson lesson, DateTime date) {
    final palette = context.palette;
    final e = _done[lesson.subjectId];
    final color = subjectColorOf(lesson.subject);

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        key: ValueKey('request-subject-${lesson.subjectId}'),
        borderRadius: BorderRadius.circular(16),
        onTap: () => _open(lesson, date),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: e == null ? palette.line : _green.withValues(alpha: .5)),
          ),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: color.withValues(alpha: .12),
                child: Icon(Icons.menu_book_rounded, color: color),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(lesson.subject, style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                    if (e == null)
                      Text('${tr('لم تُقيَّم', 'Not evaluated')} · ${attendanceDayLabel(date)}',
                          style: TextStyle(fontSize: 11.5, color: palette.muted))
                    else
                      Row(
                        children: [
                          for (var i = 1; i <= 5; i++)
                            Icon(i <= e.average.round() ? Icons.star_rounded : Icons.star_outline_rounded,
                                size: 15, color: i <= e.average.round() ? _star : palette.faint),
                          const SizedBox(width: 6),
                          Text(e.health.label, style: TextStyle(fontSize: 11.5, color: healthColor(e.health), fontWeight: FontWeight.w600)),
                        ],
                      ),
                  ],
                ),
              ),
              Icon(e == null ? Icons.radio_button_unchecked_rounded : Icons.check_circle_rounded, color: e == null ? palette.faint : _green),
            ],
          ),
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// The students of one lesson
// ---------------------------------------------------------------------------

/// A lesson on a day: each student of the class with their evaluation (or none yet); tap one to evaluate
class LessonEvaluationScreen extends StatefulWidget {
  const LessonEvaluationScreen({
    super.key,
    this.api = const EvaluationApi(),
    required this.lesson,
    required this.date,
    required this.students,
    required this.classLabel,
    this.pendingRequests = const [],
    this.remainingSubjects = const {},
    this.onSessionExpired,
  });

  final EvaluationApi api;
  final TeacherLesson lesson;
  final DateTime date;
  final List<ClassStudent> students;
  final String classLabel;

  /// Requests waiting for this teacher (the ones of this class's students show on the list)
  final List<PendingEvaluationRequest> pendingRequests;

  /// "All subjects" request id -> the subject ids still to evaluate for it
  final Map<String, Set<String>> remainingSubjects;
  final Future<void> Function()? onSessionExpired;

  @override
  State<LessonEvaluationScreen> createState() => _LessonEvaluationScreenState();
}

class _LessonEvaluationScreenState extends State<LessonEvaluationScreen> {
  Map<String, StudentEvaluation> _evaluations = const {};
  late final List<PendingEvaluationRequest> _pending = [...widget.pendingRequests];
  late final Map<String, Set<String>> _remaining = {
    for (final e in widget.remainingSubjects.entries) e.key: {...e.value},
  };

  /// Saving [request] for this lesson's subject finishes it: one subject, or the last one left of "all subjects"
  bool _finishes(PendingEvaluationRequest request) {
    if (request.subjectId != null) return true;
    final left = _remaining[request.id];
    return left != null && left.difference({widget.lesson.subjectId}).isEmpty;
  }

  /// The student's waiting request this lesson answers: about this subject, else about all subjects
  PendingEvaluationRequest? _requestOf(String studentId) {
    final mine = _pending.where((r) => r.studentId == studentId);
    return mine.where((r) => r.subjectId == widget.lesson.subjectId).firstOrNull ??
        mine.where((r) => r.subjectId == null).firstOrNull;
  }
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final list = await widget.api.lessonEvaluations(widget.lesson, widget.date);
      if (!mounted) return;
      setState(() {
        _evaluations = {for (final e in list) e.studentId: e};
        _loading = false;
      });
    } on EvaluationException catch (e) {
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

  Future<void> _open(ClassStudent student) async {
    final existing = _evaluations[student.id];
    final me = widget.api.myTeacherId;
    if (existing != null && me != null && existing.teacherId != me) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(tr('قيّم معلم آخر هذا الطالب في هذه الحصة.', 'Another teacher evaluated this student in this lesson.'))));
      return;
    }
    // A request about this subject is answered; an "all subjects" one gets this subject and is answered with
    // its last subject
    final request = _requestOf(student.id);
    final finishes = request != null && _finishes(request);
    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => EvaluateStudentScreen(
        api: widget.api,
        student: student,
        classLabel: widget.classLabel,
        lesson: widget.lesson,
        date: widget.date,
        existing: existing,
        requestId: request?.id,
        requestNote: request?.note,
        completeRequest: finishes,
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (saved == true && mounted) {
      setState(() {
        if (request != null && finishes) {
          _pending.removeWhere((r) => r.id == request.id);
        } else if (request != null) {
          _remaining[request.id]?.remove(widget.lesson.subjectId);
        }
      });
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final done = widget.students.where((s) => _evaluations.containsKey(s.id)).length;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.lesson.subject, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text('${widget.classLabel} · ${attendanceDayLabel(widget.date)}', style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(context, Icons.error_outline_rounded, _error!, retry: _load)
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
                    children: [
                      Text(tr('تم تقييم $done من ${widget.students.length} طالب', '$done of ${widget.students.length} students evaluated'),
                          style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                      const SizedBox(height: 10),
                      if (widget.students.isEmpty)
                        Padding(
                          padding: const EdgeInsets.only(top: 40),
                          child: Text(tr('لا يوجد طلاب في هذا الفصل.', 'There are no students in this class.'),
                              textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
                        )
                      else
                        for (var i = 0; i < widget.students.length; i++) ...[
                          _studentTile(i + 1, widget.students[i]),
                          const SizedBox(height: 8),
                        ],
                    ],
                  ),
                ),
    );
  }

  Widget _studentTile(int number, ClassStudent student) {
    final palette = context.palette;
    final e = _evaluations[student.id];
    final request = _requestOf(student.id);
    final requested = request != null;
    final mine = e == null || widget.api.myTeacherId == null || e.teacherId == widget.api.myTeacherId;

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        key: ValueKey('eval-student-${student.id}'),
        borderRadius: BorderRadius.circular(16),
        onTap: () => _open(student),
        child: Container(
          padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: requested ? _orange.withValues(alpha: .5) : palette.line),
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
                    if (e == null)
                      Text(tr('لم يُقيَّم', 'Not evaluated'), style: TextStyle(fontSize: 11.5, color: palette.muted))
                    else
                      Row(
                        children: [
                          for (var i = 1; i <= 5; i++)
                            Icon(i <= e.average.round() ? Icons.star_rounded : Icons.star_outline_rounded,
                                size: 15, color: i <= e.average.round() ? _star : palette.faint),
                          const SizedBox(width: 6),
                          Text(e.health.label, style: TextStyle(fontSize: 11.5, color: healthColor(e.health), fontWeight: FontWeight.w600)),
                          if (!mine) ...[
                            const SizedBox(width: 6),
                            Text(tr('· معلم آخر', '· another teacher'), style: TextStyle(fontSize: 11.5, color: palette.muted)),
                          ],
                        ],
                      ),
                  ],
                ),
              ),
              if (requested)
                Flexible(
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                    decoration: BoxDecoration(color: _orange.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                    child: Text(request.subjectId == null ? tr('طلب ولي الأمر · كل المواد', 'Parent request · all subjects') : tr('طلب ولي الأمر', 'Parent request'),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(color: _orange, fontSize: 11, fontWeight: FontWeight.bold)),
                  ),
                ),
              Icon(e == null ? Icons.add_circle_outline_rounded : Icons.edit_rounded, size: 20, color: palette.faint),
            ],
          ),
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Evaluating one student
// ---------------------------------------------------------------------------

/// Focus, interaction and behavior (1..5), health and a comment for one student in one lesson
class EvaluateStudentScreen extends StatefulWidget {
  const EvaluateStudentScreen({
    super.key,
    this.api = const EvaluationApi(),
    required this.student,
    required this.classLabel,
    required this.lesson,
    required this.date,
    this.existing,
    this.requestId,
    this.requestNote,
    this.completeRequest = true,
    this.onSessionExpired,
  });

  final EvaluationApi api;
  final ClassStudent student;
  final String classLabel;
  final TeacherLesson lesson;
  final DateTime date;

  /// The teacher's saved evaluation (changing it)
  final StudentEvaluation? existing;

  /// The parent's request being answered
  final String? requestId;
  final String? requestNote;

  /// False: one subject of an "all subjects" request that still has other subjects to evaluate
  final bool completeRequest;
  final Future<void> Function()? onSessionExpired;

  @override
  State<EvaluateStudentScreen> createState() => _EvaluateStudentScreenState();
}

class _EvaluateStudentScreenState extends State<EvaluateStudentScreen> {
  late int _focus = widget.existing?.focus ?? 0;
  late int _interaction = widget.existing?.interaction ?? 0;
  late int _behavior = widget.existing?.behavior ?? 0;
  late StudentHealth _health = widget.existing?.health ?? StudentHealth.good;
  late final _comment = TextEditingController(text: widget.existing?.comment ?? '');
  bool _saving = false;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  bool get _complete => _focus > 0 && _interaction > 0 && _behavior > 0;

  Future<void> _save() async {
    if (!_complete) {
      _show(tr('قيّم التركيز والتفاعل والسلوك أولاً.', 'Rate focus, interaction and behavior first.'));
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.api.evaluate(
        widget.lesson,
        widget.date,
        EvaluationInput(
          studentId: widget.student.id,
          focus: _focus,
          interaction: _interaction,
          behavior: _behavior,
          health: _health,
          comment: _comment.text,
          evaluationRequestId: widget.requestId,
          completeRequest: widget.completeRequest,
        ),
      );
      if (mounted) Navigator.of(context).pop(true);
    } on EvaluationException catch (e) {
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
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.existing == null ? tr('تقييم الطالب', 'Evaluate student') : tr('تعديل التقييم', 'Edit evaluation'),
            style: const TextStyle(fontWeight: FontWeight.bold)),
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: FilledButton.icon(
            key: const ValueKey('save-evaluation'),
            onPressed: _saving ? null : _save,
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(50), backgroundColor: _indigo),
            icon: _saving
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.save_rounded),
            label: Text(widget.requestId == null
                ? tr('حفظ التقييم', 'Save evaluation')
                : widget.completeRequest
                    ? tr('حفظ وإرسال لولي الأمر', 'Save and send to the parent')
                    : tr('حفظ هذه المادة', 'Save this subject')),
          ),
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              gradient: const LinearGradient(colors: [Color(0xFF6D7DF2), _indigo]),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(widget.student.name, style: const TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold)),
                const SizedBox(height: 2),
                Text(
                  [widget.classLabel, widget.lesson.subject, attendanceDayLabel(widget.date), lessonTime(widget.lesson.start)]
                      .where((s) => s.isNotEmpty)
                      .join(' · '),
                  style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5),
                ),
              ],
            ),
          ),
          if (widget.requestId != null) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(color: palette.warnTint, borderRadius: BorderRadius.circular(14)),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.mark_email_unread_rounded, color: _orange),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      widget.requestNote == null
                          ? tr('ولي الأمر طلب تقييم الطالب.', 'The parent asked for an evaluation.')
                          : '${tr('ولي الأمر:', 'Parent:')} ${widget.requestNote}',
                      style: TextStyle(color: palette.ink, fontSize: 13),
                    ),
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: 16),
          _ratingCard('focus', Icons.center_focus_strong_rounded, tr('التركيز', 'Focus'), _focus, (v) => setState(() => _focus = v)),
          const SizedBox(height: 10),
          _ratingCard('interaction', Icons.record_voice_over_rounded, tr('التفاعل', 'Interaction'), _interaction,
              (v) => setState(() => _interaction = v)),
          const SizedBox(height: 10),
          _ratingCard('behavior', Icons.emoji_people_rounded, tr('السلوك', 'Behavior'), _behavior, (v) => setState(() => _behavior = v)),
          const SizedBox(height: 16),
          Text(tr('الحالة الصحية', 'Health'), style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold, color: palette.ink)),
          const SizedBox(height: 8),
          Row(
            children: [
              for (final h in StudentHealth.values) ...[
                if (h != StudentHealth.good) const SizedBox(width: 8),
                Expanded(child: _healthButton(h)),
              ],
            ],
          ),
          const SizedBox(height: 16),
          TextField(
            key: const ValueKey('evaluation-comment'),
            controller: _comment,
            maxLength: maxEvaluationCommentLength,
            minLines: 3,
            maxLines: 6,
            decoration: InputDecoration(
              labelText: tr('تعليق لولي الأمر (اختياري)', 'Comment for the parent (optional)'),
              alignLabelWithHint: true,
            ),
          ),
        ],
      ),
    );
  }

  Widget _ratingCard(String key, IconData icon, String label, int value, ValueChanged<int> onChanged) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: value == 0 ? palette.line : _star.withValues(alpha: .6)),
      ),
      child: Row(
        children: [
          Icon(icon, color: _indigo),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                Text(value == 0 ? tr('اختر من 1 إلى 5', 'Choose 1 to 5') : ratingLabel(value),
                    style: TextStyle(fontSize: 11.5, color: value == 0 ? palette.muted : _orange, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
          for (var i = 1; i <= 5; i++)
            InkWell(
              key: ValueKey('rate-$key-$i'),
              customBorder: const CircleBorder(),
              onTap: _saving ? null : () => onChanged(i),
              child: Padding(
                padding: const EdgeInsets.all(2),
                child: Icon(i <= value ? Icons.star_rounded : Icons.star_outline_rounded, size: 30, color: i <= value ? _star : palette.faint),
              ),
            ),
        ],
      ),
    );
  }

  Widget _healthButton(StudentHealth h) {
    final palette = context.palette;
    final selected = _health == h;
    final color = healthColor(h);
    return InkWell(
      key: ValueKey('health-${h.name}'),
      borderRadius: BorderRadius.circular(14),
      onTap: _saving ? null : () => setState(() => _health = h),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(vertical: 10),
        decoration: BoxDecoration(
          color: selected ? color : palette.card,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: selected ? color : palette.line),
        ),
        child: Text(h.label,
            textAlign: TextAlign.center,
            style: TextStyle(fontWeight: FontWeight.bold, color: selected ? Colors.white : color)),
      ),
    );
  }
}

Widget _message(BuildContext context, IconData icon, String text, {VoidCallback? retry}) => Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry != null ? _red : _indigo.withValues(alpha: .6)),
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
