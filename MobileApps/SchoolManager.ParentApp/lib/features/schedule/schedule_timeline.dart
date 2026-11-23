import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import 'schedule_format.dart';
import 'schedule_service.dart';

const _ink = AppTheme.ink;
const _muted = Color(0xFF637394);
const _faded = Color(0xFFA0AABF);
const _rail = Color(0xFFE3E9F3);
const _green = Color(0xFF13864B);
const _orange = Color(0xFFF08C00);

/// Gaps shorter than this between two lessons are not shown as a break
const _minBreakMinutes = 5;

/// One day's lessons as a timeline: period number, subject colour, teacher and time,
/// breaks between lessons, and on today the lesson now (with progress) / next and the finished ones.
class ScheduleTimeline extends StatefulWidget {
  const ScheduleTimeline({
    super.key,
    required this.slots,
    required this.isToday,
    required this.now,
  });

  /// Lessons in time order
  final List<ScheduleSlot> slots;
  final bool isToday;

  /// School time now, in minutes since midnight
  final int now;

  @override
  State<ScheduleTimeline> createState() => _ScheduleTimelineState();
}

class _ScheduleTimelineState extends State<ScheduleTimeline> {
  bool _showFinished = false;

  int get _now => widget.now;

  SlotStatus _statusOf(ScheduleSlot slot) =>
      slot.statusAt(_now, isToday: widget.isToday);

