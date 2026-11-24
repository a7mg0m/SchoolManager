# smart_school_driver

Smart School Driver app.

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Learn Flutter](https://docs.flutter.dev/get-started/learn-flutter)
- [Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Flutter learning resources](https://docs.flutter.dev/reference/learning-resources)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.

## Push notifications (Firebase)

Push is wired in `lib/core/push/push_service.dart` and stays off until Firebase is configured.
Full setup and testing: [PUSH_NOTIFICATIONS.md](../../PUSH_NOTIFICATIONS.md).

- Android: put `google-services.json` in `android/app/` (not committed). The Google Services plugin is applied only when
  that file exists; `POST_NOTIFICATIONS` and the default channel `school_notifications` are already in the manifest.
- iOS: put `GoogleService-Info.plist` in `ios/Runner/` and add it to the Runner target, then in Xcode → Runner →
  Signing & Capabilities add **Push Notifications** and **Background Modes → Remote notifications**
  (`remote-notification` is already in `Info.plist`). Upload an APNs key in the Firebase console.
