# Smart School Teacher

Flutter app for teachers (Arabic / English, light / dark).

Intro -> login (Identity API through the gateway; only accounts linked to a teacher can sign in) -> home.

Run on the web:

```
flutter run -d chrome --web-port 5301
```

The API address is `http://localhost:53607` (Android emulator: `http://10.0.2.2:53607`);
override it with `--dart-define=API_BASE_URL=http://<pc-ip>:53607`.
