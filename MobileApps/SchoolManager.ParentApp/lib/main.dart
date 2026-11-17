import 'package:flutter/material.dart';
import 'core/auth/auth_service.dart';
import 'core/routes.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/login_screen.dart';
import 'features/home/home_screen.dart';
import 'features/intro/intro_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Signed in before (and the token is still valid)? Open Home directly.
  final session = await AuthService.instance.restoreSession();
  runApp(SmartSchoolParentApp(initialRoute: session != null ? AppRoutes.home : AppRoutes.intro));
}

class SmartSchoolParentApp extends StatelessWidget {
  const SmartSchoolParentApp({super.key, this.initialRoute = AppRoutes.intro});

  /// Intro for a new user, Home when a saved session exists
  final String initialRoute;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Smart School Parent',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      builder: (context, child) => Directionality(
        textDirection: TextDirection.rtl,
        child: child ?? const SizedBox.shrink(),
      ),
      // Intro (what the app is) -> Login -> Home
      initialRoute: initialRoute,
      routes: {
        AppRoutes.intro: (_) => const IntroScreen(),
        AppRoutes.login: (_) => const LoginScreen(),
        AppRoutes.home: (_) => const HomeScreen(),
      },
    );
  }
}
