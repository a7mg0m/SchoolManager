import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:smart_school_parent/features/children/children_service.dart';
import 'package:smart_school_parent/features/clinic/clinic_screen.dart';
import 'package:smart_school_parent/features/clinic/clinic_service.dart';

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

/// The clinic page opened from a first page, as from Home
Widget _app(ClinicScreen screen) => MaterialApp(
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: TextButton(
                onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => screen)),
                child: const Text('open'),
              ),
            ),
          ),
        ),
      ),
    );

Future<void> _open(WidgetTester tester, ClinicScreen screen) async {
  await tester.pumpWidget(_app(screen));
  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('Clinic shows the child health profile', (WidgetTester tester) async {
    _phoneSize(tester);
    String? askedFor;
    await _open(
      tester,
      ClinicScreen(
        child: _child,
        loadProfile: (id) async {
          askedFor = id;
          return const MedicalProfile(bloodType: 'O+', allergies: 'الفول السوداني', emergencyPhone: '0100000000');
        },
      ),
    );

    expect(askedFor, 'c1');
    expect(find.text('O+'), findsOneWidget);
    expect(find.text('الفول السوداني'), findsOneWidget);
    expect(find.text('0100000000'), findsOneWidget);
    // Empty fields read "none"
    expect(find.text('لا يوجد'), findsNWidgets(3));
  });

  testWidgets('Clinic without a profile offers to add one, then shows what was saved', (WidgetTester tester) async {
    _phoneSize(tester);
    MedicalProfile? stored;
    MedicalProfile? sent;
    await _open(
      tester,
      ClinicScreen(
        child: _child,
        loadProfile: (_) async => stored,
        saveProfile: (id, profile) async {
          sent = profile;
          stored = profile;
        },
      ),
    );

    expect(find.textContaining('لا يوجد ملف صحي'), findsOneWidget);
    await tester.tap(find.text('إضافة الملف الصحي'));
    await tester.pumpAndSettle();

    // Blood type from the list
    await tester.tap(find.text('غير محدد'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('A-').last);
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'الحساسية'), 'البنسلين');
    await tester.enterText(find.widgetWithText(TextFormField, 'هاتف الطوارئ'), '0100 123 4567');
    await tester.ensureVisible(find.text('حفظ'));
    await tester.tap(find.text('حفظ'));
    await tester.pumpAndSettle();

    expect(sent?.bloodType, 'A-');
    expect(sent?.allergies, 'البنسلين');
    // Back on the clinic page, reloaded
    expect(find.text('A-'), findsOneWidget);
    expect(find.text('البنسلين'), findsOneWidget);
    expect(find.text('تم حفظ الملف الصحي.'), findsOneWidget);
  });

  testWidgets('Edit health profile rejects a too-short emergency phone', (WidgetTester tester) async {
    _phoneSize(tester);
    var saved = false;
    await _open(
      tester,
      ClinicScreen(
        child: _child,
        loadProfile: (_) async => const MedicalProfile(bloodType: 'B+'),
        saveProfile: (id, profile) async => saved = true,
      ),
    );

    await tester.tap(find.byIcon(Icons.edit_rounded).first);
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'هاتف الطوارئ'), '12');
    await tester.ensureVisible(find.text('حفظ'));
    await tester.tap(find.text('حفظ'));
    await tester.pumpAndSettle();

    expect(find.text('رقم الهاتف غير صحيح'), findsOneWidget);
    expect(saved, isFalse);
  });

  testWidgets('Clinic tabs show the vaccinations and the clinic visits', (WidgetTester tester) async {
    _phoneSize(tester);
    await _open(
      tester,
      ClinicScreen(
        child: _child,
        loadProfile: (_) async => null,
        loadVaccinations: (_) async => [
          ChildVaccination(
              id: 'v1', vaccineName: 'الحصبة', doseNumber: 2, status: VaccinationState.given, date: DateTime(2026, 10, 5)),
          const ChildVaccination(id: 'v2', vaccineName: 'شلل الأطفال', doseNumber: 1, status: VaccinationState.parentRefused),
        ],
        loadVisits: (_) async => [
          ClinicVisit(
            id: 'cv1',
            date: DateTime(2026, 11, 12, 9, 30),
            complaint: 'صداع',
            doctorName: 'د. منى',
            temperature: 38.5,
            diagnosis: 'نزلة برد',
            outcome: VisitResult.sentHome,
            parentNotified: true,
            medications: const [('باراسيتامول', '250 مجم')],
          ),
        ],
      ),
    );

    // Counts on the tabs
    expect(find.text('التطعيمات (2)'), findsOneWidget);
    expect(find.text('الزيارات (1)'), findsOneWidget);

    await tester.tap(find.text('التطعيمات (2)'));
    await tester.pumpAndSettle();
    expect(find.text('الحصبة'), findsOneWidget);
    expect(find.text('تم التطعيم'), findsOneWidget);
    expect(find.text('رفض ولي الأمر'), findsOneWidget);
    expect(find.textContaining('الجرعة 2'), findsOneWidget);

    await tester.tap(find.text('الزيارات (1)'));
    await tester.pumpAndSettle();
    expect(find.text('صداع'), findsOneWidget);
    expect(find.text('أُرسل إلى المنزل'), findsOneWidget);
    expect(find.textContaining('نزلة برد'), findsOneWidget);
    expect(find.textContaining('باراسيتامول'), findsOneWidget);
    expect(find.text('تم إبلاغ ولي الأمر'), findsOneWidget);
  });

  testWidgets('Clinic tabs without records show a message', (WidgetTester tester) async {
    _phoneSize(tester);
    await _open(
      tester,
      ClinicScreen(
        child: _child,
        loadProfile: (_) async => null,
        loadVaccinations: (_) async => const [],
        loadVisits: (_) async => const [],
      ),
    );

    await tester.tap(find.text('التطعيمات'));
    await tester.pumpAndSettle();
    expect(find.text('لا توجد تطعيمات مسجلة للطالب.'), findsOneWidget);

    await tester.tap(find.text('الزيارات'));
    await tester.pumpAndSettle();
    expect(find.text('لم يزر الطالب العيادة بعد.'), findsOneWidget);
  });

  test('Vaccination and visit JSON: numeric enums, names in the app language, medications', () {
    final v = ChildVaccination.fromJson({
      'id': 'a',
      'vaccineNameAr': 'الحصبة',
      'vaccineNameEn': 'Measles',
      'doseNumber': 2,
      'status': 4,
      'vaccinationDate': '2026-10-05',
      'place': 2,
    });
    expect(v.vaccineName, 'الحصبة');
    expect(v.status, VaccinationState.parentRefused);
    expect(v.date, DateTime(2026, 10, 5));
    expect(v.atSchool, isFalse);

    final visit = ClinicVisit.fromJson({
      'id': 'b',
      'visitDate': '2026-11-12T09:30:00',
      'complaint': 'صداع',
      'temperature': 38.5,
      'outcome': 'Referred',
      'medications': [
        {'medicineName': 'باراسيتامول', 'dose': '250 مجم'},
      ],
    });
    expect(visit.date, DateTime(2026, 11, 12, 9, 30));
    expect(visit.temperature, 38.5);
    expect(visit.outcome, VisitResult.referred);
    expect(visit.medications, [('باراسيتامول', '250 مجم')]);
  });

  test('Empty fields are sent as null', () {
    final json = const MedicalProfile(bloodType: 'O+', allergies: '  ').toJson();
    expect(json['bloodType'], 'O+');
    expect(json['allergies'], isNull);
    expect(json['notes'], isNull);
  });
}
