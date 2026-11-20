import 'package:flutter/material.dart';

import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'homework_service.dart';

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _purple = Color(0xFF6A3FD8);

/// "الخميس 20 نوفمبر" / "Thursday 20 November"
String homeworkDate(DateTime d) {
  const arDays = ['الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت', 'الأحد'];
  const enDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  const arMonths = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
  const enMonths = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  final en = AppLanguage.instance.isEnglish;
  return '${(en ? enDays : arDays)[d.weekday - 1]} ${d.day} ${(en ? enMonths : arMonths)[d.month - 1]}';
}

/// The API calls the screens use (tests pass their own)
class HomeworkApi {
  const HomeworkApi({
    this.loadClasses,
    this.loadHomeworks,
    this.create,
    this.publish,
    this.delete,
  });

  final Future<TeacherClasses> Function()? loadClasses;
  final Future<List<TeacherHomework>> Function()? loadHomeworks;
  final Future<String> Function({
    required String subjectId,
    required String gradeId,
    required String title,
    required String description,
    required DateTime dueDate,
    List<HomeworkQuestion> questions,
  })? create;
  final Future<int> Function(String homeworkId, List<String> sectionIds)? publish;
  final Future<void> Function(String homeworkId)? delete;

  Future<TeacherClasses> classes() => (loadClasses ?? HomeworkService.instance.getMyClasses)();
  Future<List<TeacherHomework>> homeworks() => (loadHomeworks ?? HomeworkService.instance.getMyHomeworks)();
  Future<String> createHomework({
    required String subjectId,
    required String gradeId,
    required String title,
    required String description,
    required DateTime dueDate,
    List<HomeworkQuestion> questions = const [],
  }) =>
      (create ?? HomeworkService.instance.createHomework)(
          subjectId: subjectId, gradeId: gradeId, title: title, description: description, dueDate: dueDate, questions: questions);
  Future<int> publishTo(String homeworkId, List<String> sectionIds) =>
      (publish ?? HomeworkService.instance.publish)(homeworkId, sectionIds);
  Future<void> deleteHomework(String homeworkId) => (delete ?? HomeworkService.instance.delete)(homeworkId);
}

enum _Filter { all, draft, published }

/// The teacher's homework: filter, publish to classes, delete, add new
class HomeworkListScreen extends StatefulWidget {
  const HomeworkListScreen({super.key, this.api = const HomeworkApi(), this.onSessionExpired});

  final HomeworkApi api;
  final Future<void> Function()? onSessionExpired;

  @override
  State<HomeworkListScreen> createState() => _HomeworkListScreenState();
}

