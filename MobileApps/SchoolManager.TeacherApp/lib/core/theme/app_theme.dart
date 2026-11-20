import 'package:flutter/material.dart';

/// Colours that change between light and dark mode (cards, text, borders, soft backgrounds).
/// Accent colours (blue, green, red, …) stay the same in both modes.
@immutable
class AppPalette extends ThemeExtension<AppPalette> {
  const AppPalette({
    required this.card,
    required this.ink,
    required this.muted,
    required this.faint,
    required this.line,
    required this.divider,
    required this.chip,
    required this.soft,
    required this.field,
    required this.placeholder,
    required this.dot,
    required this.infoTint,
    required this.warnTint,
    required this.errorTint,
  });

  /// Cards, tiles, sheets
  final Color card;

  /// Main text
  final Color ink;

  /// Secondary text
  final Color muted;

  /// Disabled / past items, small icons
  final Color faint;

  /// Card borders
  final Color line;

  /// Dividers inside cards
  final Color divider;

  /// Count badges, chips, progress tracks
  final Color chip;

  /// Finished items (slightly darker / lighter than a card)
  final Color soft;

  /// Text field fill
  final Color field;

  /// Image loading / missing image
  final Color placeholder;

  /// Inactive slider dots
  final Color dot;

  final Color infoTint;
  final Color warnTint;
  final Color errorTint;

  static const light = AppPalette(
    card: Colors.white,
    ink: Color(0xFF14213D),
    muted: Color(0xFF637394),
    faint: Color(0xFFA0AABF),
    line: Color(0xFFE6ECF5),
    divider: Color(0xFFEDF1F7),
    chip: Color(0xFFE3E9F3),
    soft: Color(0xFFF7F9FC),
    field: Color(0xFFF3F6FC),
    placeholder: Color(0xFFE9EEF6),
    dot: Color(0xFFD3DDEE),
    infoTint: Color(0xFFDCEAFF),
    warnTint: Color(0xFFFFF4E5),
    errorTint: Color(0xFFFDECEC),
  );

  static const dark = AppPalette(
    card: Color(0xFF1A2333),
    ink: Color(0xFFE7ECF5),
    muted: Color(0xFF9AA8C2),
    faint: Color(0xFF6F7C95),
    line: Color(0xFF2A3549),
    divider: Color(0xFF263044),
    chip: Color(0xFF2A3549),
    soft: Color(0xFF151D2A),
    field: Color(0xFF222C3E),
    placeholder: Color(0xFF222C3E),
    dot: Color(0xFF3A4660),
    infoTint: Color(0xFF1C3557),
    warnTint: Color(0xFF3B2D14),
    errorTint: Color(0xFF3D2023),
  );

  @override
  AppPalette copyWith() => this;

  @override
  AppPalette lerp(AppPalette? other, double t) {
    if (other == null) return this;
    Color mix(Color a, Color b) => Color.lerp(a, b, t)!;
    return AppPalette(
      card: mix(card, other.card),
      ink: mix(ink, other.ink),
      muted: mix(muted, other.muted),
      faint: mix(faint, other.faint),
      line: mix(line, other.line),
      divider: mix(divider, other.divider),
      chip: mix(chip, other.chip),
      soft: mix(soft, other.soft),
      field: mix(field, other.field),
      placeholder: mix(placeholder, other.placeholder),
      dot: mix(dot, other.dot),
      infoTint: mix(infoTint, other.infoTint),
      warnTint: mix(warnTint, other.warnTint),
      errorTint: mix(errorTint, other.errorTint),
    );
  }
}

extension AppPaletteContext on BuildContext {
  /// Colours of the current mode (light or dark)
  AppPalette get palette => Theme.of(this).extension<AppPalette>() ?? AppPalette.light;
}

class AppTheme {
  static const Color navy = Color(0xFF0755A5);
  static const Color blue = Color(0xFF1677E8);
  static const Color background = Color(0xFFF3F6FC);
  static const Color darkBackground = Color(0xFF0F1622);

  /// Light-mode text colour (use context.palette.ink in screens)
  static const Color ink = Color(0xFF14213D);

  static ThemeData get light => _build(Brightness.light);
  static ThemeData get dark => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final isDark = brightness == Brightness.dark;
    final palette = isDark ? AppPalette.dark : AppPalette.light;
    final scheme = ColorScheme.fromSeed(
      seedColor: blue,
      brightness: brightness,
      primary: blue,
      onPrimary: Colors.white,
      surface: palette.card,
      onSurface: palette.ink,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: isDark ? darkBackground : background,
      fontFamily: 'Arial',
      extensions: [palette],
      appBarTheme: AppBarTheme(
        backgroundColor: isDark ? const Color(0xFF121B2A) : navy,
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      cardTheme: CardThemeData(
        color: palette.card,
        elevation: 2,
        shadowColor: const Color(0x140B3470),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(22),
        ),
      ),
      bottomSheetTheme: BottomSheetThemeData(backgroundColor: palette.card, surfaceTintColor: Colors.transparent),
      dialogTheme: DialogThemeData(backgroundColor: palette.card, surfaceTintColor: Colors.transparent),
      dividerTheme: DividerThemeData(color: palette.divider),
      navigationBarTheme: isDark
          ? NavigationBarThemeData(backgroundColor: palette.card, surfaceTintColor: Colors.transparent)
          : null,
      bottomNavigationBarTheme: isDark ? BottomNavigationBarThemeData(backgroundColor: palette.card) : null,
    );
  }
}
