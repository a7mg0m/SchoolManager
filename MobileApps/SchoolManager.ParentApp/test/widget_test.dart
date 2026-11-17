import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:smart_school_parent/features/children/children_service.dart';
import 'package:smart_school_parent/features/school/school_info_service.dart';
import 'package:smart_school_parent/features/events/events_service.dart';
import 'package:smart_school_parent/features/exams/exams_service.dart';
import 'package:smart_school_parent/features/fees/fees_service.dart';
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
  List<StudentFeeItem>? fees,
  List<ChildExam>? exams,
  List<ChildEvent>? events,
  SchoolInfo? schoolInfo,
}) =>
    MaterialApp(
      builder: (context, child) =>
          Directionality(textDirection: TextDirection.rtl, child: child!),
      home: HomeScreen(
        loadChildren: loader,
        loadProfile: () async => profile,
        loadTimetable: (_) async =>
            timetable ?? const ChildTimetable(today: 2, now: 600, days: []),
        loadFees: (_) async => fees ?? const [],
        loadExams: (_) async => exams ?? const [],
        loadEvents: (_) async => events ?? const [],
        loadSchoolInfo: () async => schoolInfo,
      ),
    );

StudentFeeItem _fee(String id,
    {required double total,
    double discount = 0,
    double paid = 0,
    double waived = 0,
    required FeeState status,
    required DateTime due}) {
  final net = total - discount;
  return StudentFeeItem(
    id: id,
    total: total,
    discount: discount,
    paid: paid,
    waived: waived,
    net: net,
    balance: (net - paid - waived).clamp(0, net).toDouble(),
    currency: 'EGP',
    status: status,
    dueDate: due,
  );
}

