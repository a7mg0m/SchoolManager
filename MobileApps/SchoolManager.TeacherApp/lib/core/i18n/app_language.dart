import 'package:flutter/widgets.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// App language: Arabic (default) or English. Saved on the device.
class AppLanguage extends ValueNotifier<Locale> {
  AppLanguage._() : super(arabic);
  static final instance = AppLanguage._();

  static const arabic = Locale('ar');
  static const english = Locale('en');
  static const supported = [arabic, english];

  static const _key = 'app_language';

  bool get isEnglish => value.languageCode == 'en';

  /// Read the saved language (call before runApp)
  Future<void> load() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      value = prefs.getString(_key) == 'en' ? english : arabic;
    } catch (_) {
      value = arabic;
    }
  }

  Future<void> setLanguage(Locale locale) async {
    if (locale.languageCode == value.languageCode) return;
    value = locale;
    _rebuildAll();
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_key, locale.languageCode);
    } catch (_) {
      // Still applied for this session
    }
  }

  /// Texts are picked when widgets build: rebuild every widget (screens keep their state)
  static void _rebuildAll() {
    void mark(Element element) {
      element.markNeedsBuild();
      element.visitChildren(mark);
    }

    WidgetsBinding.instance.rootElement?.visitChildren(mark);
  }
}

/// The text in the app language: tr('عن المدرسة', 'About the school')
String tr(String ar, String en) => AppLanguage.instance.isEnglish ? en : ar;

/// A name the API sends in both languages: the one of the app language, else the other
String pickLang(String? ar, String? en) {
  final a = ar?.trim() ?? '';
  final e = en?.trim() ?? '';
  if (AppLanguage.instance.isEnglish) return e.isNotEmpty ? e : a;
  return a.isNotEmpty ? a : e;
}
