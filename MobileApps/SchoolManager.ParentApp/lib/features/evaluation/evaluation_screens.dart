import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/academic_year_picker.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import 'evaluation_service.dart';

const _indigo = Color(0xFF4C5FD5);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _grey = Color(0xFF8A94A6);
const _star = Color(0xFFF7B924);

List<String> get _monthNames => AppLanguage.instance.isEnglish
    ? const ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
    : const ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];

String _day(DateTime d) => '${d.day} ${_monthNames[d.month - 1]} ${d.year}';

Color requestStatusColor(RequestStatus s) => switch (s) {
      RequestStatus.pending => _orange,
      RequestStatus.evaluated => _green,
      RequestStatus.cancelled => _grey,
    };

/// Which evaluations are shown: all, the ones the parent asked for, or the teacher's own
enum _Source { all, requested, teacher }

/// The API calls of the evaluation pages (tests pass their own)
class EvaluationApi {
  const EvaluationApi({this.loadTeachers, this.loadRequests, this.loadEvaluations, this.create, this.cancel});

  final Future<List<ChildTeacher>> Function(String studentId)? loadTeachers;
  final Future<List<EvaluationRequestItem>> Function(String studentId)? loadRequests;
  final Future<List<ChildEvaluation>> Function(String studentId, DateTime from, DateTime to)? loadEvaluations;
  final Future<void> Function(String studentId, String teacherId, String? subjectId, String? note)? create;
  final Future<void> Function(String requestId)? cancel;

  Future<List<ChildTeacher>> teachers(String studentId) =>
      (loadTeachers ?? EvaluationService.instance.getChildTeachers)(studentId);

  Future<List<EvaluationRequestItem>> requests(String studentId) =>
      (loadRequests ?? EvaluationService.instance.getRequests)(studentId);

  Future<List<ChildEvaluation>> evaluations(String studentId, DateTime from, DateTime to) =>
      (loadEvaluations ?? EvaluationService.instance.getEvaluations)(studentId, from, to);

  Future<void> createRequest(String studentId, String teacherId, String? subjectId, String? note) => create != null
      ? create!(studentId, teacherId, subjectId, note)
      : EvaluationService.instance.createRequest(studentId: studentId, teacherId: teacherId, subjectId: subjectId, note: note);

  Future<void> cancelRequest(String requestId) => (cancel ?? EvaluationService.instance.cancelRequest)(requestId);
}

// ---------------------------------------------------------------------------
// The child's requests
// ---------------------------------------------------------------------------

/// The parent's evaluation requests for the child selected on home: waiting / evaluated (with the teacher's
/// ratings) / cancelled; a new request to one of the child's teachers; cancel a waiting one
class EvaluationRequestsScreen extends StatefulWidget {
  const EvaluationRequestsScreen({super.key, required this.child, this.api = const EvaluationApi(), this.onSessionExpired, this.now});

  final Child child;
  final EvaluationApi api;
  final VoidCallback? onSessionExpired;

  /// Current time (tests pass a fixed one)
  final DateTime? now;

  @override
  State<EvaluationRequestsScreen> createState() => _EvaluationRequestsScreenState();
}

