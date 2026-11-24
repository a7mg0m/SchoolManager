import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'core/auth/auth_service.dart';
import 'core/push/push_service.dart';
import 'core/i18n/app_language.dart';
import 'core/routes.dart';
import 'core/theme/app_theme.dart';
import 'core/theme/theme_controller.dart';
import 'features/auth/login_screen.dart';
import 'features/home/home_screen.dart';
import 'features/intro/intro_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Push notifications (does nothing until Firebase is configured, see PUSH_NOTIFICATIONS.md)
  await PushService.instance.init(appName: 'parent');
  // Light / dark / auto and Arabic / English chosen in the menu
  await ThemeController.instance.load();
  await AppLanguage.instance.load();
  // Signed in before (and the token is still valid)? Open Home directly.
  final session = await AuthService.instance.restoreSession();
  // Already signed in: this device keeps receiving the user's notifications
  if (session != null) unawaited(PushService.instance.registerCurrentUser());
  runApp(SmartSchoolParentApp(
      initialRoute: session != null ? AppRoutes.home : AppRoutes.intro));
}

class SmartSchoolParentApp extends StatelessWidget {
  const SmartSchoolParentApp({super.key, this.initialRoute = AppRoutes.intro});

  /// Intro for a new user, Home when a saved session exists
  final String initialRoute;

  @override
  Widget build(BuildContext context) {
    // Rebuilds when the user picks light / dark / auto or the language in the menu
    return ListenableBuilder(
      listenable: Listenable.merge(
          [ThemeController.instance, AppLanguage.instance]),
      builder: (context, _) => MaterialApp(
        title: 'Smart School Parent',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light,
        darkTheme: AppTheme.dark,
        themeMode: ThemeController.instance.value,
        // Arabic is right-to-left, English left-to-right (set by the locale)
        locale: AppLanguage.instance.value,
        supportedLocales: AppLanguage.supported,
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        // Intro (what the app is) -> Login -> Home
        initialRoute: initialRoute,
        routes: {
          AppRoutes.intro: (_) => const IntroScreen(),
          AppRoutes.login: (_) => const LoginScreen(),
          AppRoutes.home: (_) => const HomeScreen(),
        },
      ),
    );
  }
}
