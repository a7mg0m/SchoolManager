# Smart School Parent App – Flutter Starter

A responsive Arabic RTL parent dashboard starter inspired by the reference design.

## Requirements
- Flutter SDK (stable)
- Visual Studio Code
- Flutter and Dart VS Code extensions

## Run
1. Extract the ZIP.
2. Open the `smart_school_parent_flutter` folder in VS Code.
3. Open the terminal and run:
   ```bash
   flutter pub get
   flutter run
   ```
4. Choose an Android emulator or connected device.

## Included
- Arabic RTL Material 3 theme
- Parent profile header
- Select-child dropdown (supports multiple children)
- Dashboard metrics
- Important announcement
- Upcoming activity
- Latest homework
- Bus status card
- Recent notification
- Bottom navigation

## Customize
- Demo parent and child data: `lib/data/demo_data.dart`
- Dashboard UI: `lib/features/home/home_screen.dart`
- Theme/colors: `lib/core/theme/app_theme.dart`

This is a UI starter. Replace demo data with your .NET API responses when ready.

## Push notifications (Firebase)

Push is wired in `lib/core/push/push_service.dart` and stays off until Firebase is configured.
Full setup and testing: [PUSH_NOTIFICATIONS.md](../../PUSH_NOTIFICATIONS.md).

- Android: put `google-services.json` in `android/app/` (not committed). The Google Services plugin is applied only when
  that file exists; `POST_NOTIFICATIONS` and the default channel `school_notifications` are already in the manifest.
- iOS: put `GoogleService-Info.plist` in `ios/Runner/` and add it to the Runner target, then in Xcode → Runner →
  Signing & Capabilities add **Push Notifications** and **Background Modes → Remote notifications**
  (`remote-notification` is already in `Info.plist`). Upload an APNs key in the Firebase console.