class _EvaluationRequestsScreenState extends State<EvaluationRequestsScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  List<EvaluationRequestItem>? _requests;
  List<ChildTeacher> _teachers = const [];

  /// Every evaluation loaded, by id (the year's ones and the answers of older requests)
  Map<String, ChildEvaluation> _evaluations = const {};

  /// The chosen year's evaluations, newest first
  List<ChildEvaluation> _yearEvaluations = const [];
  _Source _source = _Source.all;
  String? _error;
  bool _loading = true;
  final Set<String> _cancelling = {};

  DateTime get _now => widget.now ?? DateTime.now();

  DateTime get _today => DateTime(_now.year, _now.month, _now.day);

  /// The chosen academic year up to today (the current month when no year is known)
  (DateTime, DateTime) get _range {
    final year = AcademicYearController.instance.value;
    if (year == null) return (DateTime(_today.year, _today.month, 1), _today);
    final end = year.endDate.isAfter(_today) ? _today : year.endDate;
    return (year.startDate, end.isBefore(year.startDate) ? year.startDate : end);
  }

  bool _inRange(DateTime d) {
    final (from, to) = _range;
    return !d.isBefore(from) && !d.isAfter(to);
  }

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _requests == null;
      _error = null;
    });
    try {
      final results = await Future.wait([
        widget.api.requests(widget.child.id),
        widget.api.teachers(widget.child.id),
      ]);
      final requests = results[0] as List<EvaluationRequestItem>;
      final teachers = results[1] as List<ChildTeacher>;

      // Every evaluation of the chosen year, and back to the oldest answered request (to show its answer)
      var (from, to) = _range;
      // Answered and waiting requests (an "all subjects" one can already have some subjects evaluated)
      final evaluated = requests.where((r) => r.status != RequestStatus.cancelled).map((r) => r.requestedAt);
      for (final asked in evaluated) {
        final day = DateTime(asked.year, asked.month, asked.day).subtract(const Duration(days: 1));
        if (day.isBefore(from)) from = day;
      }
      if (to.isBefore(_today) && evaluated.isNotEmpty) to = _today;
      final list = await widget.api.evaluations(widget.child.id, from, to);

      if (!mounted) return;
      setState(() {
        _requests = requests;
        _teachers = teachers;
        _evaluations = {for (final e in list) e.id: e};
        _yearEvaluations = list.where((e) => _inRange(e.date)).toList()..sort((a, b) => b.date.compareTo(a.date));
        _loading = false;
      });
    } on EvaluationException catch (e) {
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

  ChildTeacher? _teacher(String id) => _teachers.where((t) => t.id == id).firstOrNull;

  /// The request's answers: one, or one per subject for an "all subjects" request (by date, then subject)
  List<ChildEvaluation> _answersOf(EvaluationRequestItem r) {
    final answers = {
      for (final e in _evaluations.values)
        if (e.evaluationRequestId == r.id || e.id == r.evaluationId) e.id: e,
    }.values.toList()
      ..sort((a, b) => a.date.compareTo(b.date));
    return answers;
  }

  Future<void> _newRequest() async {
    final pendingTeachers = {
      for (final r in _requests ?? const <EvaluationRequestItem>[])
        if (r.status == RequestStatus.pending) pendingKey(r.teacherId, r.subjectId),
    };
    final sent = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => NewEvaluationRequestScreen(
        child: widget.child,
        teachers: _teachers,
        pendingTeacherIds: pendingTeachers,
        api: widget.api,
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (sent == true && mounted) {
      _show(tr('تم إرسال الطلب إلى المعلم.', 'The request was sent to the teacher.'));
      _load();
    }
  }

  Future<void> _cancel(EvaluationRequestItem request) async {
    final teacher = _teacher(request.teacherId)?.name ?? tr('المعلم', 'the teacher');
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(tr('إلغاء الطلب', 'Cancel the request')),
        content: Text(tr('هل تريد إلغاء طلب التقييم المرسل إلى $teacher؟', 'Cancel the evaluation request sent to $teacher?')),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: Text(tr('رجوع', 'Back'))),
          FilledButton(
            key: const ValueKey('confirm-cancel-request'),
            style: FilledButton.styleFrom(backgroundColor: _red),
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(tr('إلغاء الطلب', 'Cancel request')),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;

    setState(() => _cancelling.add(request.id));
    try {
      await widget.api.cancelRequest(request.id);
      if (!mounted) return;
      _show(tr('تم إلغاء الطلب.', 'The request was cancelled.'));
      await _load();
    } on EvaluationException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        widget.onSessionExpired!();
        return;
      }
      _show(e.message);
    } finally {
      if (mounted) setState(() => _cancelling.remove(request.id));
    }
  }

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  @override
  Widget build(BuildContext context) {
    final yearName = AcademicYearController.instance.value?.name ?? '';
    final pending = (_requests ?? const <EvaluationRequestItem>[]).where((r) => r.status == RequestStatus.pending).length;
    return DefaultTabController(
      length: 2,
      child: Scaffold(
      appBar: AppBar(
        toolbarHeight: yearName.isNotEmpty ? 70 : null,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('تقييم المعلمين', 'Teacher evaluations'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(
              [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
            if (yearName.isNotEmpty)
              Text(yearName,
                  key: const ValueKey('evaluations-year'),
                  style: const TextStyle(fontSize: 11.5, color: Colors.white70, fontWeight: FontWeight.w600)),
          ],
        ),
        actions: [
          // Change the academic year (the page reloads)
          IconButton(
            key: const ValueKey('evaluations-change-year'),
            tooltip: tr('السنة الدراسية', 'Academic year'),
            icon: const Icon(Icons.date_range_rounded),
            onPressed: () => showAcademicYearSheet(context),
          ),
        ],
        bottom: TabBar(
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          indicatorColor: Colors.white,
          indicatorWeight: 3,
          labelStyle: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
          tabs: [
            Tab(key: const ValueKey('tab-evaluations'), text: tr('التقييمات', 'Evaluations')),
            Tab(
              key: const ValueKey('tab-requests'),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Flexible(child: Text(tr('طلباتي', 'My requests'), overflow: TextOverflow.ellipsis)),
                  if (pending > 0) ...[
                    const SizedBox(width: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
                      decoration: BoxDecoration(color: _orange, borderRadius: BorderRadius.circular(10)),
                      child: Text('$pending', style: const TextStyle(color: Colors.white, fontSize: 11.5)),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
      floatingActionButton: _loading || _error != null
          ? null
          : FloatingActionButton.extended(
              key: const ValueKey('new-evaluation-request'),
              onPressed: _newRequest,
              icon: const Icon(Icons.rate_review_rounded),
              label: Text(tr('طلب تقييم', 'Request evaluation')),
            ),
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _message(Icons.error_outline_rounded, _error!, retry: true)
                : TabBarView(children: [
                    RefreshIndicator(onRefresh: _load, child: _evaluationsTab()),
                    RefreshIndicator(onRefresh: _load, child: _content(_requests!)),
                  ]),
      ),
      ),
    );
  }

  /// Every evaluation of the year: asked for by the parent, or made by the teacher on their own
  Widget _evaluationsTab() {
    final palette = context.palette;
    final all = _yearEvaluations;
    final requested = all.where(_wasRequested).length;
    final shown = all.where((e) => switch (_source) {
          _Source.all => true,
          _Source.requested => _wasRequested(e),
          _Source.teacher => !_wasRequested(e),
        }).toList();
    double avg(int Function(ChildEvaluation e) pick) => all.isEmpty ? 0 : all.fold<int>(0, (s, e) => s + pick(e)) / all.length;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 96),
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [Color(0xFF6D7DF2), _indigo]),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(tr('تقييمات ${widget.child.name} هذه السنة', '${widget.child.name}\'s evaluations this year'),
                  style: const TextStyle(color: Colors.white, fontSize: 15.5, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              Row(
                children: [
                  _stat('${all.length}', tr('تقييم', 'Evaluations')),
                  _stat(all.isEmpty ? '—' : avg((e) => e.focus).toStringAsFixed(1), tr('التركيز', 'Focus')),
                  _stat(all.isEmpty ? '—' : avg((e) => e.interaction).toStringAsFixed(1), tr('التفاعل', 'Interaction')),
                  _stat(all.isEmpty ? '—' : avg((e) => e.behavior).toStringAsFixed(1), tr('السلوك', 'Behavior')),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        if (all.isNotEmpty) ...[
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _sourceChip(_Source.all, tr('الكل', 'All'), all.length),
              _sourceChip(_Source.requested, tr('بطلب مني', 'I requested'), requested),
              _sourceChip(_Source.teacher, tr('من المعلم', 'By the teacher'), all.length - requested),
            ],
          ),
          const SizedBox(height: 12),
        ],
        if (all.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: _message(Icons.star_outline_rounded,
                tr('لا توجد تقييمات لـ ${widget.child.name} في هذه السنة الدراسية بعد.', 'No evaluations for ${widget.child.name} in this academic year yet.')),
          )
        else if (shown.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: Text(tr('لا توجد تقييمات من هذا النوع.', 'No evaluations of this kind.'),
                textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
          )
        else
          for (final e in shown) ...[
            _evaluationCard(e),
            const SizedBox(height: 10),
          ],
      ],
    );
  }

  /// Made for one of the parent's requests
  bool _wasRequested(ChildEvaluation e) =>
      e.evaluationRequestId != null ||
      (_requests ?? const <EvaluationRequestItem>[]).any((r) => r.evaluationId == e.id);

  Widget _sourceChip(_Source source, String label, int count) {
    final palette = context.palette;
    final selected = _source == source;
    return InkWell(
      key: ValueKey('evaluations-filter-${source.name}'),
      borderRadius: BorderRadius.circular(20),
      onTap: () => setState(() => _source = source),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
        decoration: BoxDecoration(
          color: selected ? _indigo : palette.card,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: selected ? _indigo : palette.line),
        ),
        child: Text('$label ($count)',
            style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: selected ? Colors.white : palette.ink)),
      ),
    );
  }

  Widget _evaluationCard(ChildEvaluation e) {
    final palette = context.palette;
    final teacher = _teacher(e.teacherId);
    final requested = _wasRequested(e);
    final tagColor = requested ? _orange : _indigo;

    return Container(
      key: ValueKey('evaluation-${e.id}'),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: palette.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: _indigo.withValues(alpha: .12),
                child: const Icon(Icons.person_rounded, color: _indigo),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(teacher?.name ?? tr('معلم', 'Teacher'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                    Text(
                      [teacher?.subjectName(e.subjectId), _day(e.date)].whereType<String>().where((s) => s.isNotEmpty).join(' · '),
                      style: TextStyle(fontSize: 12, color: palette.muted),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(color: tagColor.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                child: Text(requested ? tr('بطلب منك', 'You asked') : tr('من المعلم', 'By the teacher'),
                    style: TextStyle(color: tagColor, fontSize: 11, fontWeight: FontWeight.bold)),
              ),
            ],
          ),
          const SizedBox(height: 10),
          _evaluationBox(e, showDate: false),
        ],
      ),
    );
  }

  Widget _content(List<EvaluationRequestItem> requests) {
    final palette = context.palette;
    final pending = requests.where((r) => r.status == RequestStatus.pending).length;
    final evaluated = requests.where((r) => r.status == RequestStatus.evaluated).length;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 96),
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [Color(0xFF6D7DF2), _indigo]),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(tr('اطلب من معلمي ${widget.child.name} تقييم مستواه', 'Ask ${widget.child.name}\'s teachers how they are doing'),
                  style: const TextStyle(color: Colors.white, fontSize: 15.5, fontWeight: FontWeight.bold)),
              const SizedBox(height: 4),
              Text(
                tr('يقيّم المعلم التركيز والتفاعل والسلوك والحالة الصحية في الحصة.',
                    'The teacher rates focus, interaction, behavior and health in class.'),
                style: TextStyle(color: Colors.white.withValues(alpha: .9), fontSize: 12.5),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  _stat('${requests.length}', tr('الطلبات', 'Requests')),
                  _stat('$pending', tr('بالانتظار', 'Waiting')),
                  _stat('$evaluated', tr('تم التقييم', 'Evaluated')),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        if (requests.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: _message(
              Icons.rate_review_outlined,
              _teachers.isEmpty
                  ? tr('لا يوجد معلمون مسجلون لهذا الطالب بعد.', 'No teachers are registered for this student yet.')
                  : tr('لم ترسل أي طلب تقييم بعد.\nاضغط "طلب تقييم" لاختيار المعلم.', 'You have not sent any request yet.\nTap "Request evaluation" to choose a teacher.'),
            ),
          )
        else
          for (final r in requests) ...[
            _requestCard(r),
            const SizedBox(height: 10),
          ],
        if (requests.isNotEmpty && _teachers.isEmpty)
          Text(tr('لا يوجد معلمون لإرسال طلب جديد.', 'There are no teachers to send a new request to.'),
              textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
      ],
    );
  }

  Widget _stat(String value, String label) => Expanded(
        child: Column(
          children: [
            Text(value, style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
            Text(label, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 11)),
          ],
        ),
      );

  Widget _requestCard(EvaluationRequestItem r) {
    final palette = context.palette;
    final teacher = _teacher(r.teacherId);
    final color = requestStatusColor(r.status);
    final subject = teacher?.subjectName(r.subjectId);
    final answers = _answersOf(r);
    final cancelling = _cancelling.contains(r.id);

    return Container(
      key: ValueKey('request-${r.id}'),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: r.status == RequestStatus.cancelled ? palette.line : color.withValues(alpha: .45)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: _indigo.withValues(alpha: .12),
                child: const Icon(Icons.person_rounded, color: _indigo),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(teacher?.name ?? tr('معلم', 'Teacher'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                    Text(
                      [subject ?? (r.subjectId == null ? tr('كل المواد', 'All subjects') : null), _day(r.requestedAt)]
                          .whereType<String>()
                          .join(' · '),
                      style: TextStyle(fontSize: 12, color: palette.muted),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                child: Text(r.status.label, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold)),
              ),
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
          // An "all subjects" request still waiting: the subjects the teacher already evaluated
          if (r.status == RequestStatus.pending && answers.isNotEmpty) ...[
            const SizedBox(height: 10),
            Text(tr('قيّم المعلم حتى الآن (${answers.length}):', 'Evaluated so far (${answers.length}):'),
                style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600, color: palette.muted)),
            for (final e in answers) ...[
              const SizedBox(height: 6),
              _answerBox(teacher, e, many: true),
            ],
          ],
          if (r.status == RequestStatus.evaluated) ...[
            const SizedBox(height: 10),
            if (answers.isNotEmpty)
              for (final e in answers) ...[
                if (e != answers.first) const SizedBox(height: 6),
                _answerBox(teacher, e, many: answers.length > 1 || r.subjectId == null),
              ]
            else
              Text(
                r.evaluatedAt == null
                    ? tr('قيّم المعلم الطالب.', 'The teacher evaluated the student.')
                    : tr('قيّم المعلم الطالب في ${_day(r.evaluatedAt!)}.', 'The teacher evaluated the student on ${_day(r.evaluatedAt!)}.'),
                style: TextStyle(fontSize: 12.5, color: palette.muted),
              ),
          ],
          if (r.status == RequestStatus.pending) ...[
            const SizedBox(height: 8),
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: TextButton.icon(
                key: ValueKey('cancel-request-${r.id}'),
                onPressed: cancelling ? null : () => _cancel(r),
                style: TextButton.styleFrom(foregroundColor: _red),
                icon: cancelling
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.close_rounded, size: 18),
                label: Text(tr('إلغاء الطلب', 'Cancel request')),
              ),
            ),
          ],
        ],
      ),
    );
  }

  /// An answer of a request; with the subject's name when the request has several (all subjects)
  Widget _answerBox(ChildTeacher? teacher, ChildEvaluation e, {required bool many}) {
    if (!many) return _evaluationBox(e);
    final palette = context.palette;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          [teacher?.subjectName(e.subjectId) ?? tr('مادة', 'Subject'), _day(e.date)].join(' · '),
          style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: palette.ink),
        ),
        const SizedBox(height: 4),
        _evaluationBox(e, showDate: false),
      ],
    );
  }

  Widget _evaluationBox(ChildEvaluation e, {bool showDate = true}) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: _green.withValues(alpha: .06), borderRadius: BorderRadius.circular(14)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (showDate) ...[
            Text(tr('تقييم ${_day(e.date)}', 'Evaluation of ${_day(e.date)}'),
                style: TextStyle(fontSize: 12.5, fontWeight: FontWeight.bold, color: palette.ink)),
            const SizedBox(height: 6),
          ],
          _ratingRow(tr('التركيز', 'Focus'), e.focus),
          _ratingRow(tr('التفاعل', 'Interaction'), e.interaction),
          _ratingRow(tr('السلوك', 'Behavior'), e.behavior),
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 3),
            child: Row(
              children: [
                SizedBox(width: 90, child: Text(tr('الحالة الصحية', 'Health'), style: TextStyle(fontSize: 12.5, color: palette.muted))),
                Text(e.health.label,
                    style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.bold,
                        color: e.health == ChildHealth.good ? _green : e.health == ChildHealth.tired ? _orange : _red)),
              ],
            ),
          ),
          if (e.comment != null) ...[
            const SizedBox(height: 6),
            Text(e.comment!, style: TextStyle(fontSize: 13, color: palette.ink)),
          ],
        ],
      ),
    );
  }

  Widget _ratingRow(String label, int rating) {
    final palette = context.palette;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          SizedBox(width: 90, child: Text(label, style: TextStyle(fontSize: 12.5, color: palette.muted))),
          for (var i = 1; i <= 5; i++)
            Icon(i <= rating ? Icons.star_rounded : Icons.star_outline_rounded, size: 18, color: i <= rating ? _star : palette.faint),
          const SizedBox(width: 6),
          Text(ratingLabel(rating), style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: palette.ink)),
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
              Icon(icon, size: 56, color: retry ? _red : _indigo.withValues(alpha: .6)),
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
// New request
// ---------------------------------------------------------------------------

