import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:smart_school_parent/features/children/children_service.dart';
import 'package:smart_school_parent/features/home/home_screen.dart';
import 'package:smart_school_parent/features/profile/edit_profile_screen.dart';
import 'package:smart_school_parent/features/profile/profile_service.dart';
import 'package:smart_school_parent/features/schedule/schedule_service.dart';
import 'package:smart_school_parent/main.dart';

void _phoneSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.6;
  addTearDown(tester.view.reset);
}

Widget _home(
  Future<List<Child>> Function() loader, {
  MyProfile? profile,
  ChildTimetable? timetable,
}) =>
    MaterialApp(
      builder: (context, child) => Directionality(textDirection: TextDirection.rtl, child: child!),
      home: HomeScreen(
        loadChildren: loader,
        loadProfile: () async => profile,
        loadTimetable: (_) async => timetable ?? const ChildTimetable(today: 2, now: 600, days: []),
      ),
    );

ScheduleSlot _slot(String id, String subject, int start, int end) => ScheduleSlot(
      id: id,
      name: 'الحصة $id',
      start: start,
      end: end,
      subject: subject,
      teacher: 'معلم $subject',
    );

/// Week with lessons on Sunday (0) and Tuesday (2)
ChildTimetable _week({required int today, required int now}) => ChildTimetable(
      today: today,
      now: now,
      days: [
        TimetableDay(dayOfWeek: 0, slots: [_slot('s1', 'قرآن', 450, 495)]),
        TimetableDay(dayOfWeek: 2, slots: [
          _slot('t1', 'رياضيات', 480, 525),
          _slot('t2', 'علوم', 570, 615),
          _slot('t3', 'إنجليزي', 630, 675),
        ]),
      ],
    );

Future<void> _openTimetable(WidgetTester tester) async {
  await tester.tap(find.text('الجدول الدراسي'));
  await tester.pumpAndSettle();
}

