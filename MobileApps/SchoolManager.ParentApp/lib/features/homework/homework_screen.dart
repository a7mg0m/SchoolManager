import 'package:flutter/material.dart';

import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../schedule/schedule_format.dart';
import 'homework_service.dart';

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _teal = Color(0xFF14916A);

enum _Tab { open, past }

/// Homework the teachers published to the selected child's class: to do (by due date) and past
class HomeworkScreen extends StatefulWidget {
  const HomeworkScreen({super.key, required this.child, this.loadHomeworks, this.onSessionExpired, this.now});

  final Child child;

  /// Defaults to the API (tests pass their own)
  final Future<List<ChildHomework>> Function(String studentId)? loadHomeworks;
  final VoidCallback? onSessionExpired;

  /// "Now" (tests pass a fixed time)
  final DateTime? now;

  @override
  State<HomeworkScreen> createState() => _HomeworkScreenState();
}

class _HomeworkScreenState extends State<HomeworkScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  List<ChildHomework>? _homeworks;
  String? _error;
  bool _loading = true;
  _Tab _tab = _Tab.open;

  DateTime get _now => widget.now ?? DateTime.now();

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _homeworks == null;
      _error = null;
    });
    try {
      final loader = widget.loadHomeworks ?? HomeworkService.instance.getChildHomeworks;
      final result = await loader(widget.child.id);
      if (!mounted) return;
      setState(() {
        _homeworks = result;
        _loading = false;
      });
    } on HomeworkException catch (e) {
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

  @override
  Widget build(BuildContext context) {
    final open = (_homeworks ?? const <ChildHomework>[]).where((h) => h.isOpen(_now)).toList()
      ..sort((a, b) => a.dueDate.compareTo(b.dueDate));
    final past = (_homeworks ?? const <ChildHomework>[]).where((h) => !h.isOpen(_now)).toList()
      ..sort((a, b) => b.dueDate.compareTo(a.dueDate));
    final list = _tab == _Tab.open ? open : past;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('الواجبات', 'Homework'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
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
                ? _message(Icons.error_outline_rounded, _error!, retry: true)
                : RefreshIndicator(
                    onRefresh: _load,
                    child: ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
                      children: [
                        if (open.isNotEmpty) _summary(open),
                        SegmentedButton<_Tab>(
                          showSelectedIcon: false,
                          segments: [
                            ButtonSegment(
                                value: _Tab.open,
                                icon: const Icon(Icons.assignment_rounded),
                                label: Text(tr('المطلوبة (${open.length})', 'To do (${open.length})'))),
                            ButtonSegment(
                                value: _Tab.past,
                                icon: const Icon(Icons.history_rounded),
                                label: Text(tr('السابقة (${past.length})', 'Past (${past.length})'))),
                          ],
                          selected: {_tab},
                          onSelectionChanged: (s) => setState(() => _tab = s.first),
                        ),
                        const SizedBox(height: 14),
                        if (list.isEmpty)
                          Padding(
                            padding: const EdgeInsets.only(top: 50),
                            child: _message(
                              Icons.menu_book_rounded,
                              _tab == _Tab.open
                                  ? tr('لا توجد واجبات مطلوبة حالياً 🎉', 'No homework to do right now 🎉')
                                  : tr('لا توجد واجبات سابقة.', 'No past homework.'),
                            ),
                          )
                        else
                          for (final h in list) ...[
                            _card(h),
                            const SizedBox(height: 12),
                          ],
                      ],
                    ),
                  ),
      ),
    );
  }

  /// Gradient card: how many to do and the nearest due date
  Widget _summary(List<ChildHomework> open) {
    final next = open.first;
    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(22),
        gradient: const LinearGradient(
          colors: [Color(0xFF2BC48A), _teal],
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 54,
            height: 54,
            alignment: Alignment.center,
            decoration: BoxDecoration(color: Colors.white.withValues(alpha: .2), shape: BoxShape.circle),
            child: Text('${open.length}', style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold)),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr('واجبات مطلوبة', 'Homework to do'),
                    style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
                const SizedBox(height: 2),
                Text('${tr('الأقرب', 'Next')}: ${next.subject} · ${_due(next).$1}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: Colors.white70, fontSize: 12.5)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Due text and colour: today / tomorrow / in N days / the date
  (String, Color) _due(ChildHomework h) {
    final days = h.daysLeft(_now);
    if (days < 0) return ('${tr('انتهى', 'Was due')} ${_date(h.dueDate)}', context.palette.faint);
    if (days == 0) return (tr('التسليم اليوم', 'Due today'), _red);
    if (days == 1) return (tr('التسليم غداً', 'Due tomorrow'), _orange);
    if (days <= 6) return (tr('التسليم بعد $days أيام', 'Due in $days days'), _green);
    return ('${tr('التسليم', 'Due')} ${_date(h.dueDate)}', _green);
  }

  static String _date(DateTime d) => '${dayNames[d.weekday % 7]} ${d.day} ${monthNames[d.month - 1]}';

  Widget _card(ChildHomework h) {
    final palette = context.palette;
    final (dueText, dueColor) = _due(h);
    final past = !h.isOpen(_now);

    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(20),
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: () => _openDetails(h),
        child: Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(20),
            border: BorderDirectional(start: BorderSide(color: past ? palette.faint : _teal, width: 4)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Flexible(
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                      decoration: BoxDecoration(color: _teal.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                      child: Text(h.subject,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(color: _teal, fontSize: 11.5, fontWeight: FontWeight.bold)),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Icon(Icons.event_rounded, size: 15, color: dueColor),
                  const SizedBox(width: 4),
                  Text(dueText, style: TextStyle(color: dueColor, fontSize: 12, fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 10),
              Text(h.title,
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: past ? palette.muted : palette.ink)),
              if (h.description.isNotEmpty) ...[
                const SizedBox(height: 4),
                Text(h.description,
                    maxLines: 2, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 13, color: palette.muted)),
              ],
              if (h.questions.isNotEmpty) ...[
                const SizedBox(height: 8),
                Row(
                  children: [
                    const Icon(Icons.help_outline_rounded, size: 15, color: AppTheme.blue),
                    const SizedBox(width: 4),
                    Text(tr('${h.questions.length} أسئلة', '${h.questions.length} questions'),
                        style: const TextStyle(fontSize: 12, color: AppTheme.blue, fontWeight: FontWeight.w600)),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  void _openDetails(ChildHomework h) {
    final (dueText, dueColor) = _due(h);
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (sheetContext) {
        final palette = sheetContext.palette;
        return SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(18, 0, 18, 20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(h.subject, style: const TextStyle(color: _teal, fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text(h.title, style: TextStyle(fontSize: 19, fontWeight: FontWeight.bold, color: palette.ink)),
                const SizedBox(height: 6),
                Text(dueText, style: TextStyle(color: dueColor, fontWeight: FontWeight.bold, fontSize: 13)),
                if (h.description.isNotEmpty) ...[
                  const SizedBox(height: 14),
                  Text(h.description, style: TextStyle(fontSize: 14.5, height: 1.6, color: palette.ink)),
                ],
                for (var i = 0; i < h.questions.length; i++) ...[
                  const SizedBox(height: 14),
                  Text('${i + 1}. ${h.questions[i].header}',
                      style: TextStyle(fontSize: 14.5, fontWeight: FontWeight.bold, color: palette.ink)),
                  for (final item in h.questions[i].items)
                    Padding(
                      padding: const EdgeInsetsDirectional.only(start: 16, top: 4),
                      child: Text('• $item', style: TextStyle(fontSize: 13.5, color: palette.ink)),
                    ),
                ],
              ],
            ),
          ),
        );
      },
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