class _HomeworkListScreenState extends State<HomeworkListScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  List<TeacherHomework> _homeworks = const [];
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
      _loading = _homeworks.isEmpty;
      _error = null;
    });
    try {
      final results = await Future.wait([widget.api.homeworks(), widget.api.classes()]);
      if (!mounted) return;
      setState(() {
        _homeworks = results[0] as List<TeacherHomework>;
        _classes = results[1] as TeacherClasses;
        _loading = false;
      });
    } on HomeworkException catch (e) {
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

  Future<void> _openForm() async {
    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => HomeworkFormScreen(api: widget.api, classes: _classes),
    ));
    if (saved == true) await _load();
  }

  Future<void> _publish(TeacherHomework homework) async {
    final subject = _classes.subjects
        .where((s) => s.subjectId == homework.subjectId && s.gradeId == homework.gradeId)
        .firstOrNull;
    final sections = subject?.sections ?? const <TeacherSection>[];
    if (sections.isEmpty) {
      _show(tr('لا توجد فصول لهذه المادة.', 'There are no classes for this subject.'));
      return;
    }
    final already = homework.publishedSections.map((p) => p.sectionId).toSet();
    final chosen = await showModalBottomSheet<List<String>>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (_) => _PublishSheet(homework: homework, sections: sections, already: already),
    );
    if (chosen == null || chosen.isEmpty || !mounted) return;

    setState(() => _busyId = homework.id);
    try {
      final added = await widget.api.publishTo(homework.id, chosen);
      if (!mounted) return;
      _show(added == 0
          ? tr('الواجب منشور بالفعل لهذه الفصول.', 'The homework is already published to these classes.')
          : tr('تم نشر الواجب لـ $added فصل، يراه أولياء الأمور الآن.',
              'Published to $added ${added == 1 ? 'class' : 'classes'}; parents can see it now.'));
      await _load();
    } on HomeworkException catch (e) {
      if (mounted) _show(e.message);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  Future<void> _delete(TeacherHomework homework) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(tr('حذف الواجب', 'Delete homework')),
        content: Text(homework.isPublished
            ? tr('هذا الواجب منشور، سيختفي من تطبيق ولي الأمر أيضاً. هل تريد حذفه؟',
                'This homework is published; it will also disappear from the parent app. Delete it?')
            : tr('هل تريد حذف هذا الواجب؟', 'Delete this homework?')),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: Text(tr('إلغاء', 'Cancel'))),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: _red),
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(tr('حذف', 'Delete')),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;

    setState(() => _busyId = homework.id);
    try {
      await widget.api.deleteHomework(homework.id);
      if (!mounted) return;
      _show(tr('تم حذف الواجب.', 'Homework deleted.'));
      await _load();
    } on HomeworkException catch (e) {
      if (mounted) _show(e.message);
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final list = _homeworks.where((h) => switch (_filter) {
          _Filter.all => true,
          _Filter.draft => !h.isPublished,
          _Filter.published => h.isPublished,
        }).toList();

    return Scaffold(
      appBar: AppBar(title: Text(tr('الواجبات', 'Homework'), style: const TextStyle(fontWeight: FontWeight.bold))),
      floatingActionButton: _loading || _error != null
          ? null
          : FloatingActionButton.extended(
              key: const ValueKey('new-homework'),
              onPressed: _openForm,
              icon: const Icon(Icons.add_rounded),
              label: Text(tr('واجب جديد', 'New homework')),
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
                        children: [
                          _filterChip(_Filter.all, tr('الكل', 'All'), _homeworks.length),
                          _filterChip(_Filter.draft, tr('غير منشورة', 'Not published'),
                              _homeworks.where((h) => !h.isPublished).length),
                          _filterChip(_Filter.published, tr('منشورة', 'Published'),
                              _homeworks.where((h) => h.isPublished).length),
                        ],
                      ),
                      const SizedBox(height: 12),
                      if (list.isEmpty)
                        Padding(
                          padding: const EdgeInsets.only(top: 60),
                          child: _message(Icons.menu_book_rounded,
                              _homeworks.isEmpty
                                  ? tr('لا توجد واجبات بعد. اضغط "واجب جديد" لإضافة أول واجب.',
                                      'No homework yet. Tap "New homework" to add the first one.')
                                  : tr('لا توجد واجبات هنا.', 'No homework here.')),
                        )
                      else
                        for (final h in list) ...[
                          _HomeworkCard(
                            homework: h,
                            busy: _busyId == h.id,
                            onPublish: () => _publish(h),
                            onDelete: () => _delete(h),
                            onTap: () => showModalBottomSheet<void>(
                              context: context,
                              showDragHandle: true,
                              isScrollControlled: true,
                              builder: (_) => _HomeworkDetails(homework: h),
                            ),
                          ),
                          const SizedBox(height: 12),
                        ],
                    ],
                  ),
                ),
    );
  }

  Widget _filterChip(_Filter value, String label, int count) => ChoiceChip(
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

/// One homework: subject, title, due date, where it is published, actions
class _HomeworkCard extends StatelessWidget {
  const _HomeworkCard({
    required this.homework,
    required this.busy,
    required this.onPublish,
    required this.onDelete,
    required this.onTap,
  });

  final TeacherHomework homework;
  final bool busy;
  final VoidCallback onPublish;
  final VoidCallback onDelete;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final overdue = homework.isOverdue(DateTime.now());
    final accent = homework.isPublished ? _green : _orange;

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(20),
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(20),
            border: BorderDirectional(start: BorderSide(color: accent, width: 4)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Flexible(
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                      decoration: BoxDecoration(color: _purple.withValues(alpha: .1), borderRadius: BorderRadius.circular(20)),
                      child: Text(
                        [homework.subjectName, homework.gradeName].where((s) => s.isNotEmpty).join(' · '),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(color: _purple, fontSize: 11.5, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                    decoration: BoxDecoration(color: accent.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                    child: Text(homework.isPublished ? tr('منشور', 'Published') : tr('غير منشور', 'Not published'),
                        style: TextStyle(color: accent, fontSize: 11, fontWeight: FontWeight.bold)),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Text(homework.title, style: TextStyle(fontSize: 16.5, fontWeight: FontWeight.bold, color: palette.ink)),
              if (homework.description.isNotEmpty) ...[
                const SizedBox(height: 4),
                Text(homework.description,
                    maxLines: 2, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 13, color: palette.muted)),
              ],
              const SizedBox(height: 10),
              Wrap(
                spacing: 14,
                runSpacing: 6,
                children: [
                  _meta(Icons.event_rounded, '${tr('التسليم', 'Due')}: ${homeworkDate(homework.dueDate)}',
                      overdue ? _red : palette.muted),
                  if (homework.questions.isNotEmpty)
                    _meta(Icons.help_outline_rounded,
                        tr('${homework.questions.length} أسئلة', '${homework.questions.length} questions'), palette.muted),
                ],
              ),
              if (homework.isPublished) ...[
                const SizedBox(height: 10),
                Wrap(
                  spacing: 6,
                  runSpacing: 6,
                  children: [
                    for (final s in homework.publishedSections)
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                        decoration: BoxDecoration(color: palette.chip, borderRadius: BorderRadius.circular(20)),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.groups_rounded, size: 13, color: _green),
                            const SizedBox(width: 4),
                            Text(s.name.isEmpty ? tr('فصل', 'Class') : s.name,
                                style: TextStyle(fontSize: 11.5, color: palette.ink, fontWeight: FontWeight.w600)),
                          ],
                        ),
                      ),
                  ],
                ),
              ],
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: FilledButton.tonalIcon(
                      onPressed: busy ? null : onPublish,
                      icon: busy
                          ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.send_rounded, size: 18),
                      label: Text(homework.isPublished ? tr('نشر لفصول أخرى', 'Publish to more') : tr('نشر', 'Publish')),
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton(
                    tooltip: tr('حذف', 'Delete'),
                    onPressed: busy ? null : onDelete,
                    icon: const Icon(Icons.delete_outline_rounded, color: _red),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _meta(IconData icon, String text, Color color) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: color),
          const SizedBox(width: 4),
          Text(text, style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.w600)),
        ],
      );
}

