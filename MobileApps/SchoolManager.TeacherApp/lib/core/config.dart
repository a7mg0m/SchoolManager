import 'package:flutter/foundation.dart';

/// Backend settings.
///
/// The app talks to the API Gateway (it forwards /api/user/... to the Identity service).
/// Override with: flutter run --dart-define=API_BASE_URL=http://192.168.1.10:53607
class AppConfig {
  static const _override = String.fromEnvironment('API_BASE_URL');

  static String get apiBaseUrl {
    if (_override.isNotEmpty) return _override;
    // The Android emulator reaches the PC's localhost through 10.0.2.2
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:53607';
    }
    return 'http://localhost:53607';
  }

  static const requestTimeout = Duration(seconds: 20);
}
