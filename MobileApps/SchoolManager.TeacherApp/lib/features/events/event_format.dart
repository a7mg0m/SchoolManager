import '../../core/i18n/app_language.dart';

const _arabicDays = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];
const _englishDays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const _arabicMonths = [
  'يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو',
  'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر',
];
const _englishMonths = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

/// Day names in the app language, 0 = Sunday … 6 = Saturday
List<String> get dayNames => AppLanguage.instance.isEnglish ? _englishDays : _arabicDays;

/// Month names in the app language, index 0 = January … 11 = December
List<String> get monthNames => AppLanguage.instance.isEnglish ? _englishMonths : _arabicMonths;

String formatTime(int minutes, {bool withPeriod = true}) {
  final h = (minutes ~/ 60) % 24;
  final m = minutes % 60;
  final hour12 = h % 12 == 0 ? 12 : h % 12;
  final text = '$hour12:${m.toString().padLeft(2, '0')}';
  return withPeriod ? '$text ${timePeriod(minutes)}' : text;
}

/// "ص" / "AM" or "م" / "PM"
String timePeriod(int minutes) => (minutes ~/ 60) % 24 < 12 ? tr('ص', 'AM') : tr('م', 'PM');

/// 15 -> "15 دقيقة" / "15 minutes", 60 -> "ساعة" / "1 hour", 90 -> "ساعة و 30 دقيقة" / "1 hour 30 minutes"
String formatDuration(int minutes) {
  if (AppLanguage.instance.isEnglish) {
    String mins(int m) => m == 1 ? '1 minute' : '$m minutes';
    if (minutes < 60) return mins(minutes);
    final h = minutes ~/ 60;
    final m = minutes % 60;
    final hours = h == 1 ? '1 hour' : '$h hours';
    return m == 0 ? hours : '$hours ${mins(m)}';
  }
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

/// 5 -> "بعد 5 دقائق" / "in 5 minutes"
String formatIn(int minutes) => minutes <= 1
    ? tr('بعد أقل من دقيقة', 'in less than a minute')
    : tr('بعد ${formatDuration(minutes)}', 'in ${formatDuration(minutes)}');