/// Choose the classes to publish to (the ones already published are ticked and locked)
class _PublishSheet extends StatefulWidget {
  const _PublishSheet({required this.homework, required this.sections, required this.already});

  final TeacherHomework homework;
  final List<TeacherSection> sections;
  final Set<String> already;

  @override
  State<_PublishSheet> createState() => _PublishSheetState();
}

class _PublishSheetState extends State<_PublishSheet> {
  final _chosen = <String>{};

  @override
  Widget build(BuildContext context) {
    final open = widget.sections.where((s) => !widget.already.contains(s.id)).toList();
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(18, 0, 18, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('نشر الواجب', 'Publish homework'),
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: context.palette.ink)),
            const SizedBox(height: 4),
            Text(tr('اختر الفصول، وسيظهر الواجب لأولياء أمور طلابها.',
                    'Choose the classes; the parents of their students will see the homework.'),
                style: TextStyle(fontSize: 13, color: context.palette.muted)),
            const SizedBox(height: 14),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final s in widget.sections)
                  FilterChip(
                    label: Text(s.name),
                    selected: widget.already.contains(s.id) || _chosen.contains(s.id),
                    onSelected: widget.already.contains(s.id)
                        ? null
                        : (v) => setState(() => v ? _chosen.add(s.id) : _chosen.remove(s.id)),
                  ),
              ],
            ),
            if (open.length > 1) ...[
              const SizedBox(height: 6),
              TextButton(
                onPressed: () => setState(() => _chosen
                  ..clear()
                  ..addAll(open.map((s) => s.id))),
                child: Text(tr('اختيار كل الفصول', 'Choose all classes')),
              ),
            ],
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                key: const ValueKey('confirm-publish'),
                onPressed: _chosen.isEmpty ? null : () => Navigator.of(context).pop(_chosen.toList()),
                icon: const Icon(Icons.send_rounded),
                label: Text(tr('نشر (${_chosen.length})', 'Publish (${_chosen.length})')),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Full homework: description and questions
class _HomeworkDetails extends StatelessWidget {
  const _HomeworkDetails({required this.homework});

