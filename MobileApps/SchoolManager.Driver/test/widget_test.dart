import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_driver/core/auth/auth_service.dart';
import 'package:smart_school_driver/core/i18n/app_language.dart';
import 'package:smart_school_driver/features/home/home_screen.dart';
import 'package:smart_school_driver/features/notifications/notifications_screen.dart';
import 'package:smart_school_driver/features/notifications/notifications_service.dart';
import 'package:smart_school_driver/features/profile/profile_service.dart';
import 'dart:async';

import 'package:smart_school_driver/features/trips/location_tracker.dart';
import 'package:smart_school_driver/features/trips/trip_map_screen.dart';
import 'package:smart_school_driver/features/trips/trip_screens.dart';
import 'package:smart_school_driver/features/trips/trip_service.dart';

void _phoneSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.6;
  addTearDown(tester.view.reset);
}

/// A JWT with the given payload (not signed: the app only reads it)
String _jwt(Map<String, dynamic> payload) {
  String part(Map<String, dynamic> m) => base64Url.encode(utf8.encode(jsonEncode(m))).replaceAll('=', '');
  return '${part({'alg': 'HS256'})}.${part(payload)}.sig';
}

const _bus = DriverBus(
  id: 'bus-1',
  number: '12',
  plateNumber: 'ABC 123',
  capacity: 30,
  routeNameAr: 'خط النخيل',
  routeNameEn: 'Palm route',
  stops: [RouteStop(nameAr: 'المحطة الأولى', nameEn: 'First stop', order: 1)],
);

Trip _trip(String id, TripType type, TripStatus status, {DateTime? date, int students = 3}) => Trip(
      id: id,
      date: date ?? DateTime.now(),
      type: type,
      status: status,
      studentStatuses: List.filled(students, TripStudentStatus.pending),
    );

/// A tracker without GPS: positions come from [positions], sent ones are collected in [sent]
LocationTracker _tracker({StreamController<(double, double)>? positions, List<String>? sent, Duration? minInterval}) =>
    LocationTracker(
      positions: () => (positions ?? StreamController<(double, double)>()).stream,
      ensurePermission: () async {},
      send: (tripId, lat, lng) async => sent?.add('$tripId:$lat,$lng'),
      minInterval: minInterval ?? const Duration(seconds: 15),
      // No resend timer: widget tests must end without pending timers
      heartbeat: null,
    );

Widget _app(Widget home) => MaterialApp(
      locale: AppLanguage.instance.value,
      supportedLocales: AppLanguage.supported,
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: home,
    );

