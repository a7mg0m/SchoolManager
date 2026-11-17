import 'dart:ui' show Color;

import 'schedule_service.dart';

/// Arabic day names, 0 = Sunday … 6 = Saturday
const arabicDays = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];

/// One colour per subject (picked from its name, so it is the same on every day)
const _palette = [
  Color(0xFF1677E8), // blue
  Color(0xFF20A66A), // green
  Color(0xFF8957E5), // purple
  Color(0xFFF08C00), // orange
  Color(0xFFE0457B), // pink
  Color(0xFF0FA5A5), // teal
  Color(0xFF4C5FD5), // indigo
  Color(0xFFB7791F), // amber
];

Color subjectColor(ScheduleSlot slot) {
  final hash = slot.title.codeUnits.fold<int>(0, (sum, c) => (sum * 31 + c) & 0x7fffffff);
  return _palette[hash % _palette.length];
}

/// 510 -> "8:30 ص" (or "8:30" without the period)
String formatTime(int minutes, {bool withPeriod = true}) {
  final h = (minutes ~/ 60) % 24;
  final m = minutes % 60;
  final hour12 = h % 12 == 0 ? 12 : h % 12;
  final text = '$hour12:${m.toString().padLeft(2, '0')}';
  return withPeriod ? '$text ${timePeriod(minutes)}' : text;
}

/// "ص" (AM) or "م" (PM)
String timePeriod(int minutes) => (minutes ~/ 60) % 24 < 12 ? 'ص' : 'م';

/// 15 -> "15 دقيقة", 2 -> "دقيقتين", 60 -> "ساعة", 90 -> "ساعة و 30 دقيقة"
String formatDuration(int minutes) {
  String mins(int m) => m == 1
      ? 'دقيقة'
      : m == 2
          ? 'دقيقتين'
          : m <= 10
              ? '$m دقائق'
              : '$m دقيقة';
  if (minutes < 60) return mins(minutes);
  final h = minutes ~/ 60;
  final m = minutes % 60;
  final hours = h == 1 ? 'ساعة' : h == 2 ? 'ساعتين' : '$h ساعات';
  return m == 0 ? hours : '$hours و ${mins(m)}';
}

/// 5 -> "بعد 5 دقائق"
String formatIn(int minutes) => minutes <= 1 ? 'بعد أقل من دقيقة' : 'بعد ${formatDuration(minutes)}';

/// "6 حصص"
String lessonsCount(int count) =>
    '$count ${count == 1 ? 'حصة' : count == 2 ? 'حصتان' : count <= 10 ? 'حصص' : 'حصة'}';
