import 'package:flutter/material.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'homework_screens.dart' show homeworkDate, subQuestionLabel;
import 'homework_service.dart';

/// What the teacher asked for on the details page (the list does it)
enum HomeworkDetailsAction { edit, publish }

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);

/// One homework on its own page: header with subject / due date / status, description, questions with their
/// sub-questions, and the classes it is published to. Edit / publish are handed back to the list.
class HomeworkDetailsScreen extends StatelessWidget {
  const HomeworkDetailsScreen({super.key, required this.homework, this.now});

  final TeacherHomework homework;

  /// "Now" (tests pass a fixed time)
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final today = now ?? DateTime.now();
    final days = homework.dueDate.difference(DateTime(today.year, today.month, today.day)).inDays;
    final (dueText, dueColor) = days < 0
        ? (tr('انتهى موعد التسليم', 'Past due'), _red)
        : days == 0
            ? (tr('التسليم اليوم', 'Due today'), _orange)
            : days == 1
                ? (tr('التسليم غداً', 'Due tomorrow'), _orange)
                : (tr('بعد $days أيام', 'In $days days'), _green);

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            expandedHeight: 230,
            foregroundColor: Colors.white,
            backgroundColor: const Color(0xFF14916A),
            actions: [
              IconButton(
                key: const ValueKey('details-edit'),
                tooltip: tr('تعديل', 'Edit'),
                icon: const Icon(Icons.edit_rounded),
                onPressed: () => Navigator.of(context).pop(HomeworkDetailsAction.edit),
              ),
              IconButton(
                key: const ValueKey('details-publish'),
                tooltip: tr('نشر', 'Publish'),
                icon: const Icon(Icons.send_rounded),
                onPressed: () => Navigator.of(context).pop(HomeworkDetailsAction.publish),
              ),
            ],
            flexibleSpace: FlexibleSpaceBar(
              background: _header(context, dueText, dueColor),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 28),
            sliver: SliverList.list(
              children: [
                // Facts row: due date, questions, classes
                Row(
                  children: [
                    Expanded(child: _fact(context, Icons.event_rounded, tr('التسليم', 'Due'), homeworkDate(homework.dueDate), dueColor)),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _fact(context, Icons.help_outline_rounded, tr('الأسئلة', 'Questions'),
                          '${homework.questions.length}', AppTheme.blue),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _fact(context, Icons.groups_rounded, tr('الفصول', 'Classes'),
                          '${homework.publishedSections.length}', homework.isPublished ? _green : _orange),
                    ),
                  ],
                ),
                if (homework.description.isNotEmpty) ...[
                  const SizedBox(height: 18),
                  _sectionTitle(context, Icons.notes_rounded, tr('الوصف والتعليمات', 'Description and instructions')),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: palette.card,
                      borderRadius: BorderRadius.circular(18),
                      border: Border.all(color: palette.line),
                    ),
                    child: Text(homework.description, style: TextStyle(fontSize: 14.5, height: 1.7, color: palette.ink)),
                  ),
                ],
                const SizedBox(height: 18),
                _sectionTitle(context, Icons.quiz_rounded, tr('الأسئلة', 'Questions'), count: homework.questions.length),
                if (homework.questions.isEmpty)
                  _empty(context, tr('لا توجد أسئلة في هذا الواجب.', 'This homework has no questions.'))
                else
                  for (var i = 0; i < homework.questions.length; i++) ...[
                    HomeworkQuestionCard(number: i + 1, question: homework.questions[i]),
                    const SizedBox(height: 10),
                  ],
                const SizedBox(height: 8),
                _sectionTitle(context, Icons.groups_rounded, tr('منشور في', 'Published to')),
                if (!homework.isPublished)
                  _empty(context, tr('لم يُنشر بعد. اضغط زر النشر لاختيار الفصول.', 'Not published yet. Tap publish to choose the classes.'))
                else
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      for (final s in homework.publishedSections)
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                          decoration: BoxDecoration(
                            color: _green.withValues(alpha: .1),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: _green.withValues(alpha: .3)),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(Icons.check_circle_rounded, size: 16, color: _green),
                              const SizedBox(width: 6),
                              Text(s.name.isEmpty ? tr('فصل', 'Class') : s.name,
                                  style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                              const SizedBox(width: 6),
                              Text(homeworkDate(s.publishedOn), style: TextStyle(fontSize: 11, color: palette.muted)),
                            ],
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

  /// Gradient header: subject · grade, title, status chips
  Widget _header(BuildContext context, String dueText, Color dueColor) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          colors: [Color(0xFF2BC48A), Color(0xFF14916A), Color(0xFF1B5FAA)],
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
        ),
      ),
      child: Stack(
        children: [
          PositionedDirectional(
            end: -30,
            top: 20,
            child: Icon(Icons.menu_book_rounded, size: 170, color: Colors.white.withValues(alpha: .1)),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 56, 20, 18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  Text(
                    [homework.subjectName, homework.gradeName].where((s) => s.isNotEmpty).join(' · '),
                    style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 13, fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 6),
                  Text(homework.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(color: Colors.white, fontSize: 23, fontWeight: FontWeight.bold, height: 1.3)),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 8,
                    runSpacing: 6,
                    children: [
                      _headerChip(Icons.schedule_rounded, dueText),
                      _headerChip(
                        homework.isPublished ? Icons.public_rounded : Icons.edit_note_rounded,
                        homework.isPublished ? tr('منشور', 'Published') : tr('غير منشور', 'Not published'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _headerChip(IconData icon, String text) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: .18),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: Colors.white.withValues(alpha: .25)),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 14, color: Colors.white),
            const SizedBox(width: 5),
            Text(text, style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.bold)),
          ],
        ),
      );

  Widget _fact(BuildContext context, IconData icon, String label, String value, Color color) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: palette.line),
      ),
      child: Column(
        children: [
          Icon(icon, color: color, size: 22),
          const SizedBox(height: 6),
          Text(value,
              textAlign: TextAlign.center,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: palette.ink)),
          const SizedBox(height: 2),
          Text(label, style: TextStyle(fontSize: 11, color: palette.muted)),
        ],
      ),
    );
  }

  Widget _sectionTitle(BuildContext context, IconData icon, String title, {int? count}) {
    final palette = context.palette;
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        children: [
          Icon(icon, size: 20, color: const Color(0xFF14916A)),
          const SizedBox(width: 6),
          Text(title, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: palette.ink)),
          if (count != null) ...[
            const SizedBox(width: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
              decoration: BoxDecoration(color: palette.chip, borderRadius: BorderRadius.circular(10)),
              child: Text('$count', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: palette.muted)),
            ),
          ],
        ],
      ),
    );
  }

  Widget _empty(BuildContext context, String text) => Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(color: context.palette.soft, borderRadius: BorderRadius.circular(16)),
        child: Text(text, style: TextStyle(color: context.palette.muted)),
      );
}

