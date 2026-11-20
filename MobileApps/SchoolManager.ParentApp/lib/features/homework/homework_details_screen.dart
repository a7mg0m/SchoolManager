import 'package:flutter/material.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../schedule/schedule_format.dart';
import 'homework_service.dart';

const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);
const _orange = Color(0xFFF08C00);
const _teal = Color(0xFF14916A);

/// Label of the [index]th sub-question: أ ب ج … / a b c …
String homeworkSubLabel(int index) {
  const ar = ['أ', 'ب', 'ج', 'د', 'هـ', 'و', 'ز', 'ح', 'ط', 'ي', 'ك', 'ل', 'م', 'ن', 'س', 'ع', 'ف', 'ص', 'ق', 'ر', 'ش', 'ت', 'ث', 'خ', 'ذ', 'ض', 'ظ', 'غ'];
  if (AppLanguage.instance.isEnglish) return index < 26 ? String.fromCharCode(97 + index) : '${index + 1}';
  return index < ar.length ? ar[index] : '${index + 1}';
}

/// "الخميس 20 نوفمبر" / "Thursday 20 November"
String _date(DateTime d) => '${dayNames[d.weekday % 7]} ${d.day} ${monthNames[d.month - 1]}';

/// One homework of the child on its own page: subject, title and when it is due, the teacher's
/// instructions, then every question with its sub-questions.
class HomeworkDetailsScreen extends StatelessWidget {
  const HomeworkDetailsScreen({super.key, required this.homework, this.childName, this.now});

  final ChildHomework homework;
  final String? childName;

  /// "Now" (tests pass a fixed time)
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final days = homework.daysLeft(now ?? DateTime.now());
    final (dueText, dueColor, dueIcon) = days < 0
        ? (tr('انتهى موعد التسليم', 'Past due'), _red, Icons.history_rounded)
        : days == 0
            ? (tr('التسليم اليوم', 'Due today'), _red, Icons.alarm_rounded)
            : days == 1
                ? (tr('التسليم غداً', 'Due tomorrow'), _orange, Icons.schedule_rounded)
                : (tr('التسليم بعد $days أيام', 'Due in $days days'), _green, Icons.event_available_rounded);

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            expandedHeight: 220,
            foregroundColor: Colors.white,
            backgroundColor: _teal,
            flexibleSpace: FlexibleSpaceBar(
              background: Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    colors: [Color(0xFF2BC48A), _teal, Color(0xFF1B5FAA)],
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
                              [homework.subject, if (childName != null && childName!.isNotEmpty) childName!].join(' · '),
                              style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 13, fontWeight: FontWeight.w600),
                            ),
                            const SizedBox(height: 6),
                            Text(homework.title,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: Colors.white, fontSize: 23, fontWeight: FontWeight.bold, height: 1.3)),
                            const SizedBox(height: 12),
                            Container(
                              key: const ValueKey('homework-due'),
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(20),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(dueIcon, size: 15, color: dueColor),
                                  const SizedBox(width: 5),
                                  Text(dueText, style: TextStyle(color: dueColor, fontSize: 12.5, fontWeight: FontWeight.bold)),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 28),
            sliver: SliverList.list(
              children: [
                Row(
                  children: [
                    Expanded(child: _fact(context, Icons.event_rounded, tr('التسليم', 'Due'), _date(homework.dueDate), dueColor)),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _fact(context, Icons.campaign_rounded, tr('نُشر في', 'Published'), _date(homework.publishedOn), AppTheme.blue),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _fact(context, Icons.help_outline_rounded, tr('الأسئلة', 'Questions'), '${homework.questions.length}', _teal),
                    ),
                  ],
                ),
                if (homework.description.isNotEmpty) ...[
                  const SizedBox(height: 18),
                  _sectionTitle(context, Icons.notes_rounded, tr('تعليمات المعلم', 'Teacher instructions')),
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
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(16)),
                    child: Text(tr('لا توجد أسئلة، اتبع التعليمات أعلاه.', 'No questions; follow the instructions above.'),
                        style: TextStyle(color: palette.muted)),
                  )
                else
                  for (var i = 0; i < homework.questions.length; i++) ...[
                    _questionCard(context, i + 1, homework.questions[i]),
                    const SizedBox(height: 10),
                  ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _fact(BuildContext context, IconData icon, String label, String value, Color color) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 12),
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
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 12.5, color: palette.ink)),
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
          Icon(icon, size: 20, color: _teal),
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

  /// Numbered header, then the lettered sub-questions
  Widget _questionCard(BuildContext context, int number, HomeworkQuestion question) {
    final palette = context.palette;
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
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: _teal.withValues(alpha: .07),
              borderRadius: const BorderRadius.vertical(top: Radius.circular(18)),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 28,
                  height: 28,
                  alignment: Alignment.center,
                  decoration: const BoxDecoration(color: _teal, shape: BoxShape.circle),
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
                            decoration: BoxDecoration(color: _teal.withValues(alpha: .12), borderRadius: BorderRadius.circular(7)),
                            child: Text(homeworkSubLabel(j),
                                style: const TextStyle(color: _teal, fontWeight: FontWeight.bold, fontSize: 12)),
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