/// Key of a waiting request in [NewEvaluationRequestScreen.pendingTeacherIds]: teacher and subject
String pendingKey(String teacherId, String? subjectId) => '$teacherId|${subjectId ?? ''}';

/// Choose one of the child's teachers and one of the subjects they teach the child, add a note, send
class NewEvaluationRequestScreen extends StatefulWidget {
  const NewEvaluationRequestScreen({
    super.key,
    required this.child,
    required this.teachers,
    this.pendingTeacherIds = const {},
    this.api = const EvaluationApi(),
    this.onSessionExpired,
  });

  final Child child;
  final List<ChildTeacher> teachers;

  /// Waiting requests as [pendingKey] (teacher + subject): one at a time per teacher and subject
  final Set<String> pendingTeacherIds;
  final EvaluationApi api;
  final VoidCallback? onSessionExpired;

  @override
  State<NewEvaluationRequestScreen> createState() => _NewEvaluationRequestScreenState();
}

class _NewEvaluationRequestScreenState extends State<NewEvaluationRequestScreen> {
  String? _teacherId;
  String? _subjectId;
  final _note = TextEditingController();
  bool _sending = false;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  ChildTeacher? get _teacher => widget.teachers.where((t) => t.id == _teacherId).firstOrNull;

  /// The teacher already has a waiting request about this subject
  bool _isPending(String teacherId, String subjectId) => widget.pendingTeacherIds.contains(pendingKey(teacherId, subjectId));

