import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_parent/features/chat/chat_screens.dart';
import 'package:smart_school_parent/features/notifications/notifications_screen.dart';
import 'package:smart_school_parent/features/notifications/notifications_service.dart';
import 'package:smart_school_parent/features/chat/chat_service.dart';
import 'package:smart_school_parent/core/academic_year/academic_year_controller.dart';
import 'package:smart_school_parent/core/academic_year/academic_year_picker.dart';
import 'package:smart_school_parent/core/i18n/app_language.dart';
import 'package:smart_school_parent/features/schedule/schedule_format.dart';

import 'package:smart_school_parent/core/theme/app_theme.dart';
import 'package:smart_school_parent/core/theme/theme_controller.dart';

import 'package:smart_school_parent/features/children/children_service.dart';
import 'package:smart_school_parent/features/school/school_info_screen.dart';
import 'package:smart_school_parent/features/school/school_info_service.dart';
import 'package:smart_school_parent/features/events/events_screen.dart';
import 'package:smart_school_parent/features/events/events_service.dart';
import 'package:smart_school_parent/features/exams/exams_screen.dart';
import 'package:smart_school_parent/features/exams/exams_service.dart';
import 'package:smart_school_parent/features/fees/fees_screen.dart';
import 'package:smart_school_parent/features/fees/fees_service.dart';
import 'package:smart_school_parent/features/home/home_screen.dart';
import 'package:smart_school_parent/features/homework/homework_details_screen.dart';
import 'package:smart_school_parent/features/homework/homework_service.dart';
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
  List<ChildHomework>? homeworks,
}) =>
    // Same language set-up as the app: Arabic (RTL) by default, English (LTR) when chosen
    ListenableBuilder(
      listenable: AppLanguage.instance,
      builder: (context, _) => MaterialApp(
      locale: AppLanguage.instance.value,
      supportedLocales: AppLanguage.supported,
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: HomeScreen(
        loadChildren: loader,
        loadProfile: () async => profile,
        loadTimetable: (_) async =>
            timetable ?? const ChildTimetable(today: 2, now: 600, days: []),
        loadFees: (_) async => fees ?? const [],
        loadExams: (_) async => exams ?? const [],
        loadEvents: (_) async => events ?? const [],
        loadSchoolInfo: () async => schoolInfo,
        loadHomeworks: (_) async => homeworks ?? const [],
        loadAcademicYears: () async => _years,
      ),
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

/// Scrolls Home to a service tile (it may be under the floating bar or off screen), then taps it
Future<void> _tapTile(WidgetTester tester, String label) async {
  await tester.ensureVisible(find.text(label));
  await tester.pumpAndSettle();
  await tester.tap(find.text(label));
}

Future<void> _openTimetable(WidgetTester tester) async {
  await _tapTile(tester, 'الجدول الدراسي');
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

/// Two academic years: 2026-2027 is the current one
final _years = [
  AcademicYear(
    id: 'y-2026',
    name: '2026-2027',
    startDate: DateTime(DateTime.now().year - 1, 1, 1),
    endDate: DateTime(DateTime.now().year + 1, 12, 31),
    semesters: [
      // Running today
      AcademicSemester(id: 's-1', name: 'الفصل الأول', startDate: DateTime(DateTime.now().year - 1, 1, 1), endDate: DateTime(DateTime.now().year, 12, 31)),
      AcademicSemester(id: 's-2', name: 'الفصل الثاني', startDate: DateTime(DateTime.now().year + 1, 1, 1), endDate: DateTime(DateTime.now().year + 1, 12, 31)),
    ],
  ),
  AcademicYear(
    id: 'y-2025',
    name: '2025-2026',
    startDate: DateTime(2000, 9, 1),
    endDate: DateTime(2001, 6, 30),
    isActive: false,
    semesters: [AcademicSemester(id: 's-old', name: 'فصل قديم', startDate: DateTime(2000, 9, 1), endDate: DateTime(2001, 6, 30))],
  ),
];

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await AcademicYearController.instance.reset();
  });

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

  testWidgets('Important notice shows the next upcoming event; View all opens the events',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    final day = DateTime(today.year, today.month, today.day);
    ChildEvent event(String id, String title, int inDays) => ChildEvent(
          id: id,
          title: title,
          description: 'وصف $title',
          date: day.add(Duration(days: inDays)),
          start: 9 * 60,
          end: 11 * 60,
          audience: EventAudience.all,
        );
    await tester.pumpWidget(_home(() async => [_sara], events: [
      event('e1', 'حفل نهاية العام', 9),
      event('e2', 'رحلة المتحف', 1),
      event('e3', 'يوم رياضي سابق', -3),
    ]));
    await tester.pumpAndSettle();

    // The nearest one that is not over (tomorrow's trip), before the services
    expect(find.text('إعلان هام'), findsOneWidget);
    expect(find.text('رحلة المتحف'), findsWidgets);
    expect(find.textContaining('غداً · 9:00'), findsOneWidget);
    expect(tester.getTopLeft(find.text('إعلان هام')).dy,
        lessThan(tester.getTopLeft(find.text('الخدمات')).dy));

    await tester.tap(find.byKey(const ValueKey('announcement-view-all')));
    await tester.pumpAndSettle();
    expect(find.byType(EventsScreen), findsOneWidget);
    expect(find.text('الرئيسية'), findsOneWidget); // bottom bar stays
  });

  testWidgets('Only past events: no important notice', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara], events: [
      ChildEvent(id: 'old', title: 'حفل سابق', description: '', date: DateTime(2000, 1, 1), start: 540, end: 600, audience: EventAudience.all),
    ]));
    await tester.pumpAndSettle();
    expect(find.text('إعلان هام'), findsNothing);
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
    // No upcoming event: no important notice
    expect(find.text('إعلان هام'), findsNothing);
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

    await _tapTile(tester, 'الرسوم الدراسية');
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
          .descendant(
              of: find.byType(ListView), matching: find.byType(Scrollable))
          .first,
    );
    expect(find.text('مدفوع بالكامل'), findsOneWidget);
  });

  testWidgets('Fees screen without fees shows a message',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await _tapTile(tester, 'الرسوم الدراسية');
    await tester.pumpAndSettle();

    expect(find.text('لا توجد رسوم دراسية مسجلة للطالب.'), findsOneWidget);
  });

  testWidgets(
      'Exams tile opens a tab per exam type with upcoming exams and results',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    DateTime day(int offset) =>
        DateTime(today.year, today.month, today.day + offset);
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
      exam('1', ExamKind.monthly, 'رياضيات',
          day(5)), // next upcoming -> opens on "monthly"
      exam('2', ExamKind.monthly, 'علوم', day(-7),
          score: 18, passed: true, percentage: 90),
      exam('3', ExamKind.quiz, 'لغة عربية', day(-2)), // no result yet
    ]));
    await tester.pumpAndSettle();

    await _tapTile(tester, 'الاختبارات والدرجات');
    await tester.pumpAndSettle();

    // Opens on the type of the next exam
    expect(find.text('امتحانات شهرية'), findsOneWidget);
    expect(find.text('رياضيات'), findsOneWidget);
    expect(find.text('بعد 5 أيام'), findsOneWidget);
    expect(find.text('علوم'), findsOneWidget);
    expect(find.text('18'), findsOneWidget);
    expect(find.text('ناجح'),
        findsNWidgets(2)); // summary "passed" count + the result badge
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
    ChildExam monthly(String id, String subject, int month, int daysAgo) =>
        ChildExam(
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
    await _tapTile(tester, 'الاختبارات والدرجات');
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

  testWidgets(
      'Events tile shows the next event, upcoming and past events with their audience',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    ChildEvent event(
            String id, String title, int dayOffset, EventAudience audience) =>
        ChildEvent(
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
    await _tapTile(tester, 'الفعاليات');
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

  testWidgets('Events screen without events shows a message',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();
    await _tapTile(tester, 'الفعاليات');
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
        AlbumImage(
            id: 'a1',
            url: 'http://localhost/upload/school-album/1.jpg',
            fileName: '1.jpg'),
        AlbumImage(
            id: 'a2',
            url: 'http://localhost/upload/school-album/2.jpg',
            fileName: '2.jpg'),
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
      scrollable: find
          .descendant(
              of: find.byType(ListView), matching: find.byType(Scrollable))
          .first,
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

  testWidgets('About the school before the school info is set up',
      (WidgetTester tester) async {
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
      if (calls == 1) {
        throw const ChildrenException(
            'هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.');
      }
      return [_sara];
    }));
    await tester.pumpAndSettle();

    expect(find.text('هذا الحساب غير مرتبط بولي أمر، تواصل مع إدارة المدرسة.'),
        findsOneWidget);
    await tester.tap(find.text('إعادة المحاولة'));
    await tester.pumpAndSettle();

    expect(find.text('سارة أحمد'), findsOneWidget);
  });

  test('Album items from the API know whether they are videos', () {
    final video = AlbumImage.fromJson({
      'id': 'v1',
      'url': '/upload/school-album/v.mp4',
      'fileName': 'party.mp4',
      'contentType': 'video/mp4',
      'mediaType': 'video',
    });
    final image = AlbumImage.fromJson({
      'id': 'i1',
      'url': '/upload/school-album/p.jpg',
      'fileName': 'p.jpg',
      'contentType': 'image/jpeg',
    });
    expect(video.isVideo, isTrue);
    expect(image.isVideo, isFalse);
  });

  testWidgets('Album slider shows a video as a play tile and opens it on tap',
      (WidgetTester tester) async {
    _phoneSize(tester);
    int? opened;
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: AlbumSlider(
          images: const [
            AlbumImage(
                id: 'v1',
                url: 'http://localhost/upload/school-album/v.mp4',
                fileName: 'party.mp4',
                contentType: 'video/mp4'),
          ],
          onOpen: (i) => opened = i,
        ),
      ),
    ));
    await tester.pump();

    expect(find.byKey(const ValueKey('album-video-play')), findsOneWidget);
    expect(find.text('party.mp4'), findsOneWidget);
    expect(find.byType(Image), findsNothing); // no image download for a video

    await tester.tap(find.byKey(const ValueKey('album-slide-0')));
    expect(opened, 0);
  });

  test('School info reads the location (and ignores invalid coordinates)', () {
    final info = SchoolInfo.fromJson({
      'nameAr': 'مدرسة النور',
      'nameEn': 'Al Noor',
      'latitude': 30.0444,
      'longitude': 31.2357,
    });
    expect(info.hasLocation, isTrue);
    expect(info.latitude, 30.0444);
    expect(info.longitude, 31.2357);

    final none = SchoolInfo.fromJson({'nameAr': 'x', 'nameEn': 'x', 'latitude': null, 'longitude': null});
    expect(none.hasLocation, isFalse);

    final invalid = SchoolInfo.fromJson({'nameAr': 'x', 'nameEn': 'x', 'latitude': 120, 'longitude': 31});
    expect(invalid.hasLocation, isFalse);
  });

  testWidgets('About the school shows the location map and opens the full map',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(MaterialApp(
      home: Directionality(
        textDirection: TextDirection.rtl,
        child: SchoolInfoScreen(
          showMapTiles: false,
          loadSchoolInfo: () async => const SchoolInfo(
            nameAr: 'مدرسة النور',
            nameEn: 'Al Noor School',
            address: 'القاهرة - مدينة نصر',
            latitude: 30.0444,
            longitude: 31.2357,
          ),
        ),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('موقع المدرسة'), findsOneWidget);
    expect(find.byKey(const ValueKey('school-pin')), findsOneWidget);
    expect(find.text('الاتجاهات'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('school-map-preview')));
    await tester.pumpAndSettle();

    expect(find.text('الاتجاهات إلى المدرسة'), findsOneWidget);
    expect(find.text('30.044400, 31.235700'), findsOneWidget);
  });

  testWidgets('About the school without a location has no map',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(MaterialApp(
      home: SchoolInfoScreen(
        showMapTiles: false,
        loadSchoolInfo: () async => const SchoolInfo(nameAr: 'مدرسة النور', nameEn: 'Al Noor School'),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('موقع المدرسة'), findsNothing);
    expect(find.byKey(const ValueKey('school-pin')), findsNothing);
  });

  testWidgets('More menu switches light / dark / auto and saves it',
      (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();

    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    expect(find.text('المظهر'), findsOneWidget);

    await tester.tap(find.text('داكن'));
    await tester.pumpAndSettle();
    expect(ThemeController.instance.value, ThemeMode.dark);
    expect((await SharedPreferences.getInstance()).getString('theme_mode'), 'dark');

    await tester.tap(find.text('فاتح'));
    await tester.pumpAndSettle();
    expect(ThemeController.instance.value, ThemeMode.light);

    await tester.tap(find.text('تلقائي'));
    await tester.pumpAndSettle();
    expect(ThemeController.instance.value, ThemeMode.system);
  });

  testWidgets('Dark mode gives the screens the dark palette',
      (WidgetTester tester) async {
    late AppPalette palette;
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.dark,
      home: Builder(builder: (context) {
        palette = context.palette;
        return const SizedBox();
      }),
    ));
    expect(palette.card, AppPalette.dark.card);
    expect(palette.ink, AppPalette.dark.ink);
  });

  testWidgets('More menu switches the app to English (LTR) and back to Arabic',
      (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});
    addTearDown(() => AppLanguage.instance.setLanguage(AppLanguage.arabic));
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();

    // Arabic, right to left
    expect(find.text('الخدمات'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('الخدمات'))), TextDirection.rtl);

    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    expect(find.text('اللغة'), findsOneWidget);
    await tester.tap(find.text('English'));
    await tester.pumpAndSettle();

    expect(AppLanguage.instance.isEnglish, isTrue);
    expect((await SharedPreferences.getInstance()).getString('app_language'), 'en');
    // The open menu is in English now
    expect(find.text('Language'), findsOneWidget);
    expect(find.text('Sign out'), findsOneWidget);

    // Close the menu: Home in English, left to right, the screen kept its state
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();
    expect(find.text('Services'), findsOneWidget);
    expect(find.text('Timetable'), findsOneWidget);
    expect(find.text('Home'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('Services'))), TextDirection.ltr);

    // Back to Arabic
    await tester.tap(find.text('More'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('العربية'));
    await tester.pumpAndSettle();
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();
    expect(find.text('الخدمات'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('الخدمات'))), TextDirection.rtl);
  });

  test('Day / month names, durations and API names follow the language', () async {
    SharedPreferences.setMockInitialValues({});
    addTearDown(() => AppLanguage.instance.setLanguage(AppLanguage.arabic));
    const child = Child(
      id: '1', fullNameAr: 'سارة أحمد', fullNameEn: 'Sara Ahmed', gender: 'Female',
      stageName: '', gradeName: '', sectionName: '');

    expect(dayNames[0], 'الأحد');
    expect(child.name, 'سارة أحمد');

    await AppLanguage.instance.setLanguage(AppLanguage.english);
    expect(dayNames[0], 'Sunday');
    expect(monthNames[11], 'December');
    expect(formatDuration(90), '1 hour 30 minutes');
    expect(formatTime(510), '8:30 AM');
    expect(lessonsCount(6), '6 lessons');
    expect(child.name, 'Sara Ahmed');
    expect(ExamKind.monthly.label, 'Monthly exam');
    expect(pickLang('عربي', ''), 'عربي'); // English missing: Arabic
  });

  testWidgets('Home header shows the school logo from school info',
      (WidgetTester tester) async {
    _phoneSize(tester);
    // 1x1 PNG
    final logo = base64Decode(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==');
    await tester.pumpWidget(_home(() async => [_sara],
        schoolInfo: SchoolInfo(nameAr: 'مدرسة النور', nameEn: 'Al Noor', logo: logo)));
    await tester.pumpAndSettle();

    final header = find.byKey(const ValueKey('school-logo'));
    expect(find.descendant(of: header, matching: find.byType(Image)), findsOneWidget);
    expect(find.descendant(of: header, matching: find.byIcon(Icons.school_rounded)), findsNothing);

    // Tap: About the school
    await tester.tap(header);
    await tester.pumpAndSettle();
    expect(find.text('عن المدرسة'), findsOneWidget);
  });

  testWidgets('Home header keeps the school icon when there is no logo',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara]));
    await tester.pumpAndSettle();

    final header = find.byKey(const ValueKey('school-logo'));
    expect(find.descendant(of: header, matching: find.byIcon(Icons.school_rounded)), findsOneWidget);
    expect(find.descendant(of: header, matching: find.byType(Image)), findsNothing);
  });

  testWidgets('Home today card shows the lesson now and the tiles show live data',
      (WidgetTester tester) async {
    _phoneSize(tester);
    // Tuesday 10:00: science now (9:30 - 10:15), English next
    await tester.pumpWidget(_home(
      () async => [_sara],
      timetable: _week(today: 2, now: 600),
      fees: [
        _fee('f1', total: 1500, paid: 250, status: FeeState.partiallyPaid,
            due: DateTime.now().add(const Duration(days: 20))),
      ],
    ));
    await tester.pumpAndSettle();

    final today = find.byKey(const ValueKey('today-card'));
    expect(today, findsOneWidget);
    expect(find.descendant(of: today, matching: find.text('الآن')), findsOneWidget);
    expect(find.descendant(of: today, matching: find.text('علوم')), findsOneWidget);
    expect(find.descendant(of: today, matching: find.text('1/3')), findsOneWidget);
    // Lessons today on the timetable tile, the balance on the fees tile
    expect(find.text('3 حصص اليوم'), findsOneWidget);
    expect(find.text('المتبقي 1,250 ج.م'), findsOneWidget);

    // The today card opens the timetable
    await tester.tap(today);
    await tester.pumpAndSettle();
    expect(find.text('اليوم - الثلاثاء'), findsOneWidget);
  });

  testWidgets('Home children are stories: tapping one selects it',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara, _omar]));
    await tester.pumpAndSettle();

    expect(find.text('الصف الأول - فصل أ'), findsOneWidget);
    await tester.tap(find.text('عمر أحمد'));
    await tester.pumpAndSettle();
    expect(find.text('KG 2 - فصل ب'), findsOneWidget);
    expect(find.text('الصف الأول - فصل أ'), findsNothing);
  });

  testWidgets('Bottom bar stays pinned on pages opened from Home',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(() async => [_sara], fees: const []));
    await tester.pumpAndSettle();

    // Fees page: the bar is still there
    await _tapTile(tester, 'الرسوم الدراسية');
    await tester.pumpAndSettle();
    expect(find.text('لا توجد رسوم دراسية مسجلة للطالب.'), findsOneWidget);
    expect(find.text('المزيد'), findsOneWidget);
    expect(find.text('الرئيسية'), findsOneWidget);

    // A page opened from the More menu keeps it too
    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('عن المدرسة'));
    await tester.pumpAndSettle();
    expect(find.text('لم تُضف بيانات المدرسة بعد.'), findsOneWidget);
    expect(find.text('الرئيسية'), findsOneWidget);

    // Home in the bar goes back to the home page
    await tester.tap(find.text('الرئيسية'));
    await tester.pumpAndSettle();
    expect(find.text('الخدمات'), findsOneWidget);
    expect(find.text('لم تُضف بيانات المدرسة بعد.'), findsNothing);
  });

  testWidgets('Homework tile shows how many are due and opens the child homework',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    ChildHomework hw(String id, String title, int dueInDays) => ChildHomework(
          id: id,
          subjectNameAr: 'رياضيات',
          subjectNameEn: 'Math',
          title: title,
          description: 'وصف $title',
          dueDate: DateTime(today.year, today.month, today.day).add(Duration(days: dueInDays)),
          publishedOn: today,
          questions: const [HomeworkQuestion(header: 'احسب', items: ['2 + 3'])],
        );
    await tester.pumpWidget(_home(() async => [_sara], homeworks: [
      hw('1', 'واجب الجمع', 1),
      hw('2', 'واجب الطرح', 0),
      hw('3', 'واجب قديم', -5),
    ]));
    await tester.pumpAndSettle();

    // Two still to hand in
    expect(find.text('2 مطلوبة'), findsOneWidget);
    await _tapTile(tester, 'الواجبات');
    await tester.pumpAndSettle();

    expect(find.text('واجب الطرح'), findsOneWidget);
    expect(find.text('التسليم اليوم'), findsOneWidget);
    expect(find.text('واجب الجمع'), findsOneWidget);
    expect(find.text('التسليم غداً'), findsOneWidget);
    expect(find.text('واجب قديم'), findsNothing);

    // Past tab
    await tester.tap(find.text('السابقة (1)'));
    await tester.pumpAndSettle();
    expect(find.text('واجب قديم'), findsOneWidget);

    // Details open on their own page with the questions
    await tester.tap(find.text('واجب قديم'));
    await tester.pumpAndSettle();
    expect(find.byType(HomeworkDetailsScreen), findsOneWidget);
    expect(find.text('انتهى موعد التسليم'), findsOneWidget);
    expect(find.text('تعليمات المعلم'), findsOneWidget);
    expect(find.text('وصف واجب قديم'), findsOneWidget);
    expect(find.text('احسب'), findsOneWidget);
    expect(find.text('أ'), findsOneWidget);
    expect(find.text('2 + 3'), findsOneWidget);

    // Back to the list
    await tester.tap(find.byType(BackButton).last);
    await tester.pumpAndSettle();
    expect(find.byType(HomeworkDetailsScreen), findsNothing);
    expect(find.text('واجب قديم'), findsOneWidget);
  });

  testWidgets('Academic year and semester: current ones by default, others reload the child data with their ids',
      (WidgetTester tester) async {
    _phoneSize(tester);
    var homeworkLoads = 0;
    final sent = <Map<String, String>>[];
    await tester.pumpWidget(ListenableBuilder(
      listenable: AppLanguage.instance,
      builder: (context, _) => MaterialApp(
        locale: AppLanguage.instance.value,
        supportedLocales: AppLanguage.supported,
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        home: HomeScreen(
          loadChildren: () async => [_sara],
          loadProfile: () async => null,
          loadTimetable: (_) async => const ChildTimetable(today: 2, now: 600, days: []),
          loadFees: (_) async => const [],
          loadExams: (_) async => const [],
          loadEvents: (_) async => const [],
          loadSchoolInfo: () async => null,
          loadHomeworks: (_) async {
            homeworkLoads++;
            sent.add(AcademicYearController.instance.withYear(Uri.parse('/x')).queryParameters);
            return const [];
          },
          loadAcademicYears: () async => _years,
        ),
      ),
    ));
    await tester.pumpAndSettle();

    // Current year and semester chosen and shown in the header
    expect(AcademicYearController.instance.selectedId, 'y-2026');
    expect(AcademicYearController.instance.selectedSemesterId, 's-1');
    expect(find.text('2026-2027 · الفصل الأول'), findsOneWidget);
    expect(sent.last, {'academicYearId': 'y-2026', 'semesterId': 's-1'});
    var before = homeworkLoads;

    // Another semester from the header chip
    await tester.tap(find.byKey(const ValueKey('academic-year-chip')));
    await tester.pumpAndSettle();
    expect(find.text('الفصل الحالي'), findsOneWidget);
    await tester.tap(find.text('الفصل الثاني'));
    await tester.pumpAndSettle();

    expect(find.text('2026-2027 · الفصل الثاني'), findsOneWidget);
    expect(homeworkLoads, greaterThan(before));
    expect(sent.last, {'academicYearId': 'y-2026', 'semesterId': 's-2'});
    before = homeworkLoads;

    // Another year: its own semester comes with it
    await tester.tap(find.byKey(const ValueKey('academic-year-chip')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('2025-2026'));
    await tester.pumpAndSettle();
    expect(find.text('فصل قديم'), findsOneWidget);
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();

    expect(find.text('2025-2026 · فصل قديم'), findsOneWidget);
    expect(homeworkLoads, greaterThan(before));
    expect(sent.last, {'academicYearId': 'y-2025', 'semesterId': 's-old'});
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getString('academic_year_id'), 'y-2025');
    expect(prefs.getString('semester_id'), 's-old');

    // The More menu has the pickers too
    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    expect(find.text('السنة الدراسية'), findsOneWidget);
    expect(find.text('الفصل الدراسي'), findsOneWidget);
  });

  testWidgets('Exams screen shows and changes the academic year / semester it is for',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await AcademicYearController.instance.ensureLoaded(fetch: () async => _years);
    var loads = 0;
    final sent = <Map<String, String>>[];
    await tester.pumpWidget(MaterialApp(
      home: ExamsScreen(
        child: _sara,
        loadExams: (_) async {
          loads++;
          sent.add(AcademicYearController.instance.withYear(Uri.parse('/x')).queryParameters);
          return const [];
        },
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('2026-2027 · الفصل الأول'), findsOneWidget);
    expect(sent.last, {'academicYearId': 'y-2026', 'semesterId': 's-1'});
    final before = loads;

    // Another semester from the exams page itself
    await tester.tap(find.byKey(const ValueKey('exams-change-period')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('الفصل الثاني'));
    await tester.pumpAndSettle();

    expect(loads, greaterThan(before));
    expect(sent.last, {'academicYearId': 'y-2026', 'semesterId': 's-2'});
    expect(find.text('2026-2027 · الفصل الثاني'), findsOneWidget);
  });

  testWidgets('Fees screen shows the bus fee next to the school fees', (WidgetTester tester) async {
    _phoneSize(tester);
    final due = DateTime.now().add(const Duration(days: 20));
    await tester.pumpWidget(MaterialApp(
      home: FeesScreen(
        child: _sara,
        loadFees: (_) async => [
          StudentFeeItem.fromJson({
            'id': 'f-school', 'totalAmount': 5000, 'discountAmount': 0, 'paidAmount': 1000, 'waivedAmount': 0,
            'netAmount': 5000, 'balance': 4000, 'currency': 'EGP', 'status': 'PartiallyPaid', 'dueDate': due.toIso8601String(),
          }),
          StudentFeeItem.fromJson({
            'id': 'f-bus', 'busAssignmentId': 'ba-1', 'totalAmount': 1500, 'discountAmount': 0, 'paidAmount': 0, 'waivedAmount': 0,
            'netAmount': 1500, 'balance': 1500, 'currency': 'EGP', 'status': 'Pending', 'dueDate': due.toIso8601String(),
          }),
        ],
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.descendant(of: find.byKey(const ValueKey('fee-kind-f-bus')), matching: find.text('رسوم الباص')), findsOneWidget);
    expect(find.descendant(of: find.byKey(const ValueKey('fee-kind-f-school')), matching: find.text('الرسوم الدراسية')), findsOneWidget);
    expect(StudentFeeItem.fromJson({'id': 'x', 'busAssignmentId': null}).isBusFee, isFalse);
  });

  testWidgets('Fees screen shows the academic year and reloads for another one', (WidgetTester tester) async {
    _phoneSize(tester);
    await AcademicYearController.instance.ensureLoaded(fetch: () async => _years);
    var loads = 0;
    await tester.pumpWidget(MaterialApp(
      home: FeesScreen(
        child: _sara,
        loadFees: (_) async {
          loads++;
          return const [];
        },
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.byKey(const ValueKey('fees-year')), findsOneWidget);
    expect(find.text('2026-2027'), findsOneWidget);
    final before = loads;

    await tester.tap(find.byKey(const ValueKey('fees-change-year')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('2025-2026'));
    await tester.pumpAndSettle();
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();

    expect(loads, greaterThan(before));
    expect(find.text('2025-2026'), findsOneWidget);
  });

  group('Notifications', () {
    final now = DateTime(2026, 11, 20, 10);
    final notices = [
      AppNotice(id: 'n1', title: 'اجتماع أولياء الأمور', description: 'يوم الخميس الساعة 10 صباحاً في قاعة المدرسة', sentOn: DateTime(2026, 11, 20), type: NoticeType.event, forEveryone: true),
      AppNotice(id: 'n2', title: 'واجب الرياضيات', description: 'حل تمارين صفحة 20', sentOn: DateTime(2026, 11, 19), type: NoticeType.homework),
      AppNotice(id: 'n3', title: 'نتائج الامتحان الشهري', description: 'ظهرت النتائج', sentOn: DateTime(2026, 11, 10), type: NoticeType.examResult, imageUrl: 'http://localhost/x.png'),
    ];

    testWidgets('List grouped by day with types; filter by type; details page', (WidgetTester tester) async {
      _phoneSize(tester);
      var loads = 0;
      await tester.pumpWidget(MaterialApp(
        home: NotificationsScreen(
          loadNotices: () async {
            loads++;
            return notices;
          },
          subtitle: 'سارة',
          targetedLabel: 'خاص',
          now: now,
        ),
      ));
      await tester.pumpAndSettle();

      expect(loads, 1);
      expect(find.text('1 إشعار جديد اليوم'), findsOneWidget);
      expect(find.text('اليوم'), findsOneWidget);
      expect(find.text('أمس'), findsOneWidget);
      expect(find.text('اجتماع أولياء الأمور'), findsOneWidget);
      expect(find.text('جديد'), findsOneWidget);
      expect(find.text('للجميع'), findsOneWidget);
      expect(find.text('خاص'), findsNWidgets(2));

      // Only homework
      await tester.tap(find.byKey(const ValueKey('notice-type-homework')));
      await tester.pumpAndSettle();
      expect(find.text('واجب الرياضيات'), findsOneWidget);
      expect(find.text('اجتماع أولياء الأمور'), findsNothing);
      await tester.tap(find.byKey(const ValueKey('notice-type-all')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('notice-n1')));
      await tester.pumpAndSettle();
      expect(find.byType(NotificationDetailsScreen), findsOneWidget);
      expect(find.text('يوم الخميس الساعة 10 صباحاً في قاعة المدرسة'), findsOneWidget);
      expect(find.text('الجميع'), findsOneWidget);
      expect(find.text('الجمعة 20 نوفمبر 2026'), findsOneWidget);
    });

    testWidgets('No notifications shows a message', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(home: NotificationsScreen(loadNotices: () async => const [], now: now)));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد إشعارات في هذا العام الدراسي.'), findsOneWidget);
    });

    test('Notification JSON: type, date, image and audience', () {
      final n = AppNotice.fromJson({
        'id': 'x',
        'title': ' تنبيه ',
        'description': 'نص',
        'sentOn': '2026-11-18',
        'notificationType': 2,
        'imageUrl': '',
        'forEveryone': true,
      });
      expect(n.title, 'تنبيه');
      expect(n.type, NoticeType.exam);
      expect(n.sentOn, DateTime(2026, 11, 18));
      expect(n.imageUrl, isNull);
      expect(n.forEveryone, isTrue);
      expect(NoticeType.from(99), NoticeType.message);
    });
  });

  group('Chat', () {
    const me = 'me-1';
    final now = DateTime(2026, 11, 20, 12, 0);

    ChatMessage msg(String id, String sender, String text, int minutesAgo, {ChatStatus status = ChatStatus.sent}) =>
        ChatMessage(id: id, senderId: sender, content: text, sentOn: now.subtract(Duration(minutes: minutesAgo)), status: status);

    testWidgets('Chats list: unread badge, my last message; a chat shows the messages, marks them seen and sends',
        (WidgetTester tester) async {
      _phoneSize(tester);
      List<String>? seenIds;
      final sent = <String>[];
      final api = ChatApi(
        userId: me,
        loadChats: () async => [
          ChatSummary(roomId: 'r1', otherUserId: 'u-1', nameAr: 'منى حسن', nameEn: 'Mona Hassan', detailAr: 'ولي أمر عمر',
              lastMessage: 'شكراً لك', lastMessageSenderId: 'u-1', lastMessageOn: now.subtract(const Duration(minutes: 5)), unreadCount: 2),
          ChatSummary(roomId: 'r2', otherUserId: 'u-2', nameAr: 'أحمد محمود', nameEn: 'Ahmed Mahmoud',
              lastMessage: 'سأرسل الواجب', lastMessageSenderId: me, lastMessageOn: now.subtract(const Duration(days: 1))),
        ],
        // Newest first, as the API returns them
        loadMessages: (roomId) async => [
          msg('m3', 'u-1', 'شكراً لك', 5),
          msg('m2', me, 'عمر متميز هذا الأسبوع', 10, status: ChatStatus.seen),
          msg('m1', 'u-1', 'كيف مستوى عمر؟', 20),
        ],
        seen: (roomId, ids) async => seenIds = ids,
        send: (roomId, content) async {
          sent.add(content);
          return ChatMessage(id: 'm4', senderId: me, content: content, sentOn: now);
        },
      );
      await tester.pumpWidget(MaterialApp(home: ChatListScreen(api: api, refreshEvery: null, now: now)));
      await tester.pumpAndSettle();

      expect(find.text('منى حسن'), findsOneWidget);
      expect(find.byKey(const ValueKey('unread-r1')), findsOneWidget);
      expect(find.text('2 رسالة غير مقروءة'), findsOneWidget);
      expect(find.text('أنت:'), findsOneWidget); // my last message to Ahmed
      expect(find.text('أمس'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('chat-r1')));
      await tester.pumpAndSettle();
      expect(find.text('ولي أمر عمر'), findsOneWidget);
      expect(find.text('كيف مستوى عمر؟'), findsOneWidget);
      expect(find.text('عمر متميز هذا الأسبوع'), findsOneWidget);
      expect(find.byKey(const ValueKey('seen-tick')), findsOneWidget);
      expect(find.text('اليوم'), findsOneWidget);
      // Their two messages are marked seen
      expect(seenIds, ['m3', 'm1']);

      await tester.enterText(find.byKey(const ValueKey('chat-input')), '  أهلاً، سأتابع معه  ');
      await tester.pump();
      await tester.tap(find.byKey(const ValueKey('chat-send')));
      await tester.pumpAndSettle();
      expect(sent, ['أهلاً، سأتابع معه']);
      expect(find.text('أهلاً، سأتابع معه'), findsOneWidget);
    });

    testWidgets('Profile pictures show in the chats list, the chat header and next to their messages',
        (WidgetTester tester) async {
      _phoneSize(tester);
      ChatAvatar.clearCache();
      // 1x1 PNG
      final png = base64Decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=');
      final asked = <String>[];
      final api = ChatApi(
        userId: me,
        loadPhoto: (userId) async {
          asked.add(userId);
          return userId == 'u-1' ? png : null;
        },
        loadChats: () async => [
          ChatSummary(roomId: 'r1', otherUserId: 'u-1', nameAr: 'منى حسن', nameEn: 'Mona', lastMessage: 'مرحباً', lastMessageOn: now),
          ChatSummary(roomId: 'r2', otherUserId: 'u-2', nameAr: 'أحمد محمود', nameEn: 'Ahmed', lastMessage: 'شكراً', lastMessageOn: now),
        ],
        loadMessages: (roomId) async => [msg('b', 'u-1', 'كيف الحال؟', 1), msg('a', 'u-1', 'مرحباً', 2)],
        seen: (roomId, ids) async {},
      );
      await tester.pumpWidget(MaterialApp(home: ChatListScreen(api: api, refreshEvery: null, now: now)));
      await tester.pumpAndSettle();

      // Mona has a picture, Ahmed shows his initial
      expect(find.byKey(const ValueKey('photo-u-1')), findsOneWidget);
      expect(find.byKey(const ValueKey('photo-u-2')), findsNothing);
      expect(find.text('أ'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('chat-r1')));
      await tester.pumpAndSettle();
      // Header + one picture for her two messages in a row
      expect(find.byKey(const ValueKey('photo-u-1')), findsNWidgets(2));
      // Loaded once per person (kept in memory)
      expect(asked.where((id) => id == 'u-1').length, 1);
    });

    testWidgets('Chats belong to the academic year: another year reloads its own chats', (WidgetTester tester) async {
      _phoneSize(tester);
      var loads = 0;
      final api = ChatApi(
        userId: me,
        loadChats: () async {
          loads++;
          return loads == 1
              ? [ChatSummary(roomId: 'r1', otherUserId: 'u-1', nameAr: 'منى حسن', nameEn: 'Mona', lastMessage: 'مرحباً', lastMessageOn: now)]
              : const <ChatSummary>[];
        },
      );
      await tester.pumpWidget(MaterialApp(home: ChatListScreen(api: api, refreshEvery: null, now: now)));
      await tester.pumpAndSettle();
      expect(find.text('منى حسن'), findsOneWidget);

      await AcademicYearController.instance.select(
          AcademicYear(id: 'y-old', name: '2025-2026', startDate: DateTime(2025, 9, 1), endDate: DateTime(2026, 6, 30)));
      await tester.pumpAndSettle();
      expect(loads, 2);
      expect(find.text('منى حسن'), findsNothing);
      // The year chosen on home is shown, not changed here
      expect(find.text('العام الدراسي 2025-2026'), findsOneWidget);
      expect(find.byType(AcademicYearChip), findsNothing);
      expect(find.text('لا توجد محادثات في هذا العام الدراسي. اضغط "محادثة جديدة" للبدء.'), findsOneWidget);
    });

    testWidgets('A message that fails can be sent again', (WidgetTester tester) async {
      _phoneSize(tester);
      var fail = true;
      final api = ChatApi(
        userId: me,
        loadMessages: (roomId) async => const [],
        send: (roomId, content) async {
          if (fail) throw const ChatException('x');
          return ChatMessage(id: 'ok', senderId: me, content: content, sentOn: now);
        },
      );
      await tester.pumpWidget(MaterialApp(home: ChatScreen(api: api, roomId: 'r', title: 'منى', refreshEvery: null, now: now)));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد رسائل بعد. اكتب أول رسالة.'), findsOneWidget);

      await tester.enterText(find.byKey(const ValueKey('chat-input')), 'مرحباً');
      await tester.pump();
      await tester.tap(find.byKey(const ValueKey('chat-send')));
      await tester.pumpAndSettle();
      expect(find.text('لم تُرسل، اضغط لإعادة المحاولة'), findsOneWidget);

      fail = false;
      await tester.tap(find.text('مرحباً'));
      await tester.pumpAndSettle();
      expect(find.text('لم تُرسل، اضغط لإعادة المحاولة'), findsNothing);
      expect(find.text('مرحباً'), findsOneWidget);
    });

    testWidgets('New chat: pick a contact, the chat with them opens', (WidgetTester tester) async {
      _phoneSize(tester);
      String? startedWith;
      final api = ChatApi(
        userId: me,
        loadChats: () async => const [],
        loadContacts: () async => const [
          ChatContact(userId: 'u-7', nameAr: 'سعاد علي', nameEn: 'Suad Ali', detailAr: 'رياضيات، علوم'),
          ChatContact(userId: 'u-8', nameAr: 'خالد سمير', nameEn: 'Khaled Samir', detailAr: 'لغة عربية'),
        ],
        start: (otherUserId) async {
          startedWith = otherUserId;
          return 'room-new';
        },
        loadMessages: (roomId) async => const [],
      );
      await tester.pumpWidget(MaterialApp(home: ChatListScreen(api: api, refreshEvery: null, now: now)));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد محادثات في هذا العام الدراسي. اضغط "محادثة جديدة" للبدء.'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('new-chat')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const ValueKey('contacts-search')), 'علوم');
      await tester.pump();
      expect(find.text('خالد سمير'), findsNothing);
      await tester.tap(find.byKey(const ValueKey('contact-u-7')));
      await tester.pumpAndSettle();

      expect(startedWith, 'u-7');
      expect(find.byType(ChatScreen), findsOneWidget);
      expect(find.text('سعاد علي'), findsOneWidget);
      expect(find.text('رياضيات، علوم'), findsOneWidget);
    });

    test('Chat JSON: times without a zone are UTC, statuses', () {
      final m = ChatMessage.fromJson({'id': 'a', 'senderId': 's', 'content': 'hi', 'sentOn': '2026-11-20T08:00:00', 'status': 3});
      expect(m.sentOn, DateTime.utc(2026, 11, 20, 8).toLocal());
      expect(m.status, ChatStatus.seen);
      final sent = ChatMessage.fromJson({'messageId': 'b', 'senderId': 's', 'content': 'x', 'sentOn': '2026-11-20T08:00:00Z', 'status': 1});
      expect(sent.id, 'b');
      expect(sent.status, ChatStatus.sent);
      final c = ChatSummary.fromJson({'roomId': 'r', 'otherUserId': 'u', 'nameAr': 'منى', 'nameEn': 'Mona', 'unreadCount': 3});
      expect(c.unreadCount, 3);
      expect(c.lastMessageOn, isNull);
    });
  });
}
