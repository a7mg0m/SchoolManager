# Push notifications (Firebase Cloud Messaging)

The parent, teacher and driver apps receive push notifications for:

- **School notifications** created in school-admin (Notifications → new). The push goes to the notification's
  recipients: everyone (no targets), the parents of a grade / class / student, or a teacher.
  A notification dated in the future is pushed on that day (background job, every 15 minutes).
- **Chat messages**: the other participants of the chat get the sender's name and the first 100 characters.

Tapping a notification opens the app on the notifications list or the chats (parent / teacher apps).

Until Firebase is configured, everything works as before and nothing is pushed (the server logs
"Push notifications are disabled", the apps skip push).

## 1. Create the Firebase project

1. <https://console.firebase.google.com> → **Add project** (one project for all three apps).
2. Add the apps (Project settings → General → **Add app**):

   | App | Android package | iOS bundle id |
   |---|---|---|
   | Parent | `com.schoolmanager.smart_school_parent` | as set in Xcode (Runner → Bundle Identifier) |
   | Teacher | `com.schoolmanager.smart_school_teacher` | as set in Xcode |
   | Driver | `com.schoolmanager.smart_school_driver` | as set in Xcode |

3. iOS only: Project settings → **Cloud Messaging** → upload an **APNs authentication key** (.p8) from your
   Apple developer account.

## 2. Files to put in place (never commit them — they are in `.gitignore`)

| File | Where |
|---|---|
| `google-services.json` (one per Android app) | `MobileApps/<App>/android/app/google-services.json` |
| `GoogleService-Info.plist` (one per iOS app) | `MobileApps/<App>/ios/Runner/GoogleService-Info.plist` (add it to the Runner target in Xcode) |
| Service-account key (Project settings → Service accounts → **Generate new private key**) | anywhere on the server, e.g. `D:\secrets\firebase-service-account.json` |

The Android build applies the Google Services Gradle plugin only when `google-services.json` exists, so the apps
still build without it.

Optional (recommended by Firebase): run `flutterfire configure` in each app and pass
`DefaultFirebaseOptions.currentPlatform` to `Firebase.initializeApp(...)` in `lib/core/push/push_service.dart`
(both places are marked `TODO(firebase)`). This is required for push on the web; the apps skip push on the web today.

## 3. Server configuration

`Services/Notification/SchoolManager.Notification.API/appsettings.json` (or user secrets / environment variable
`Firebase__CredentialsPath`):

```json
"Firebase": {
  "CredentialsPath": "D:\\secrets\\firebase-service-account.json"
}
```

Restart the Notification service. Its log shows `Push notifications are enabled (Firebase project …)`.

Other keys used:

- `ManagementService:BaseUrl` / `IdentityService:BaseUrl`: the gateway (`http://localhost:53607/`).
- `Jwt`: the Notification service signs a short-lived service token with the shared Jwt settings to ask the
  Management service for the parents of a grade / class / student (also from the background job).

## 4. iOS project settings (Xcode, each app)

Runner target → **Signing & Capabilities**:

1. **+ Capability → Push Notifications**.
2. **+ Capability → Background Modes** → tick **Remote notifications** (already in `Info.plist`; the driver app also
   keeps **Location updates**).

## 5. How it works

| Piece | Where |
|---|---|
| Device tokens table `UserDeviceTokens` (migration `AddUserDeviceTokens`) | Notification service |
| `POST /api/devices/register` `{ token, platform: 1 Android / 2 iOS / 3 Web, appName: parent / teacher / driver }` | `DevicesController` (the user comes from the login token) |
| `DELETE /api/devices/{token}` (URL-encoded) | called by the apps at logout |
| Recipients of a notification | `NotificationRecipientResolver` (+ Management `POST /api/student/notification-parents`) |
| Sending (batches of 500, dead tokens deleted) | `FcmPushNotificationSender` |
| Due future notifications | `DuePushNotificationsService` (marks `Notifications.PushSentAt`) |
| App side | `lib/core/push/push_service.dart` in each app |

User ids: a parent's user id is the parent id, a teacher's is the teacher id, a driver's is the bus id.

## 6. Test

1. Configure Firebase (steps 1–3), restart the Notification, Management and Gateway services
   (Kafka is not needed for push).
2. Install an app on a real phone (push does not work on most emulators without Google Play):
   `flutter run -d <device> --dart-define=API_BASE_URL=http://localhost:53607` (with `adb reverse tcp:53607 tcp:53607`).
   On this machine C: is full: build with `GRADLE_USER_HOME=D:\gradle-home`.
3. Sign in and allow notifications. Check the token reached the server:
   `SELECT UserId, UserType, AppName, LastSeenAt FROM SchoolManagerNotification.dbo.UserDeviceTokens`.
4. In school-admin create a notification for everyone (or the child's class) dated **today** → the phone shows it.
   Put the app in the background / close it and repeat.
5. Send a chat message from the other app (teacher ↔ parent) → the receiver gets "sender name: message".
6. Sign out → the token row disappears.

Troubleshooting: the Notification service logs each push (`Push "title": N delivered, M failed`); tokens rejected as
`Unregistered` / `InvalidArgument` are deleted automatically.