  @override
  Widget build(BuildContext context) {
    final slots = widget.slots;
    final statuses = {for (final s in slots) s.id: _statusOf(s)};
    final finishedCount =
        statuses.values.where((s) => s == SlotStatus.finished).length;
    // Fold finished lessons away while some lessons are still ahead
    final canFold = finishedCount > 0 && finishedCount < slots.length;
    final hideFinished = canFold && !_showFinished;
    final nextId = slots
        .where((s) => statuses[s.id] == SlotStatus.upcoming)
        .firstOrNull
        ?.id;

    final rows = <Widget>[];
    ScheduleSlot? previous;
    for (var i = 0; i < slots.length; i++) {
      final slot = slots[i];
      final status = statuses[slot.id]!;
      if (hideFinished && status == SlotStatus.finished) {
        previous = slot;
        continue;
      }
      if (previous != null &&
          (!hideFinished || statuses[previous.id] != SlotStatus.finished)) {
        final gap = slot.start - previous.end;
        if (gap >= _minBreakMinutes)
          rows.add(_breakRow(previous.end, slot.start, gap));
      }
      rows.add(_lessonRow(slot, i + 1, status,
          isNext: slot.id == nextId, isLast: i == slots.length - 1));
      previous = slot;
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (hideFinished) _finishedToggle(finishedCount, expanded: false),
        ...rows,
        if (canFold && _showFinished)
          _finishedToggle(finishedCount, expanded: true),
      ],
    );
  }

  /// One lesson: time | rail with dot | card
  Widget _lessonRow(ScheduleSlot slot, int period, SlotStatus status,
      {required bool isNext, required bool isLast}) {
    final isNow = status == SlotStatus.current;
    final isDone = status == SlotStatus.finished;
    // "Next" is only worth a badge on today
    final showNext = isNext && widget.isToday;
    final color = isDone ? _faded : subjectColor(slot);

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Time column
          SizedBox(
            width: 58,
            child: Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(formatTime(slot.start, withPeriod: false),
                      style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: isDone ? _faded : _ink)),
                  Text(timePeriod(slot.start),
                      style: TextStyle(
                          fontSize: 10.5, color: isDone ? _faded : _muted)),
                ],
              ),
            ),
          ),
          // Rail: dot (glowing for the lesson now) and the line to the next row
          SizedBox(
            width: 22,
            child: Column(
              children: [
                const SizedBox(height: 14),
                Container(
                  width: isNow ? 16 : 12,
                  height: isNow ? 16 : 12,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: color,
                    border: Border.all(color: Colors.white, width: 2),
                    boxShadow: isNow
                        ? [
                            BoxShadow(
                                color: color.withValues(alpha: .45),
                                blurRadius: 8,
                                spreadRadius: 2)
                          ]
                        : null,
                  ),
                  child: isDone
                      ? const Icon(Icons.check, size: 7, color: Colors.white)
                      : null,
                ),
                if (!isLast) Expanded(child: Container(width: 2, color: _rail)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          // Lesson card
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Container(
                decoration: BoxDecoration(
                  color: isNow
                      ? color.withValues(alpha: .08)
                      : (isDone ? const Color(0xFFF7F9FC) : Colors.white),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                      color: isNow
                          ? color.withValues(alpha: .55)
                          : const Color(0xFFE6ECF5),
                      width: isNow ? 1.5 : 1),
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(16),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Container(width: 5, color: color), // subject colour strip
                      Expanded(
                        child: Padding(
                          padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Period badge at the start, status at the end (wraps on narrow screens)
                              Wrap(
                                alignment: WrapAlignment.spaceBetween,
                                crossAxisAlignment: WrapCrossAlignment.center,
                                spacing: 6,
                                runSpacing: 6,
                                children: [
                                  _chip(
                                      'الحصة $period',
                                      color.withValues(
                                          alpha: isDone ? .10 : .12),
                                      color),
                                  if (isNow)
                                    _chip('الآن', _green, Colors.white,
                                        icon: Icons.play_arrow_rounded),
                                  if (showNext)
                                    _chip('الحصة التالية', AppTheme.blue,
                                        Colors.white),
                                  if (isDone)
                                    const Icon(Icons.check_circle_rounded,
                                        size: 18, color: _faded),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Text(
                                slot.title,
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: isDone ? _faded : _ink,
                                  decoration: isDone
                                      ? TextDecoration.lineThrough
                                      : null,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Wrap(
                                spacing: 10,
                                runSpacing: 2,
                                children: [
                                  if (slot.teacher.isNotEmpty)
                                    _meta(Icons.person_outline_rounded,
                                        slot.teacher, isDone),
                                  _meta(
                                      Icons.schedule_rounded,
                                      '${formatTime(slot.start)} - ${formatTime(slot.end)}',
                                      isDone),
                                ],
                              ),
                              if (isNow) ...[
                                const SizedBox(height: 10),
                                _nowProgress(slot, color)
                              ],
                              if (showNext) ...[
                                const SizedBox(height: 8),
                                Text('تبدأ ${formatIn(slot.start - _now)}',
                                    style: const TextStyle(
                                        fontSize: 12,
                                        fontWeight: FontWeight.bold,
                                        color: AppTheme.blue)),
                              ],
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  /// Progress of the lesson now and the time left
  Widget _nowProgress(ScheduleSlot slot, Color color) {
    final length = (slot.end - slot.start).clamp(1, 24 * 60);
    final done = (_now - slot.start).clamp(0, length);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(6),
          child: LinearProgressIndicator(
              value: done / length,
              minHeight: 6,
              backgroundColor: Colors.white,
              color: color),
        ),
        const SizedBox(height: 5),
        Text('تنتهي ${formatIn(slot.end - _now)}',
            style: const TextStyle(
                fontSize: 12, fontWeight: FontWeight.bold, color: _green)),
      ],
    );
  }

  /// Break between two lessons (highlighted while it is happening)
  Widget _breakRow(int from, int to, int minutes) {
    final isNow = widget.isToday && from <= _now && _now < to;
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        children: [
          const SizedBox(width: 58),
          SizedBox(
            width: 22,
            child: Center(
                child: Icon(Icons.local_cafe_rounded,
                    size: 14, color: isNow ? _orange : _faded)),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
              decoration: BoxDecoration(
                color:
                    isNow ? const Color(0xFFFFF4E5) : const Color(0xFFF7F9FC),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                isNow
                    ? 'استراحة الآن · تنتهي ${formatIn(to - _now)}'
                    : 'استراحة ${formatDuration(minutes)}',
                style: TextStyle(
                  fontSize: 12,
                  color: isNow ? const Color(0xFFB36200) : _muted,
                  fontWeight: isNow ? FontWeight.bold : null,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _finishedToggle(int count, {required bool expanded}) {
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: TextButton.icon(
        onPressed: () => setState(() => _showFinished = !expanded),
        icon: Icon(
            expanded ? Icons.expand_less_rounded : Icons.expand_more_rounded,
            size: 18),
        label: Text(
            expanded ? 'إخفاء الحصص المنتهية' : 'عرض الحصص المنتهية ($count)'),
      ),
    );
  }

  /// Icon + text; a long text (teacher name) is cut with "…" instead of overflowing
  Widget _meta(IconData icon, String text, bool isDone) {
    final color = isDone ? _faded : _muted;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color),
        const SizedBox(width: 3),
        Flexible(
          child: Text(text,
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
              style: TextStyle(fontSize: 12, color: color)),
        ),
      ],
    );
  }

  Widget _chip(String text, Color background, Color foreground,
      {IconData? icon}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
          color: background, borderRadius: BorderRadius.circular(20)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) Icon(icon, size: 13, color: foreground),
          Text(text,
              style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: foreground)),
        ],
      ),
    );
  }
}