void main() {
  // The tracker watches the app lifecycle (back from the background)
  TestWidgetsFlutterBinding.ensureInitialized();
  setUp(() => SharedPreferences.setMockInitialValues({}));

  group('Session', () {
    test('the bus id comes from the profile, else from the bus_id claim of the token', () {
      final fromProfile = AuthSession.fromLoginResult({
        'userId': 'u1',
        'loginName': 'driver1',
        'token': _jwt({'exp': 4102444800}),
        'userProfile': {'firstName': 'Ali', 'busId': 'bus-1'},
      });
      expect(fromProfile.busId, 'bus-1');
      expect(fromProfile.isDriver, isTrue);

      final fromToken = AuthSession.fromLoginResult({
        'userId': 'u1',
        'loginName': 'driver1',
        'token': _jwt({'exp': 4102444800, 'bus_id': 'bus-2'}),
        'userProfile': {'firstName': 'Ali'},
      });
      expect(fromToken.busId, 'bus-2');

      final teacher = AuthSession.fromLoginResult({
        'userId': 'u2',
        'loginName': 'teacher1',
        'token': _jwt({'exp': 4102444800, 'teacher_id': 't-1'}),
        'userProfile': {'teacherId': 't-1'},
      });
      expect(teacher.isDriver, isFalse);
      // Kept on the device with the bus id
      expect(AuthSession.fromJson(fromToken.toJson()).busId, 'bus-2');
    });
  });

  group('Trip JSON', () {
    test('type, status, UTC times and students', () {
      final trip = Trip.fromJson({
        'id': 't1',
        'date': '2026-11-24',
        'type': 1,
        'status': 1,
        'startedAt': '2026-11-24T06:30:00',
        'endedAt': null,
        'students': [
          {'status': 0},
          {'status': 1},
          {'status': 1},
          {'status': 3},
        ],
      });
      expect(trip.type, TripType.toHome);
      expect(trip.status, TripStatus.inProgress);
      expect(trip.canComplete, isTrue);
      expect(trip.canStart, isFalse);
      expect(trip.startedAt, DateTime.utc(2026, 11, 24, 6, 30).toLocal());
      expect(trip.students, 4);
      expect(trip.count(TripStudentStatus.boarded), 2);
      expect(trip.count(TripStudentStatus.absent), 1);
    });

    test('bus with its route stops in order', () {
      final bus = DriverBus.fromJson({
        'bus': {'id': 'b1', 'number': '7', 'plateNumber': 'XYZ', 'capacity': 25, 'isActive': true},
        'route': {
          'nameAr': 'خط',
          'nameEn': 'Line',
          'stops': [
            {'nameAr': 'ب', 'nameEn': 'B', 'order': 2},
            {'nameAr': 'أ', 'nameEn': 'A', 'order': 1},
          ],
        },
      });
      expect(bus.number, '7');
      expect(bus.capacity, 25);
      expect(bus.stops.map((s) => s.nameEn), ['A', 'B']);
    });
  });

  testWidgets('Home shows the bus and today\'s trips; a started trip opens its student list', (tester) async {
    _phoneSize(tester);
    var trips = [
      _trip('t1', TripType.toSchool, TripStatus.planned, students: 5),
      _trip('t2', TripType.toHome, TripStatus.planned),
    ];
    final started = <String>[];

    await tester.pumpWidget(_app(HomeScreen(
      tracker: _tracker(),
      loadBus: () async => _bus,
      loadTrips: () async => trips,
      loadProfile: () async => const MyProfile(firstName: 'علي', lastName: 'حسن', loginName: 'driver1'),
      startTrip: (id) async {
        started.add(id);
        trips = [_trip('t1', TripType.toSchool, TripStatus.inProgress, students: 5), trips[1]];
      },
      loadTrip: (id) async => trips.firstWhere((t) => t.id == id),
      loadStudents: (_) async => const [
        TripRider(studentId: 's1', status: TripStudentStatus.pending, fullNameAr: 'سارة'),
      ],
    )));
    await tester.pumpAndSettle();

    expect(find.text('علي حسن'), findsOneWidget);
    expect(find.text('حافلة رقم 12'), findsOneWidget);
    expect(find.text('رحلات اليوم'), findsOneWidget);
    expect(find.text('رحلة الذهاب إلى المدرسة'), findsOneWidget);
    expect(find.text('5 طالب'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('trip-action-t1')));
    await tester.pumpAndSettle();

    expect(started, ['t1']);
    // The trip opens so the driver can mark who boards
    expect(find.text('تفاصيل الرحلة'), findsOneWidget);
    expect(find.text('سارة'), findsOneWidget);
    expect(find.byKey(const ValueKey('board-s1')), findsOneWidget);

    // Back on Today the trip is on the road
    await Navigator.of(tester.element(find.text('تفاصيل الرحلة'))).maybePop();
    await tester.pumpAndSettle();
    expect(find.text('في الطريق'), findsOneWidget);
    expect(find.text('إنهاء الرحلة'), findsOneWidget);
  });

  testWidgets('Finishing a running trip asks first', (tester) async {
    _phoneSize(tester);
    final completed = <String>[];
    await tester.pumpWidget(_app(HomeScreen(
      tracker: _tracker(),
      loadBus: () async => _bus,
      loadTrips: () async => [_trip('t1', TripType.toSchool, TripStatus.inProgress)],
      loadProfile: () async => null,
      completeTrip: (id) async => completed.add(id),
    )));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('trip-action-t1')));
    await tester.pumpAndSettle();
    expect(find.text('هل وصلت؟ سيتم إنهاء الرحلة.'), findsOneWidget);

    await tester.tap(find.text('تراجع'));
    await tester.pumpAndSettle();
    expect(completed, isEmpty);

    await tester.tap(find.byKey(const ValueKey('trip-action-t1')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('تأكيد'));
    await tester.pumpAndSettle();
    expect(completed, ['t1']);
  });

  testWidgets('New trip: pick the type and create it', (tester) async {
    _phoneSize(tester);
    final created = <TripType>[];
    await tester.pumpWidget(_app(HomeScreen(
      tracker: _tracker(),
      loadBus: () async => _bus,
      loadTrips: () async => const [],
      loadProfile: () async => null,
      createTrip: (date, type) async => created.add(type),
    )));
    await tester.pumpAndSettle();

    expect(find.text('لا توجد رحلات اليوم'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('new-trip')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('type-toHome')));
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey('create-trip')));
    await tester.pumpAndSettle();

    expect(created, [TripType.toHome]);
    expect(find.text('تم إنشاء الرحلة.'), findsOneWidget);
  });

  testWidgets('A trip that already exists shows the API error in the sheet', (tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(HomeScreen(
      tracker: _tracker(),
      loadBus: () async => _bus,
      loadTrips: () async => const [],
      loadProfile: () async => null,
      createTrip: (date, type) async => throw const TripException('هذه الرحلة موجودة بالفعل لهذا اليوم.'),
    )));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('new-trip')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('create-trip')));
    await tester.pumpAndSettle();

    expect(find.text('هذه الرحلة موجودة بالفعل لهذا اليوم.'), findsOneWidget);
  });

  group('Students on a trip', () {
    TripRider rider(String id, String name, TripStudentStatus status) => TripRider(
          studentId: id,
          status: status,
          fullNameAr: name,
          gradeNameAr: 'الصف الأول',
          sectionNameAr: 'أ',
          stopNameAr: 'المحطة الأولى',
          parentPhone: '0500000000',
        );

    test('rider JSON', () {
      final r = TripRider.fromJson({
        'studentId': 's1',
        'status': 1,
        'fullNameAr': 'سارة',
        'fullNameEn': 'Sara',
        'gender': 'Female',
        'gradeNameEn': 'Grade 1',
        'sectionNameEn': 'A',
        'parentPhone': '0555',
        'stopNameEn': 'Gate',
        'boardedAt': '2026-11-24T06:00:00',
      });
      expect(r.status, TripStudentStatus.boarded);
      expect(r.isFemale, isTrue);
      expect(r.boardedAt, DateTime.utc(2026, 11, 24, 6).toLocal());
      expect(r.parentPhone, '0555');
    });

    Future<void> openTrip(WidgetTester tester, TripType type, List<TripRider> riders, List<String> marks,
        {List<String>? calls}) async {
      await tester.pumpWidget(_app(TripDetailsScreen(
        tracker: _tracker(),
        showMapTiles: false,
        tripId: 't1',
        loadTrip: (_) async => _trip('t1', type, TripStatus.inProgress, students: riders.length),
        loadStudents: (_) async => riders,
        markStudent: (tripId, studentId, status) async => marks.add('$studentId:${status.name}'),
        callPhone: (phone) async => calls?.add(phone),
      )));
      await tester.pumpAndSettle();
    }

    testWidgets('to school: the student boards, then arrives at school', (tester) async {
      _phoneSize(tester);
      final marks = <String>[];
      final calls = <String>[];
      await openTrip(tester, TripType.toSchool, [rider('s1', 'سارة', TripStudentStatus.pending)], marks, calls: calls);

      expect(find.text('سارة'), findsOneWidget);
      expect(find.text('الصف الأول · أ · المحطة الأولى'), findsOneWidget);
      expect(find.text('بانتظار الركوب'), findsWidgets);

      await tester.ensureVisible(find.byKey(const ValueKey('board-s1')));
      await tester.tap(find.byKey(const ValueKey('board-s1')));
      await tester.pumpAndSettle();
      expect(marks, ['s1:boarded']);
      expect(find.byKey(const ValueKey('arrive-s1')), findsOneWidget);
      expect(find.text('وصل المدرسة'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('arrive-s1')));
      await tester.pump(const Duration(seconds: 5));
      await tester.pumpAndSettle();
      expect(marks, ['s1:boarded', 's1:droppedOff']);
      expect(find.byKey(const ValueKey('arrive-s1')), findsNothing);

      await tester.tap(find.byTooltip('الاتصال بولي الأمر'));
      await tester.pumpAndSettle();
      expect(calls, ['0500000000']);
    });

    testWidgets('to home: "arrived home"; a waiting student can be absent', (tester) async {
      _phoneSize(tester);
      final marks = <String>[];
      await openTrip(tester, TripType.toHome, [
        rider('s1', 'سارة', TripStudentStatus.boarded),
        rider('s2', 'عمر', TripStudentStatus.pending),
      ], marks);

      expect(find.text('وصل المنزل'), findsOneWidget);
      await tester.ensureVisible(find.byKey(const ValueKey('absent-s2')));
      await tester.tap(find.byKey(const ValueKey('absent-s2')));
      await tester.pumpAndSettle();
      expect(marks, ['s2:absent']);
      expect(find.byKey(const ValueKey('board-s2')), findsNothing);
    });

    testWidgets('a planned trip lists the students without buttons', (tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(_app(TripDetailsScreen(
        tracker: _tracker(),
        showMapTiles: false,
        tripId: 't1',
        loadTrip: (_) async => _trip('t1', TripType.toSchool, TripStatus.planned, students: 1),
        loadStudents: (_) async => [rider('s1', 'سارة', TripStudentStatus.pending)],
      )));
      await tester.pumpAndSettle();

      expect(find.text('سارة'), findsOneWidget);
      expect(find.text('ابدأ الرحلة لتسجيلهم'), findsOneWidget);
      expect(find.byKey(const ValueKey('board-s1')), findsNothing);
    });
  });

  group('Bus location', () {
    test('the first position is sent at once, the next ones at most every interval', () async {
      final positions = StreamController<(double, double)>();
      final sent = <String>[];
      final tracker = _tracker(positions: positions, sent: sent, minInterval: const Duration(hours: 1));

      await tracker.start('t1');
      expect(tracker.isTracking, isTrue);
      positions.add((24.7, 46.6));
      await Future<void>.delayed(Duration.zero);
      positions.add((24.8, 46.7));
      await Future<void>.delayed(Duration.zero);

      expect(sent, ['t1:24.7,46.6']);
      expect(tracker.last, (24.8, 46.7));
      expect(tracker.lastSentAt, isNotNull);

      // No trip on the road any more: sharing stops
      await tracker.follow([_trip('t1', TripType.toSchool, TripStatus.completed)]);
      expect(tracker.isTracking, isFalse);
      expect(tracker.tripId, isNull);
      await positions.close();
    });

    test('standing still: the last position is sent again; a trip finished elsewhere stops sharing', () async {
      final positions = StreamController<(double, double)>();
      final sent = <String>[];
      var running = true;
      final tracker = LocationTracker(
        positions: () => positions.stream,
        ensurePermission: () async {},
        send: (tripId, lat, lng) async {
          if (!running) throw const TripNotRunningException();
          sent.add('$tripId:$lat,$lng');
        },
        minInterval: Duration.zero,
        heartbeat: const Duration(milliseconds: 40),
      );

      await tracker.start('t1');
      positions.add((24.7, 46.6));
      await Future<void>.delayed(const Duration(milliseconds: 100));
      expect(sent.length, greaterThanOrEqualTo(2)); // first send + heartbeat
      expect(sent.toSet(), {'t1:24.7,46.6'});

      // The office finished the trip: the next send stops the tracker
      running = false;
      await Future<void>.delayed(const Duration(milliseconds: 100));
      expect(tracker.tripId, isNull);
      expect(tracker.isTracking, isFalse);
      expect(tracker.error, isNull);
      await positions.close();
    });

    test('battery: the GPS stops in the background and starts again when the app is opened', () async {
      final positions = StreamController<(double, double)>.broadcast();
      final sent = <String>[];
      final tracker = LocationTracker(
        positions: () => positions.stream,
        ensurePermission: () async {},
        send: (tripId, lat, lng) async => sent.add('$tripId:$lat,$lng'),
        minInterval: Duration.zero,
        heartbeat: null,
      );

      await tracker.start('t1');
      positions.add((24.7, 46.6));
      await Future<void>.delayed(Duration.zero);
      expect(sent, ['t1:24.7,46.6']);

      // In the background: nothing is listened to or sent, the trip is kept
      tracker.didChangeAppLifecycleState(AppLifecycleState.paused);
      expect(tracker.isTracking, isFalse);
      expect(tracker.paused, isTrue);
      expect(tracker.tripId, 't1');
      expect(positions.hasListener, isFalse);
      positions.add((24.8, 46.7));
      await Future<void>.delayed(Duration.zero);
      expect(sent.length, 1);

      // Opened again: sharing resumes for the same trip
      tracker.didChangeAppLifecycleState(AppLifecycleState.resumed);
      await Future<void>.delayed(Duration.zero);
      expect(tracker.isTracking, isTrue);
      expect(tracker.paused, isFalse);
      expect(tracker.tripId, 't1');
      positions.add((24.9, 46.8));
      await Future<void>.delayed(Duration.zero);
      expect(sent.last, 't1:24.9,46.8');

      tracker.stop();
      await positions.close();
    });

    test('follow() shares the running trip; a refused permission is shown', () async {
      final tracker = LocationTracker(
        positions: () => const Stream.empty(),
        ensurePermission: () async => throw const TripException('الموقع مرفوض'),
        send: (_, _, _) async {},
      );
      await tracker.follow([
        _trip('a', TripType.toSchool, TripStatus.completed),
        _trip('b', TripType.toHome, TripStatus.inProgress),
      ]);
      expect(tracker.tripId, 'b');
      expect(tracker.isTracking, isFalse);
      expect(tracker.error, 'الموقع مرفوض');
    });

    testWidgets('Today shows that the position is shared', (tester) async {
      _phoneSize(tester);
      final positions = StreamController<(double, double)>();
      final sent = <String>[];
      await tester.pumpWidget(_app(HomeScreen(
        tracker: _tracker(positions: positions, sent: sent),
        loadBus: () async => _bus,
        loadTrips: () async => [_trip('t1', TripType.toSchool, TripStatus.inProgress)],
        loadProfile: () async => null,
      )));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('tracking-banner')), findsOneWidget);

      positions.add((24.7, 46.6));
      await tester.pumpAndSettle();
      expect(sent, ['t1:24.7,46.6']);
      expect(find.textContaining('يتم مشاركة موقع الحافلة تلقائياً'), findsOneWidget);
      await positions.close();
    });
  });

  group('Trip map', () {
    TripRider home(String id, String name, double? lat, double? lng) => TripRider(
          studentId: id,
          status: TripStudentStatus.pending,
          fullNameAr: name,
          latitude: lat,
          longitude: lng,
        );

    testWidgets('school, the students\' homes in order, and those without a location', (tester) async {
      _phoneSize(tester);
      final directions = <String>[];
      await tester.pumpWidget(_app(TripMapScreen(
        trip: _trip('t1', TripType.toHome, TripStatus.planned),
        riders: [
          home('s1', 'سارة', 24.71, 46.67),
          home('s2', 'عمر', 24.73, 46.69),
          home('s3', 'ليلى', null, null),
        ],
        loadSchool: () async => const SchoolPlace(nameAr: 'مدرسة النور', latitude: 24.70, longitude: 46.65),
        tracker: _tracker(),
        showTiles: false,
        openDirections: (lat, lng) async => directions.add('$lat,$lng'),
      )));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('school-pin')), findsOneWidget);
      expect(find.byKey(const ValueKey('student-pin-s1')), findsOneWidget);
      expect(find.byKey(const ValueKey('student-pin-s2')), findsOneWidget);
      expect(find.byKey(const ValueKey('student-pin-s3')), findsNothing);
      expect(find.byKey(const ValueKey('missing-locations')), findsOneWidget);
      expect(find.byKey(const ValueKey('total-km')), findsOneWidget);

      // On the way home the school comes first in the list
      final school = tester.getTopLeft(find.text('مدرسة النور')).dy;
      final sara = tester.getTopLeft(find.text('سارة')).dy;
      expect(school, lessThan(sara));

      await tester.tap(find.byTooltip('الاتجاهات').at(1));
      await tester.pumpAndSettle();
      expect(directions, ['24.71,46.67']);
    });

    testWidgets('the trip screen opens the map', (tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(_app(TripDetailsScreen(
        tripId: 't1',
        tracker: _tracker(),
        showMapTiles: false,
        loadTrip: (_) async => _trip('t1', TripType.toSchool, TripStatus.planned, students: 1),
        loadStudents: (_) async => [home('s1', 'سارة', 24.71, 46.67)],
        loadSchool: () async => const SchoolPlace(latitude: 24.70, longitude: 46.65),
      )));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('open-map')));
      await tester.pumpAndSettle();
      expect(find.text('خريطة الرحلة'), findsOneWidget);
      expect(find.byKey(const ValueKey('student-pin-s1')), findsOneWidget);
    });
  });

  testWidgets('The bottom bar stays on pages opened from the tabs; a tab closes them', (tester) async {
    _phoneSize(tester);
    final trip = _trip('t1', TripType.toSchool, TripStatus.completed);
    await tester.pumpWidget(_app(HomeScreen(
      tracker: _tracker(),
      loadBus: () async => _bus,
      loadTrips: () async => [trip],
      loadProfile: () async => null,
      loadTrip: (_) async => trip,
      loadStudents: (_) async => const [],
    )));
    await tester.pumpAndSettle();

    // Open the trip: its page shows with the bar under it
    await tester.tap(find.text('رحلة الذهاب إلى المدرسة'));
    await tester.pumpAndSettle();
    expect(find.text('تفاصيل الرحلة'), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(find.text('الإشعارات'), findsOneWidget);

    // A tab closes the page and opens the tab
    await tester.tap(find.text('الرحلات').last);
    await tester.pumpAndSettle();
    expect(find.text('تفاصيل الرحلة'), findsNothing);
    expect(find.text('آخر 14 يوماً والأسبوع القادم'), findsOneWidget);
  });

  testWidgets('Notification details: the picture is the header and opens full screen', (tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(NotificationDetailsScreen(
      notice: AppNotice(
        id: 'n1',
        title: 'رحلة مدرسية',
        description: 'تفاصيل',
        sentOn: DateTime.now(),
        type: NoticeType.values.last,
        imageUrl: 'http://localhost:53607/notification-files/default/notification.png',
      ),
    )));
    await tester.pump();

    expect(find.byKey(const ValueKey('notice-image')), findsOneWidget);
    expect(find.text('رحلة مدرسية'), findsWidgets);

    await tester.tap(find.byKey(const ValueKey('notice-image')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('notice-image-full')), findsOneWidget);
  });

  testWidgets('Notification details without a picture keeps the coloured header', (tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(NotificationDetailsScreen(
      notice: AppNotice(id: 'n1', title: 'إجازة', description: 'تفاصيل', sentOn: DateTime.now(), type: NoticeType.values.last),
    )));
    await tester.pump();
    expect(find.byKey(const ValueKey('notice-image')), findsNothing);
    expect(find.text('إجازة'), findsOneWidget);
  });

  testWidgets('Notifications tab: the school\'s and my bus\'s notifications', (tester) async {
    _phoneSize(tester);
    var loads = 0;
    await tester.pumpWidget(_app(HomeScreen(
      tracker: _tracker(),
      loadBus: () async => _bus,
      loadTrips: () async => const [],
      loadProfile: () async => null,
      loadNotifications: () async {
        loads++;
        return [
          AppNotice(id: 'n1', title: 'تغيير خط السير', description: 'تفاصيل', sentOn: DateTime.now(), type: NoticeType.values.last),
          AppNotice(id: 'n2', title: 'إجازة رسمية', description: 'للجميع', sentOn: DateTime.now(), type: NoticeType.values.last, forEveryone: true),
        ];
      },
    )));
    await tester.pumpAndSettle();
    // Not loaded until the tab is opened
    expect(loads, 0);

    await tester.tap(find.text('الإشعارات'));
    await tester.pumpAndSettle();
    expect(loads, 1);
    expect(find.text('تغيير خط السير'), findsOneWidget);
    expect(find.text('إجازة رسمية'), findsOneWidget);
    expect(find.text('لحافلتك'), findsOneWidget);
  });

  testWidgets('Trips tab lists past trips; More tab shows the route and English switches to LTR', (tester) async {
    addTearDown(() => AppLanguage.instance.setLanguage(AppLanguage.arabic));
    _phoneSize(tester);
    await tester.pumpWidget(ListenableBuilder(
      listenable: AppLanguage.instance,
      builder: (context, _) => _app(HomeScreen(
      tracker: _tracker(),
        loadBus: () async => _bus,
        loadTrips: () async => [
          _trip('old', TripType.toHome, TripStatus.completed, date: DateTime.now().subtract(const Duration(days: 2))),
        ],
        loadProfile: () async => null,
      )),
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.text('الرحلات').last);
    await tester.pumpAndSettle();
    expect(find.text('السابقة'), findsOneWidget);
    expect(find.text('مكتملة'), findsOneWidget);

    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    expect(find.text('المحطة الأولى'), findsOneWidget);

    await tester.tap(find.text('English'));
    await tester.pumpAndSettle();
    expect(find.text('First stop'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('First stop'))), TextDirection.ltr);
  });
}