  /// Subjects of the teacher that can be asked about (no waiting request yet)
  List<TeacherSubjectRef> _free(ChildTeacher t) => t.subjects.where((s) => !_isPending(t.id, s.id)).toList();

  void _pickTeacher(ChildTeacher t) => setState(() {
        _teacherId = t.id;
        // Only one subject left to ask about: it is the one
        final free = _free(t);
        _subjectId = free.length == 1 ? free.first.id : null;
      });

  Future<void> _send() async {
    final teacher = _teacher;
    if (teacher == null) {
      _show(tr('اختر المعلم أولاً.', 'Choose the teacher first.'));
      return;
    }
    if (_subjectId == null) {
      _show(tr('اختر المادة.', 'Choose the subject.'));
      return;
    }
    setState(() => _sending = true);
    try {
      await widget.api.createRequest(widget.child.id, teacher.id, _subjectId, _note.text);
      if (mounted) Navigator.of(context).pop(true);
    } on EvaluationException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        widget.onSessionExpired!();
        return;
      }
      _show(e.message);
    } finally {
      if (mounted) setState(() => _sending = false);
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
    final teacher = _teacher;

    return Scaffold(
      appBar: AppBar(title: Text(tr('طلب تقييم', 'Request evaluation'), style: const TextStyle(fontWeight: FontWeight.bold))),
      bottomNavigationBar: widget.teachers.isEmpty
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: FilledButton.icon(
                  key: const ValueKey('send-evaluation-request'),
                  onPressed: _sending || teacher == null || _subjectId == null ? null : _send,
                  style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(50)),
                  icon: _sending
                      ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.send_rounded),
                  label: Text(tr('إرسال الطلب', 'Send request')),
                ),
              ),
            ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
        children: [
          _sectionTitle(tr('المعلم', 'Teacher')),
          if (widget.teachers.isEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 20),
              child: Text(tr('لا يوجد معلمون مسجلون لهذا الطالب بعد.', 'No teachers are registered for this student yet.'),
                  textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
            )
          else
            for (final t in widget.teachers) ...[
              _teacherTile(t),
              const SizedBox(height: 8),
            ],
          if (teacher != null) ...[
            const SizedBox(height: 8),
            _sectionTitle(tr('المادة', 'Subject')),
            if (teacher.subjects.isEmpty)
              Text(tr('لا توجد مواد مسجلة لهذا المعلم مع ابنك.', 'No subjects are registered for this teacher with your child.'),
                  style: TextStyle(color: palette.muted))
            else
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  for (final s in teacher.subjects)
                    _subjectChip(s.id, s.name, pending: _isPending(teacher.id, s.id)),
                ],
              ),
          ],
          if (teacher != null) ...[
            const SizedBox(height: 16),
            _sectionTitle(tr('ملاحظة للمعلم (اختياري)', 'Note for the teacher (optional)')),
            TextField(
              key: const ValueKey('evaluation-request-note'),
              controller: _note,
              maxLength: maxRequestNoteLength,
              minLines: 3,
              maxLines: 6,
              decoration: InputDecoration(
                hintText: tr('مثال: كيف مستواه في الحصة مؤخراً؟', 'e.g. How has he been doing in class lately?'),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _sectionTitle(String text) => Padding(
        padding: const EdgeInsets.fromLTRB(4, 0, 4, 8),
        child: Text(text, style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold, color: context.palette.ink)),
      );

  Widget _teacherTile(ChildTeacher t) {
    final palette = context.palette;
    final pending = t.subjects.isNotEmpty && _free(t).isEmpty;
    final selected = t.id == _teacherId;

    return Material(
      color: selected ? _indigo.withValues(alpha: .08) : palette.card,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        key: ValueKey('teacher-${t.id}'),
        borderRadius: BorderRadius.circular(16),
        onTap: pending || _sending ? null : () => _pickTeacher(t),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: selected ? _indigo : palette.line, width: selected ? 1.5 : 1),
          ),
          child: Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: _indigo.withValues(alpha: pending ? .05 : .12),
                child: Icon(Icons.person_rounded, color: pending ? palette.faint : _indigo),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(t.name, style: TextStyle(fontWeight: FontWeight.bold, color: pending ? palette.muted : palette.ink)),
                    if (t.subjectsText.isNotEmpty) Text(t.subjectsText, style: TextStyle(fontSize: 12, color: palette.muted)),
                    if (pending)
                      Text(tr('لديك طلب بانتظار هذا المعلم في كل مواده', 'You have a waiting request for each of this teacher\'s subjects'),
                          style: const TextStyle(fontSize: 11.5, color: _orange, fontWeight: FontWeight.w600)),
                  ],
                ),
              ),
              Icon(selected ? Icons.radio_button_checked_rounded : Icons.radio_button_off_rounded,
                  color: selected ? _indigo : palette.faint),
            ],
          ),
        ),
      ),
    );
  }

  Widget _subjectChip(String id, String label, {bool pending = false}) {
    final selected = _subjectId == id;
    return ChoiceChip(
      key: ValueKey('subject-$id'),
      // A subject with a waiting request cannot be asked about again
      label: Text(pending ? '$label · ${tr('بالانتظار', 'waiting')}' : label),
      selected: selected,
      onSelected: _sending || pending ? null : (_) => setState(() => _subjectId = id),
      selectedColor: _indigo.withValues(alpha: .15),
      labelStyle: TextStyle(fontWeight: FontWeight.w600, color: selected ? _indigo : context.palette.ink),
    );
  }
}
