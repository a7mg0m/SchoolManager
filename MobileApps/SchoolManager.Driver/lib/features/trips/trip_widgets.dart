import 'package:flutter/material.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'trip_service.dart';

/// Colour of a trip status
Color tripStatusColor(TripStatus status) => switch (status) {
      TripStatus.planned => const Color(0xFFE19A00),
      TripStatus.inProgress => AppTheme.blue,
      TripStatus.completed => const Color(0xFF1F9D57),
      TripStatus.cancelled => const Color(0xFF8A8F99),
    };

IconData tripTypeIcon(TripType type) => type == TripType.toSchool ? Icons.school_rounded : Icons.home_rounded;

/// "08:15"
String formatTime(DateTime? t) =>
    t == null ? '--:--' : '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

/// "الأحد 24/11" / "Sun 24/11"
String formatDay(DateTime d) {
  const ar = ['الإثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت', 'الأحد'];
  const en = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  final name = AppLanguage.instance.isEnglish ? en[d.weekday - 1] : ar[d.weekday - 1];
  return '$name ${d.day}/${d.month}/${d.year}';
}

class TripStatusChip extends StatelessWidget {
  const TripStatusChip(this.status, {super.key});
  final TripStatus status;

  @override
  Widget build(BuildContext context) {
    final color = tripStatusColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (status == TripStatus.inProgress) ...[
            Container(width: 7, height: 7, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
            const SizedBox(width: 6),
          ],
          Text(status.label, style: TextStyle(color: color, fontWeight: FontWeight.w700, fontSize: 12)),
        ],
      ),
    );
  }
}

/// A trip with its times, students and the next action (start / finish)
class TripCard extends StatelessWidget {
  const TripCard({
    super.key,
    required this.trip,
    this.onTap,
    this.onStart,
    this.onComplete,
    this.busy = false,
    this.showDate = false,
  });

  final Trip trip;
  final VoidCallback? onTap;
  final VoidCallback? onStart;
  final VoidCallback? onComplete;
  final bool busy;
  final bool showDate;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final color = tripStatusColor(trip.status);
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 0,
      color: palette.card,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(color: trip.status == TripStatus.inProgress ? color.withValues(alpha: .5) : palette.line),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(14)),
                    child: Icon(tripTypeIcon(trip.type), color: color),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(trip.type.label,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: palette.ink)),
                        const SizedBox(height: 2),
                        Text(
                          showDate ? formatDay(trip.date) : tr('${trip.students} طالب', '${trip.students} students'),
                          style: TextStyle(color: palette.muted, fontSize: 12.5),
                        ),
                      ],
                    ),
                  ),
                  TripStatusChip(trip.status),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  _time(context, Icons.play_circle_outline_rounded, tr('البدء', 'Started'), trip.startedAt),
                  const SizedBox(width: 8),
                  _time(context, Icons.flag_outlined, tr('الانتهاء', 'Finished'), trip.endedAt),
                  const SizedBox(width: 8),
                  _time(context, Icons.groups_2_outlined, tr('الطلاب', 'Students'), null, text: '${trip.students}'),
                ],
              ),
              if (onStart != null || onComplete != null) ...[
                const SizedBox(height: 12),
                SizedBox(
                  height: 46,
                  child: FilledButton.icon(
                    key: ValueKey('trip-action-${trip.id}'),
                    onPressed: busy ? null : (onStart ?? onComplete),
                    style: FilledButton.styleFrom(
                      backgroundColor: onStart != null ? AppTheme.blue : const Color(0xFF1F9D57),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                    ),
                    icon: busy
                        ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : Icon(onStart != null ? Icons.play_arrow_rounded : Icons.flag_rounded),
                    label: Text(onStart != null ? tr('بدء الرحلة', 'Start trip') : tr('إنهاء الرحلة', 'Finish trip'),
                        style: const TextStyle(fontWeight: FontWeight.bold)),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _time(BuildContext context, IconData icon, String label, DateTime? time, {String? text}) {
    final palette = context.palette;
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 6),
        decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(12)),
        child: Column(
          children: [
            Icon(icon, size: 18, color: palette.muted),
            const SizedBox(height: 2),
            Text(text ?? formatTime(time), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
            Text(label, style: TextStyle(fontSize: 11, color: palette.muted)),
          ],
        ),
      ),
    );
  }
}