ScheduleSlot _slot(String id, String subject, int start, int end) =>
    ScheduleSlot(
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
  testWidgets('New user: intro -> login, empty form is validated',
      (WidgetTester tester) async {
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

  testWidgets('Home shows the parent\'s children and switches between them',
      (WidgetTester tester) async {
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

  testWidgets('Home shows the profile name and picture',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(
      () async => [_sara],
      profile: MyProfile(
          firstName: 'أحمد', lastName: 'جمعة', loginName: 'ahmed', image: _png),
    ));
    await tester.pumpAndSettle();

    expect(find.text('أحمد جمعة'), findsOneWidget);
    final withImage = tester
        .widgetList<CircleAvatar>(find.byType(CircleAvatar))
        .where((a) => a.foregroundImage is MemoryImage);
    expect(withImage, hasLength(1));
  });

  testWidgets('Home without a picture keeps the person icon',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(
      () async => [_sara],
      profile: const MyProfile(
          firstName: 'أحمد', lastName: 'جمعة', loginName: 'ahmed'),
    ));
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.person), findsOneWidget);
    expect(
      tester
          .widgetList<CircleAvatar>(find.byType(CircleAvatar))
          .where((a) => a.foregroundImage != null),
      isEmpty,
    );
  });

  testWidgets('Edit profile saves the changes and keeps a placeholder email',
      (WidgetTester tester) async {
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

  testWidgets('Change password checks the new passwords match',
      (WidgetTester tester) async {
    _phoneSize(tester);
    var changed = false;
    await tester.pumpWidget(MaterialApp(
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: EditProfileScreen(
          profile: const MyProfile(
              firstName: 'أحمد',
              lastName: 'جمعة',
              loginName: 'ahmed',
              email: 'a@b.com'),
          changePassword: (current, next) async => changed = true,
        ),
      ),
    ));

    // The password card is further down the (lazily built) list
    await tester.scrollUntilVisible(
      find.text('تأكيد كلمة المرور'),
      300,
      scrollable: find
          .descendant(
              of: find.byType(ListView), matching: find.byType(Scrollable))
          .first,
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

  testWidgets(
      'Home shows service icons; timetable opens on today with the lesson now',
      (WidgetTester tester) async {
    _phoneSize(tester);
    // Tuesday 10:00: math finished, science now, English next
    await tester.pumpWidget(
        _home(() async => [_sara], timetable: _week(today: 2, now: 600)));
    await tester.pumpAndSettle();

    expect(find.text('الخدمات'), findsOneWidget);
    expect(find.text('الواجبات'), findsOneWidget);
    expect(find.text('إعلان هام'), findsOneWidget);
    // The announcement comes before the services
    expect(tester.getTopLeft(find.text('إعلان هام')).dy,
        lessThan(tester.getTopLeft(find.text('الخدمات')).dy));
    // Only services and the announcement are on home
    for (final removed in [
      'نسبة الحضور',
      'أقرب الأنشطة',
      'آخر الواجبات',
      'حالة الباص',
      'آخر الإشعارات'
    ]) {
      expect(find.text(removed), findsNothing);
    }

    await _openTimetable(tester);

    expect(find.text('اليوم - الثلاثاء'), findsOneWidget);
    expect(find.text('الآن'), findsOneWidget);
    expect(find.text('تنتهي بعد 15 دقيقة'), findsOneWidget);
    expect(find.text('الحصة التالية'), findsOneWidget); // English
    expect(find.text('استراحة 15 دقيقة'), findsOneWidget); // 10:15 - 10:30
    expect(find.text('1/3'), findsOneWidget); // lessons done
    expect(
        find.text('رياضيات'), findsNothing); // finished: hidden until expanded

    await tester.tap(find.text('عرض الحصص المنتهية (1)'));
    await tester.pumpAndSettle();
    expect(find.text('رياضيات'), findsOneWidget);

    // Another day from the chips: no "now" there
    await tester.tap(find.text('1 حصة'));
    await tester.pumpAndSettle();
    expect(find.text('قرآن'), findsOneWidget);
    expect(find.text('الآن'), findsNothing);
  });

  testWidgets('Timetable "all days" lists the week and opens a day',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(
        _home(() async => [_sara], timetable: _week(today: 2, now: 600)));
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

  testWidgets('Timetable on a day without lessons opens the next school day',
      (WidgetTester tester) async {
    _phoneSize(tester);
    // Friday: no lessons -> Sunday
    await tester.pumpWidget(
        _home(() async => [_sara], timetable: _week(today: 5, now: 600)));
    await tester.pumpAndSettle();
    await _openTimetable(tester);

    expect(find.text('لا توجد حصص اليوم (الجمعة).'), findsOneWidget);
    expect(find.text('قرآن'), findsOneWidget);
    expect(find.text('الآن'), findsNothing);
  });

  testWidgets('Timetable without slots shows a message',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await _openTimetable(tester);

    expect(find.text('لا يوجد جدول دراسي لفصل الطالب حالياً.'), findsOneWidget);
  });

  testWidgets('Fees tile opens the child fees with totals and overdue fees',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final past = DateTime.now().subtract(const Duration(days: 10));
    final future = DateTime.now().add(const Duration(days: 30));
    final fees = [
      // 3000 - 500 discount, 1000 paid, due 10 days ago -> overdue, 1,500 left
      _fee('1',
          total: 3000,
          discount: 500,
          paid: 1000,
          status: FeeState.partiallyPaid,
          due: past),
      // 2000 pending, not due yet
      _fee('2', total: 2000, status: FeeState.pending, due: future),
      // 800 fully paid
      _fee('3', total: 800, paid: 800, status: FeeState.paid, due: past),
    ];
    await tester.pumpWidget(_home(() async => [_sara], fees: fees));
    await tester.pumpAndSettle();

    await tester.tap(find.text('الرسوم الدراسية'));
    await tester.pumpAndSettle();

    // Left to pay: 1,500 + 2,000 = 3,500
    expect(find.text('3,500 ج.م'), findsOneWidget);
    expect(find.text('يوجد رسم متأخر عن موعد السداد'), findsOneWidget);
    expect(find.text('متأخر'), findsOneWidget);
    expect(find.text('بانتظار السداد'), findsOneWidget);
    expect(find.text('المتبقي 1,500 ج.م'), findsOneWidget);
    expect(find.text('مستحقات قائمة'), findsOneWidget);

    await tester.scrollUntilVisible(
      find.text('مدفوع بالكامل'),
      300,
      scrollable: find
          .descendant(of: find.byType(ListView), matching: find.byType(Scrollable))
          .first,
    );
    expect(find.text('مدفوع بالكامل'), findsOneWidget);
  });

  testWidgets('Fees screen without fees shows a message',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await tester.tap(find.text('الرسوم الدراسية'));
    await tester.pumpAndSettle();

    expect(find.text('لا توجد رسوم دراسية مسجلة للطالب.'), findsOneWidget);
  });

  testWidgets('Exams tile opens a tab per exam type with upcoming exams and results',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    DateTime day(int offset) => DateTime(today.year, today.month, today.day + offset);
    ChildExam exam(String id, ExamKind kind, String subject, DateTime date,
            {double? score, bool? passed, double? percentage}) =>
        ChildExam(
          id: id,
          kind: kind,
          subject: subject,
          date: date,
          start: 600,
          end: 660,
          duration: 60,
          fullMark: 20,
          passMark: 10,
          score: score,
          isPassed: passed,
          percentage: percentage,
        );

    await tester.pumpWidget(_home(() async => [_sara], exams: [
      exam('1', ExamKind.monthly, 'رياضيات', day(5)), // next upcoming -> opens on "monthly"
      exam('2', ExamKind.monthly, 'علوم', day(-7), score: 18, passed: true, percentage: 90),
      exam('3', ExamKind.quiz, 'لغة عربية', day(-2)), // no result yet
    ]));
    await tester.pumpAndSettle();

    await tester.tap(find.text('الاختبارات والدرجات'));
    await tester.pumpAndSettle();

    // Opens on the type of the next exam
    expect(find.text('امتحانات شهرية'), findsOneWidget);
    expect(find.text('رياضيات'), findsOneWidget);
    expect(find.text('بعد 5 أيام'), findsOneWidget);
    expect(find.text('علوم'), findsOneWidget);
    expect(find.text('18'), findsOneWidget);
    expect(find.text('ناجح'), findsNWidgets(2)); // summary "passed" count + the result badge
    expect(find.text('90%'), findsWidgets); // result and average

    // Quiz tab (the tab bar scrolls; bring the tab into view first): exam without a result yet
    await tester.ensureVisible(find.text('اختبار قصير'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('اختبار قصير'));
    await tester.pumpAndSettle();
    expect(find.text('لغة عربية'), findsOneWidget);
    expect(find.text('بانتظار النتيجة'), findsOneWidget);

    // Final tab: empty
    await tester.ensureVisible(find.text('نهائي'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('نهائي'));
    await tester.pumpAndSettle();
    expect(find.text('لا توجد امتحانات نهائية حالياً.'), findsOneWidget);
  });

  testWidgets('Monthly exams show their month and can be filtered by month',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    ChildExam monthly(String id, String subject, int month, int daysAgo) => ChildExam(
          id: id,
          kind: ExamKind.monthly,
          month: month,
          subject: subject,
          date: DateTime(today.year, today.month, today.day - daysAgo),
          start: 600,
          end: 660,
          duration: 60,
          fullMark: 20,
          passMark: 10,
          score: 15,
          isPassed: true,
          percentage: 75,
        );

    await tester.pumpWidget(_home(() async => [_sara], exams: [
      monthly('1', 'رياضيات', 10, 40), // October exam
      monthly('2', 'علوم', 11, 5), // November exam
    ]));
    await tester.pumpAndSettle();
    await tester.tap(find.text('الاختبارات والدرجات'));
    await tester.pumpAndSettle();

    // Opens on the Monthly tab (latest result); both months listed with their tags
    expect(find.text('الكل'), findsOneWidget);
    expect(find.text('شهر أكتوبر'), findsOneWidget);
    expect(find.text('شهر نوفمبر'), findsOneWidget);

    // Filter: October only
    await tester.tap(find.text('أكتوبر'));
    await tester.pumpAndSettle();
    expect(find.text('رياضيات'), findsOneWidget);
    expect(find.text('علوم'), findsNothing);

    // Back to all months
    await tester.tap(find.text('الكل'));
    await tester.pumpAndSettle();
    expect(find.text('علوم'), findsOneWidget);
  });

  testWidgets('Events tile shows the next event, upcoming and past events with their audience',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    ChildEvent event(String id, String title, int dayOffset, EventAudience audience) => ChildEvent(
          id: id,
          title: title,
          description: 'وصف $title',
          date: DateTime(today.year, today.month, today.day + dayOffset),
          start: 540,
          end: 600,
          audience: audience,
        );

    await tester.pumpWidget(_home(() async => [_sara], events: [
      event('1', 'رحلة مدرسية', 3, EventAudience.all), // next event
      event('2', 'يوم رياضي', 20, EventAudience.grade),
      event('3', 'حفل تكريم', -10, EventAudience.student), // past
    ]));
    await tester.pumpAndSettle();
    await tester.tap(find.text('الفعاليات'));
    await tester.pumpAndSettle();

    // Upcoming: next event highlighted, the other with its audience
    expect(find.text('الفعالية القادمة'), findsOneWidget);
    expect(find.text('رحلة مدرسية'), findsOneWidget);
    expect(find.text('بعد 3 أيام'), findsOneWidget);
    expect(find.text('يوم رياضي'), findsOneWidget);
    expect(find.text('لصف الطالب'), findsOneWidget);
    expect(find.text('حفل تكريم'), findsNothing);

    // Past tab
    await tester.tap(find.text('السابقة (1)'));
    await tester.pumpAndSettle();
    expect(find.text('حفل تكريم'), findsOneWidget);
    expect(find.text('للطالب خاصة'), findsOneWidget);

    // Details sheet
    await tester.tap(find.text('حفل تكريم'));
    await tester.pumpAndSettle();
    expect(find.text('وصف حفل تكريم'), findsOneWidget);
  });

  testWidgets('Events screen without events shows a message', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await tester.tap(find.text('الفعاليات'));
    await tester.pumpAndSettle();

    expect(find.text('لا توجد فعاليات قادمة حالياً.'), findsOneWidget);
  });

  testWidgets('More menu opens "About the school" with contacts and the album',
      (WidgetTester tester) async {
    _phoneSize(tester);
    const info = SchoolInfo(
      nameAr: 'مدرسة النور',
      nameEn: 'Al Noor School',
      address: 'القاهرة - مدينة نصر',
      phone: '0223456789',
      email: 'info@alnoor.edu',
      album: [
        AlbumImage(id: 'a1', url: 'http://localhost/upload/school-album/1.jpg', fileName: '1.jpg'),
        AlbumImage(id: 'a2', url: 'http://localhost/upload/school-album/2.jpg', fileName: '2.jpg'),
      ],
    );
    await tester.pumpWidget(_home(() async => [_sara], schoolInfo: info));
    await tester.pumpAndSettle();

    // More tab -> About the school
    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('عن المدرسة'));
    await tester.pumpAndSettle();

    expect(find.text('مدرسة النور'), findsOneWidget);
    expect(find.text('Al Noor School'), findsOneWidget);
    expect(find.text('0223456789'), findsOneWidget);
    expect(find.text('info@alnoor.edu'), findsOneWidget);
    expect(find.text('ألبوم المدرسة'), findsOneWidget);

    // Album slider: first photo with its counter
    await tester.scrollUntilVisible(
      find.byKey(const ValueKey('album-slide-0')),
      300,
      scrollable: find.descendant(of: find.byType(ListView), matching: find.byType(Scrollable)).first,
    );
    expect(find.text('1 من 2'), findsOneWidget);

    // Auto-play moves to the second photo after the interval
    await tester.pump(const Duration(seconds: 4));
    await tester.pumpAndSettle();
    expect(find.text('2 من 2'), findsOneWidget);

    // Tap it: the viewer opens on that photo
    await tester.tap(find.byKey(const ValueKey('album-slide-1')));
    await tester.pumpAndSettle();
    expect(find.text('2 / 2'), findsOneWidget);
  });

  testWidgets('About the school before the school info is set up', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('عن المدرسة'));
    await tester.pumpAndSettle();

    expect(find.text('لم تُضف بيانات المدرسة بعد.'), findsOneWidget);
  });

  testWidgets('Home with no children shows a message',
      (WidgetTester tester) async {
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
      if (calls == 1)
        throw const ChildrenException(
            'هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.');
      return [_sara];
    }));
    await tester.pumpAndSettle();

    expect(find.text('هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.'),
        findsOneWidget);
    await tester.tap(find.text('إعادة المحاولة'));
    await tester.pumpAndSettle();

    expect(find.text('سارة أحمد'), findsOneWidget);
  });
}
