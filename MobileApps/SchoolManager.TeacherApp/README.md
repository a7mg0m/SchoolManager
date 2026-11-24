# Smart School Teacher

Flutter app for teachers (Arabic / English, light / dark).

Intro -> login (Identity API through the gateway; only accounts linked to a teacher can sign in) -> home.

Run on the web:

```
flutter run -d chrome --web-port 5301
```

The API address is `http://localhost:53607` (Android emulator: `http://10.0.2.2:53607`);
override it with `--dart-define=API_BASE_URL=http://<pc-ip>:53607`.

## Push notifications (Firebase)

Push is wired in `lib/core/push/push_service.dart` and stays off until Firebase is configured.
Full setup and testing: [PUSH_NOTIFICATIONS.md](../../PUSH_NOTIFICATIONS.md).

- Android: put `google-services.json` in `android/app/` (not committed). The Google Services plugin is applied only when
  that file exists; `POST_NOTIFICATIONS` and the default channel `school_notifications` are already in the manifest.
- iOS: put `GoogleService-Info.plist` in `ios/Runner/` and add it to the Runner target, then in Xcode → Runner →
  Signing & Capabilities add **Push Notifications** and **Background Modes → Remote notifications**
  (`remote-notification` is already in `Info.plist`). Upload an APNs key in the Firebase console.