/// A question: numbered header, then its lettered sub-questions
class HomeworkQuestionCard extends StatelessWidget {
  const HomeworkQuestionCard({super.key, required this.number, required this.question});

  final int number;
  final HomeworkQuestion question;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    const accent = Color(0xFF14916A);
    return Container(
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: palette.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
            decoration: BoxDecoration(
              color: accent.withValues(alpha: .07),
              borderRadius: const BorderRadius.vertical(top: Radius.circular(18)),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 28,
                  height: 28,
                  alignment: Alignment.center,
                  decoration: const BoxDecoration(color: accent, shape: BoxShape.circle),
                  child: Text('$number', style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13)),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(top: 3),
                    child: Text(question.header,
                        style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, height: 1.4, color: palette.ink)),
                  ),
                ),
              ],
            ),
          ),
          if (question.items.isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(14, 6, 14, 10),
              child: Column(
                children: [
                  for (var j = 0; j < question.items.length; j++) ...[
                    if (j > 0) Divider(height: 1, color: palette.divider),
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 9),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 24,
                            height: 24,
                            alignment: Alignment.center,
                            decoration: BoxDecoration(
                              color: accent.withValues(alpha: .12),
                              borderRadius: BorderRadius.circular(7),
                            ),
                            child: Text(subQuestionLabel(j),
                                style: const TextStyle(color: accent, fontWeight: FontWeight.bold, fontSize: 12)),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: Padding(
                              padding: const EdgeInsets.only(top: 2),
                              child: Text(question.items[j], style: TextStyle(fontSize: 14, height: 1.5, color: palette.ink)),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ],
              ),
            ),
        ],
      ),
    );
  }
}