/// 1x1 transparent PNG
final _png = base64Decode(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=');

const _sara = Child(
  id: '1',
  fullNameAr: 'سارة أحمد',
  fullNameEn: 'Sara Ahmed',
  gender: 'Female',
  stageName: 'الابتدائية',
  gradeName: 'الصف الأول',
  sectionName: 'فصل أ',
);

const _omar = Child(
  id: '2',
  fullNameAr: 'عمر أحمد',
  fullNameEn: 'Omar Ahmed',
  gender: 'Male',
  stageName: 'رياض الأطفال',
  gradeName: 'KG 2',
  sectionName: 'فصل ب',
);

void main() {
  testWidgets('New user: intro -> login, empty form is validated', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(const SmartSchoolParentApp());

    expect(find.text('مرحباً بك في تطبيق ولي الأمر'), findsOneWidget);
    await tester.tap(find.text('تخطي'));
    await tester.pumpAndSettle();

    expect(find.text('تسجيل الدخول'), findsOneWidget);
    await tester.tap(find.text('دخول'));
    await tester.pump();
    expect(find.text('من فضلك أدخل اسم المستخدم'), findsOneWidget);
    expect(find.text('من فضلك أدخل كلمة المرور'), findsOneWidget);
  });

  testWidgets('Home shows the parent\'s children and switches between them', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara, _omar]));
    await tester.pumpAndSettle();

    expect(find.text('سارة أحمد'), findsOneWidget);
    expect(find.text('الصف الأول - فصل أ'), findsOneWidget);

    await tester.tap(find.text('سارة أحمد'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('عمر أحمد').last);
    await tester.pumpAndSettle();

    expect(find.text('KG 2 - فصل ب'), findsOneWidget);
  });

  testWidgets('Home shows the profile name and picture', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(
      () async => [_sara],
      profile: MyProfile(firstName: 'أحمد', lastName: 'جمعة', loginName: 'ahmed', image: _png),
    ));
    await tester.pumpAndSettle();

    expect(find.text('أحمد جمعة'), findsOneWidget);
    final withImage = tester
        .widgetList<CircleAvatar>(find.byType(CircleAvatar))
        .where((a) => a.foregroundImage is MemoryImage);
    expect(withImage, hasLength(1));
  });

  testWidgets('Home without a picture keeps the person icon', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(
      () async => [_sara],
      profile: const MyProfile(firstName: 'أحمد', lastName: 'جمعة', loginName: 'ahmed'),
    ));
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.person), findsOneWidget);
    expect(
      tester.widgetList<CircleAvatar>(find.byType(CircleAvatar)).where((a) => a.foregroundImage != null),
      isEmpty,
    );
  });

  testWidgets('Edit profile saves the changes and keeps a placeholder email', (WidgetTester tester) async {
    _phoneSize(tester);
    Map<String, Object?>? sent;
    const original = MyProfile(
      firstName: 'أحمد',
      lastName: 'جمعة',
      loginName: '0500000000',
      email: 'parent-abc@parents.local',
      phoneNumber: '0500000000',
      address: 'الرياض',
    );

    await tester.pumpWidget(MaterialApp(
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: EditProfileScreen(
          profile: original,
          saveProfile: ({
            required String firstName,
            required String lastName,
            required String email,
            required String phoneNumber,
            required String address,
            image,
            bool removeImage = false,
          }) async {
            sent = {'firstName': firstName, 'email': email, 'address': address};
            return original;
          },
        ),
      ),
    ));

    // The placeholder email is not shown
    expect(find.text('parent-abc@parents.local'), findsNothing);

    await tester.enterText(find.widgetWithText(TextFormField, 'أحمد'), 'محمد');
    await tester.tap(find.text('حفظ التعديلات'));
    await tester.pumpAndSettle();

    expect(sent?['firstName'], 'محمد');
    expect(sent?['email'], 'parent-abc@parents.local'); // kept, not cleared
    expect(find.text('تم حفظ الملف الشخصي.'), findsOneWidget);
  });

  testWidgets('Change password checks the new passwords match', (WidgetTester tester) async {
    _phoneSize(tester);
    var changed = false;
    await tester.pumpWidget(MaterialApp(
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: EditProfileScreen(
          profile: const MyProfile(firstName: 'أحمد', lastName: 'جمعة', loginName: 'ahmed', email: 'a@b.com'),
          changePassword: (current, next) async => changed = true,
        ),
      ),
    ));

    // The password card is further down the (lazily built) list
    await tester.scrollUntilVisible(
      find.text('تأكيد كلمة المرور'),
      300,
      scrollable: find.descendant(of: find.byType(ListView), matching: find.byType(Scrollable)).first,
    );
    Finder field(String label) => find.widgetWithText(TextFormField, label);
    final button = find.widgetWithText(FilledButton, 'تغيير كلمة المرور');

    await tester.enterText(field('كلمة المرور الحالية'), 'old123');
    await tester.enterText(field('كلمة المرور الجديدة'), 'new1234');
    await tester.enterText(field('تأكيد كلمة المرور'), 'other99');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();

    expect(find.text('كلمتا المرور غير متطابقتين'), findsOneWidget);
    expect(changed, isFalse);

    await tester.enterText(field('تأكيد كلمة المرور'), 'new1234');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();

    expect(changed, isTrue);
    expect(find.text('تم تغيير كلمة المرور.'), findsOneWidget);
  });

  testWidgets('Home shows service icons; timetable opens on today with the lesson now', (WidgetTester tester) async {
    _phoneSize(tester);
    // Tuesday 10:00: math finished, science now, English next
    await tester.pumpWidget(_home(() async => [_sara], timetable: _week(today: 2, now: 600)));
    await tester.pumpAndSettle();

    expect(find.text('الخدمات'), findsOneWidget);
    expect(find.text('الواجبات'), findsOneWidget);
    expect(find.text('إعلان هام'), findsOneWidget);
    // The announcement comes before the services
    expect(tester.getTopLeft(find.text('إعلان هام')).dy, lessThan(tester.getTopLeft(find.text('الخدمات')).dy));
    // Only services and the announcement are on home
    for (final removed in ['نسبة الحضور', 'أقرب الأنشطة', 'آخر الواجبات', 'حالة الباص', 'آخر الإشعارات']) {
      expect(find.text(removed), findsNothing);
    }

    await _openTimetable(tester);

    expect(find.text('اليوم - الثلاثاء'), findsOneWidget);
    expect(find.text('الآن'), findsOneWidget);
    expect(find.text('تنتهي بعد 15 دقيقة'), findsOneWidget);
    expect(find.text('الحصة التالية'), findsOneWidget); // English
    expect(find.text('استراحة 15 دقيقة'), findsOneWidget); // 10:15 - 10:30
    expect(find.text('1/3'), findsOneWidget); // lessons done
    expect(find.text('رياضيات'), findsNothing); // finished: hidden until expanded

    await tester.tap(find.text('عرض الحصص المنتهية (1)'));
    await tester.pumpAndSettle();
    expect(find.text('رياضيات'), findsOneWidget);

    // Another day from the chips: no "now" there
    await tester.tap(find.text('1 حصة'));
    await tester.pumpAndSettle();
    expect(find.text('قرآن'), findsOneWidget);
    expect(find.text('الآن'), findsNothing);
  });

  testWidgets('Timetable "all days" lists the week and opens a day', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara], timetable: _week(today: 2, now: 600)));
    await tester.pumpAndSettle();
    await _openTimetable(tester);

    await tester.tap(find.text('كل الأيام'));
    await tester.pumpAndSettle();
    // Both days with all their lessons
    for (final subject in ['قرآن', 'رياضيات', 'علوم', 'إنجليزي']) {
      expect(find.text(subject), findsOneWidget);
    }

    // Tap Sunday's header: back to the day view on Sunday
    await tester.tap(find.text('الأحد'));
    await tester.pumpAndSettle();
    expect(find.text('قرآن'), findsOneWidget);
    expect(find.text('علوم'), findsNothing);
  });

  testWidgets('Timetable on a day without lessons opens the next school day', (WidgetTester tester) async {
    _phoneSize(tester);
    // Friday: no lessons -> Sunday
    await tester.pumpWidget(_home(() async => [_sara], timetable: _week(today: 5, now: 600)));
    await tester.pumpAndSettle();
    await _openTimetable(tester);

    expect(find.text('لا توجد حصص اليوم (الجمعة).'), findsOneWidget);
    expect(find.text('قرآن'), findsOneWidget);
    expect(find.text('الآن'), findsNothing);
  });

  testWidgets('Timetable without slots shows a message', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await _openTimetable(tester);

    expect(find.text('لا يوجد جدول دراسي لفصل الطالب حالياً.'), findsOneWidget);
  });

  testWidgets('Home with no children shows a message', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => const <Child>[]));
    await tester.pumpAndSettle();

    expect(find.text('لا يوجد أبناء مرتبطون بحسابك حالياً.'), findsOneWidget);
  });

  testWidgets('Home shows the error and retries', (WidgetTester tester) async {
    _phoneSize(tester);
    var calls = 0;
    await tester.pumpWidget(_home(() async {
      calls++;
      if (calls == 1) throw const ChildrenException('هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.');
      return [_sara];
    }));
    await tester.pumpAndSettle();

    expect(find.text('هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.'), findsOneWidget);
    await tester.tap(find.text('إعادة المحاولة'));
    await tester.pumpAndSettle();

    expect(find.text('سارة أحمد'), findsOneWidget);
  });
}