  final TeacherHomework homework;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(18, 0, 18, 18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(homework.title, style: TextStyle(fontSize: 19, fontWeight: FontWeight.bold, color: palette.ink)),
            const SizedBox(height: 4),
            Text('${[homework.subjectName, homework.gradeName].where((s) => s.isNotEmpty).join(' · ')}  •  '
                '${tr('التسليم', 'Due')}: ${homeworkDate(homework.dueDate)}',
                style: TextStyle(fontSize: 12.5, color: palette.muted)),
            if (homework.description.isNotEmpty) ...[
              const SizedBox(height: 14),
              Text(homework.description, style: TextStyle(fontSize: 14.5, height: 1.6, color: palette.ink)),
            ],
            for (var i = 0; i < homework.questions.length; i++) ...[
              const SizedBox(height: 14),
              Text('${i + 1}. ${homework.questions[i].header}',
                  style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold, color: palette.ink)),
              for (final item in homework.questions[i].items)
                Padding(
                  padding: const EdgeInsetsDirectional.only(start: 16, top: 4),
                  child: Text('• $item', style: TextStyle(fontSize: 13.5, color: palette.ink)),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Add a homework: subject, title, description, due date, questions; optionally publish right away
class HomeworkFormScreen extends StatefulWidget {
  const HomeworkFormScreen({super.key, required this.api, required this.classes});

  final HomeworkApi api;
  final TeacherClasses classes;

  @override
  State<HomeworkFormScreen> createState() => _HomeworkFormScreenState();
}

class _QuestionFields {
  final header = TextEditingController();
  final items = TextEditingController();

  void dispose() {
    header.dispose();
    items.dispose();
  }
}

class _HomeworkFormScreenState extends State<HomeworkFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _title = TextEditingController();
  final _description = TextEditingController();
  final _questions = <_QuestionFields>[];
  TeacherSubject? _subject;
  late DateTime _dueDate = DateTime.now().add(const Duration(days: 1));
  final _publishTo = <String>{};
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    if (widget.classes.subjects.length == 1) _subject = widget.classes.subjects.first;
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    for (final q in _questions) {
      q.dispose();
    }
    super.dispose();
  }

  Future<void> _pickDate() async {
    final today = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dueDate,
      firstDate: DateTime(today.year, today.month, today.day),
      lastDate: today.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _dueDate = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate() || _subject == null) return;
    setState(() => _saving = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      final questions = _questions
          .where((q) => q.header.text.trim().isNotEmpty)
          .map((q) => HomeworkQuestion(
                header: q.header.text.trim(),
                items: q.items.text.split('\n').map((s) => s.trim()).where((s) => s.isNotEmpty).toList(),
              ))
          .toList();
      final id = await widget.api.createHomework(
        subjectId: _subject!.subjectId,
        gradeId: _subject!.gradeId,
        title: _title.text.trim(),
        description: _description.text.trim(),
        dueDate: _dueDate,
        questions: questions,
      );
      if (_publishTo.isNotEmpty && id.isNotEmpty) {
        await widget.api.publishTo(id, _publishTo.toList());
      }
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(
          content: Text(_publishTo.isEmpty
              ? tr('تم حفظ الواجب.', 'Homework saved.')
              : tr('تم حفظ الواجب ونشره.', 'Homework saved and published.'))));
      Navigator.of(context).pop(true);
    } on HomeworkException catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(content: Text(e.message)));
      setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final subjects = widget.classes.subjects;
    final palette = context.palette;

    return Scaffold(
      appBar: AppBar(title: Text(tr('واجب جديد', 'New homework'), style: const TextStyle(fontWeight: FontWeight.bold))),
      body: subjects.isEmpty || !widget.classes.hasActiveSemester
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  subjects.isEmpty
                      ? tr('لا توجد مواد مرتبطة بك بعد، تواصل مع إدارة المدرسة.',
                          'No subjects are linked to you yet. Please contact the school office.')
                      : tr('لا يوجد فصل دراسي حالي، تواصل مع إدارة المدرسة.',
                          'There is no current semester. Please contact the school office.'),
                  textAlign: TextAlign.center,
                  style: TextStyle(color: palette.ink),
                ),
              ),
            )
          : Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
                children: [
                  DropdownButtonFormField<TeacherSubject>(
                    key: const ValueKey('homework-subject'),
                    initialValue: _subject,
                    isExpanded: true,
                    decoration: _decoration(tr('المادة والصف', 'Subject and grade'), Icons.menu_book_rounded),
                    items: [
                      for (final s in subjects) DropdownMenuItem(value: s, child: Text(s.label, overflow: TextOverflow.ellipsis)),
                    ],
                    validator: (v) => v == null ? tr('اختر المادة', 'Choose the subject') : null,
                    onChanged: (v) => setState(() {
                      _subject = v;
                      _publishTo.clear();
                    }),
                  ),
                  const SizedBox(height: 14),
                  TextFormField(
                    key: const ValueKey('homework-title'),
                    controller: _title,
                    maxLength: 200,
                    decoration: _decoration(tr('عنوان الواجب', 'Homework title'), Icons.title_rounded),
                    validator: (v) => (v ?? '').trim().isEmpty ? tr('من فضلك أدخل عنوان الواجب', 'Please enter the title') : null,
                  ),
                  const SizedBox(height: 6),
                  TextFormField(
                    key: const ValueKey('homework-description'),
                    controller: _description,
                    maxLength: 2000,
                    minLines: 3,
                    maxLines: 6,
                    decoration: _decoration(tr('الوصف والتعليمات', 'Description and instructions'), Icons.notes_rounded),
                  ),
                  const SizedBox(height: 6),
                  InkWell(
                    borderRadius: BorderRadius.circular(14),
                    onTap: _pickDate,
                    child: InputDecorator(
                      decoration: _decoration(tr('موعد التسليم', 'Due date'), Icons.event_rounded),
                      child: Text(homeworkDate(_dueDate), style: TextStyle(color: palette.ink, fontWeight: FontWeight.w600)),
                    ),
                  ),
                  const SizedBox(height: 20),
                  Row(
                    children: [
                      Expanded(
                        child: Text(tr('الأسئلة (اختياري)', 'Questions (optional)'),
                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: palette.ink)),
                      ),
                      TextButton.icon(
                        key: const ValueKey('add-question'),
                        onPressed: _questions.length >= 30 ? null : () => setState(() => _questions.add(_QuestionFields())),
                        icon: const Icon(Icons.add_rounded),
                        label: Text(tr('إضافة سؤال', 'Add question')),
                      ),
                    ],
                  ),
                  for (var i = 0; i < _questions.length; i++)
                    Container(
                      margin: const EdgeInsets.only(top: 8),
                      padding: const EdgeInsets.fromLTRB(12, 8, 4, 12),
                      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(16), border: Border.all(color: palette.line)),
                      child: Column(
                        children: [
                          Row(
                            children: [
                              CircleAvatar(radius: 13, backgroundColor: AppTheme.blue.withValues(alpha: .12),
                                  child: Text('${i + 1}', style: const TextStyle(fontSize: 12, color: AppTheme.blue, fontWeight: FontWeight.bold))),
                              const SizedBox(width: 8),
                              Expanded(
                                child: TextFormField(
                                  controller: _questions[i].header,
                                  maxLength: 500,
                                  decoration: InputDecoration(hintText: tr('نص السؤال', 'Question'), counterText: ''),
                                ),
                              ),
                              IconButton(
                                tooltip: tr('حذف السؤال', 'Remove question'),
                                onPressed: () => setState(() => _questions.removeAt(i).dispose()),
                                icon: const Icon(Icons.close_rounded, color: _red),
                              ),
                            ],
                          ),
                          Padding(
                            padding: const EdgeInsetsDirectional.only(start: 34, end: 8),
                            child: TextFormField(
                              controller: _questions[i].items,
                              minLines: 1,
                              maxLines: 5,
                              decoration: InputDecoration(
                                hintText: tr('النقاط أو الخيارات (كل سطر نقطة) - اختياري', 'Points or choices (one per line) - optional'),
                                hintStyle: const TextStyle(fontSize: 12.5),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  if (_subject != null && _subject!.sections.isNotEmpty) ...[
                    const SizedBox(height: 22),
                    Text(tr('النشر الآن لفصول (اختياري)', 'Publish now to classes (optional)'),
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: palette.ink)),
                    const SizedBox(height: 4),
                    Text(tr('يمكنك النشر لاحقاً من قائمة الواجبات.', 'You can also publish later from the homework list.'),
                        style: TextStyle(fontSize: 12.5, color: palette.muted)),
                    const SizedBox(height: 10),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        for (final s in _subject!.sections)
                          FilterChip(
                            label: Text(s.name),
                            selected: _publishTo.contains(s.id),
                            onSelected: (v) => setState(() => v ? _publishTo.add(s.id) : _publishTo.remove(s.id)),
                          ),
                      ],
                    ),
                  ],
                  const SizedBox(height: 26),
                  FilledButton.icon(
                    key: const ValueKey('save-homework'),
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(50)),
                    onPressed: _saving ? null : _save,
                    icon: _saving
                        ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white))
                        : Icon(_publishTo.isEmpty ? Icons.save_rounded : Icons.send_rounded),
                    label: Text(_publishTo.isEmpty ? tr('حفظ الواجب', 'Save homework') : tr('حفظ ونشر', 'Save and publish')),
                  ),
                ],
              ),
            ),
    );
  }

  InputDecoration _decoration(String label, IconData icon) => InputDecoration(
        labelText: label,
        prefixIcon: Icon(icon),
        filled: true,
        fillColor: context.palette.field,
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
      );
}
