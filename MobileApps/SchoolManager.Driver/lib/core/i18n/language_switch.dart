import 'package:flutter/material.dart';

import 'app_language.dart';

/// العربية | English — changes the app language (menu, intro and login screens)
class LanguageSwitch extends StatelessWidget {
  const LanguageSwitch({super.key, this.compact = false});

  /// Small version for the intro / login screens
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<Locale>(
      valueListenable: AppLanguage.instance,
      builder: (context, locale, _) => SegmentedButton<String>(
        showSelectedIcon: false,
        style: compact
            ? const ButtonStyle(visualDensity: VisualDensity.compact, tapTargetSize: MaterialTapTargetSize.shrinkWrap)
            : null,
        // Each language name in its own language, so it can be found in either
        segments: const [
          ButtonSegment(value: 'ar', label: Text('العربية')),
          ButtonSegment(value: 'en', label: Text('English')),
        ],
        selected: {locale.languageCode},
        onSelectionChanged: (s) =>
            AppLanguage.instance.setLanguage(s.first == 'en' ? AppLanguage.english : AppLanguage.arabic),
      ),
    );
  }
}

/// Globe button that switches between Arabic and English (top bar of intro / login)
class LanguageToggleButton extends StatelessWidget {
  const LanguageToggleButton({super.key, this.color});

  final Color? color;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<Locale>(
      valueListenable: AppLanguage.instance,
      builder: (context, locale, _) => TextButton.icon(
        key: const ValueKey('language-toggle'),
        style: TextButton.styleFrom(foregroundColor: color),
        onPressed: () => AppLanguage.instance
            .setLanguage(AppLanguage.instance.isEnglish ? AppLanguage.arabic : AppLanguage.english),
        icon: const Icon(Icons.language_rounded, size: 20),
        // Shows the language it switches to
        label: Text(AppLanguage.instance.isEnglish ? 'العربية' : 'English',
            style: const TextStyle(fontWeight: FontWeight.bold)),
      ),
    );
  }
}
