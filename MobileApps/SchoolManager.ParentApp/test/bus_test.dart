import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:smart_school_parent/features/bus/bus_screen.dart';
import 'package:smart_school_parent/features/bus/bus_service.dart';
import 'package:smart_school_parent/features/children/children_service.dart';

const _child = Child(
  id: 'c1',
  fullNameAr: 'سارة أحمد',
  fullNameEn: 'Sara Ahmed',
  gender: 'Female',
  stageName: 'ابتدائي',
  gradeName: 'الصف الثالث',
  sectionName: 'أ',
);

void _phoneSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.6;
  addTearDown(tester.view.reset);
}

Widget _app(Widget screen) => MaterialApp(home: Directionality(textDirection: TextDirection.rtl, child: screen));

final _now = DateTime(2026, 11, 23, 7, 30);

const _trip = CurrentTrip(
  tripId: 't1',
  direction: TripDirection.toSchool,
  busNumber: 'B-01',
  plateNumber: 'ABC 1234',
  driverName: 'محمود علي',
  driverPhone: '0100000000',
  riderStatus: RiderStatus.boarded,
  latitude: 30.05,
  longitude: 31.24,
);

void main() {
  testWidgets('Current trip: bus, driver, the child on the bus, bus and home on the map', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(BusScreen(
      child: _child.withLocation(30.0505, 31.2405),
      now: _now,
      showTiles: false,
      refreshEvery: null,
      api: BusApi(loadCurrentTrip: (_) async => _trip, loadTrips: (id, f, t) async => const []),
    )));
    await tester.pumpAndSettle();

    expect(find.text('باص B-01 · إلى المدرسة'), findsOneWidget);
    expect(find.text('السائق: محمود علي'), findsOneWidget);
    expect(find.byKey(const ValueKey('bus-call-driver')), findsOneWidget);
    expect(find.descendant(of: find.byKey(const ValueKey('bus-rider-status')), matching: find.text('في الباص')), findsOneWidget);
    expect(find.byKey(const ValueKey('bus-pin')), findsOneWidget);
    expect(find.byKey(const ValueKey('home-pin')), findsOneWidget);
  });

  testWidgets('No running trip says so; trips of the last 30 days are listed', (WidgetTester tester) async {
    _phoneSize(tester);
    String? range;
    await tester.pumpWidget(_app(BusScreen(
      child: _child,
      now: _now,
      showTiles: false,
      refreshEvery: null,
      api: BusApi(
        loadCurrentTrip: (_) async => null,
        loadTrips: (id, from, to) async {
          range = '${busDate(from)} ${busDate(to)}';
          return [
            ChildTrip(tripId: 'a', date: DateTime(2026, 11, 22), direction: TripDirection.toHome, state: TripState.completed,
                busNumber: 'B-01', riderStatus: RiderStatus.droppedOff),
            ChildTrip(tripId: 'b', date: DateTime(2026, 11, 22), direction: TripDirection.toSchool, state: TripState.completed,
                busNumber: 'B-01', riderStatus: RiderStatus.absent),
          ];
        },
      ),
    )));
    await tester.pumpAndSettle();

    expect(find.byKey(const ValueKey('bus-no-trip')), findsOneWidget);
    expect(range, '2026-10-24 2026-11-23');

    await tester.tap(find.byKey(const ValueKey('bus-tab-trips')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('bus-trip-a')), findsOneWidget);
    expect(find.byKey(const ValueKey('bus-trip-b')), findsOneWidget);
    expect(find.text('غائب'), findsOneWidget);
    expect(find.text('نزل'), findsOneWidget);
  });

  testWidgets('Home location: tap the map to place the pin, save it; remove it', (WidgetTester tester) async {
    _phoneSize(tester);
    final saved = <String>[];
    Child? updated;
    await tester.pumpWidget(_app(BusScreen(
      child: _child,
      now: _now,
      showTiles: false,
      refreshEvery: null,
      onLocationSaved: (c) => updated = c,
      api: BusApi(
        loadCurrentTrip: (_) async => null,
        loadTrips: (id, f, t) async => const [],
        saveLocation: (id, lat, lng) async => saved.add('$id ${lat != null} ${lng != null}'),
      ),
    )));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('bus-tab-home')));
    await tester.pumpAndSettle();

    expect(find.text('لم يتم تحديد موقع المنزل بعد.'), findsOneWidget);
    // Nothing placed yet: save is disabled
    expect(tester.widget<FilledButton>(find.byKey(const ValueKey('home-location-save'))).onPressed, isNull);

    // Tap the map: the pin is placed
    await tester.tapAt(tester.getCenter(find.byType(FlutterMap)));
    // The map waits to tell a tap from a double tap
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('home-location-pin')), findsOneWidget);
    expect(find.textContaining('الموقع الجديد'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('home-location-save')));
    await tester.pumpAndSettle();
    expect(saved, ['c1 true true']);
    expect(updated!.hasLocation, isTrue);
    expect(find.text('تم حفظ موقع المنزل.'), findsOneWidget);
    expect(find.textContaining('الموقع المحفوظ'), findsOneWidget);

    // Remove it (after the message closes; it covers the buttons)
    await tester.pump(const Duration(seconds: 5));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('home-location-clear')));
    await tester.pumpAndSettle();
    expect(saved.last, 'c1 false false');
    expect(updated!.hasLocation, isFalse);
  });

  testWidgets('Home location from the phone: the pin goes to the current position, then it is saved', (WidgetTester tester) async {
    _phoneSize(tester);
    final saved = <String>[];
    await tester.pumpWidget(_app(BusScreen(
      child: _child,
      now: _now,
      showTiles: false,
      refreshEvery: null,
      api: BusApi(
        loadCurrentTrip: (_) async => null,
        loadTrips: (id, f, t) async => const [],
        loadPhonePosition: () async => (30.123456, 31.654321),
        saveLocation: (id, lat, lng) async => saved.add('$id $lat $lng'),
      ),
    )));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('bus-tab-home')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('home-location-mine')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('home-location-pin')), findsOneWidget);
    expect(find.text('الموقع الجديد: 30.123456, 31.654321'), findsOneWidget);
    expect(saved, isEmpty); // not saved until the parent confirms

    await tester.pump(const Duration(seconds: 5));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('home-location-save')));
    await tester.pumpAndSettle();
    expect(saved, ['c1 30.123456 31.654321']);
  });

  testWidgets('Location off or not allowed: the message is shown and nothing changes', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(BusScreen(
      child: _child,
      now: _now,
      showTiles: false,
      refreshEvery: null,
      api: BusApi(
        loadCurrentTrip: (_) async => null,
        loadTrips: (id, f, t) async => const [],
        loadPhonePosition: () async => throw const BusException('خدمة الموقع مغلقة في الهاتف. افتحها ثم حاول مرة أخرى.'),
      ),
    )));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('bus-tab-home')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('home-location-mine')));
    await tester.pumpAndSettle();
    expect(find.text('خدمة الموقع مغلقة في الهاتف. افتحها ثم حاول مرة أخرى.'), findsOneWidget);
    expect(find.byKey(const ValueKey('home-location-pin')), findsNothing);
  });

  test('JSON: current trip and trips (UTC times, enums as numbers)', () {
    final t = CurrentTrip.fromJson({
      'tripId': 't', 'type': 1, 'startedAt': '2026-11-23T05:00:00', 'busNumber': 'B-2', 'plateNumber': 'X', 'driverName': 'D',
      'driverPhone': '1', 'currentLatitude': 30.1, 'currentLongitude': 31.2, 'lastLocationAt': '2026-11-23T05:10:00', 'studentStatus': 2,
    });
    expect(t.direction, TripDirection.toHome);
    expect(t.riderStatus, RiderStatus.droppedOff);
    expect(t.hasPosition, isTrue);
    expect(t.startedAt, DateTime.utc(2026, 11, 23, 5).toLocal());

    final c = ChildTrip.fromJson({'tripId': 'x', 'date': '2026-11-20', 'type': 0, 'status': 3, 'busNumber': 'B', 'studentStatus': 3});
    expect(c.state, TripState.cancelled);
    expect(c.riderStatus, RiderStatus.absent);
    expect(c.date, DateTime(2026, 11, 20));

    final child = Child.fromJson({'id': 'c', 'latitude': 30.5, 'longitude': 31.5});
    expect(child.hasLocation, isTrue);
    expect(child.withLocation(null, null).hasLocation, isFalse);
  });
}

