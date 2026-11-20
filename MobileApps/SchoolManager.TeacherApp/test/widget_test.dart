import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_teacher/core/academic_year/academic_year_controller.dart';
import 'package:smart_school_teacher/core/auth/auth_service.dart';
import 'package:smart_school_teacher/core/i18n/app_language.dart';
import 'package:smart_school_teacher/features/home/home_screen.dart';
import 'package:smart_school_teacher/features/homework/homework_screens.dart';
import 'package:smart_school_teacher/features/homework/homework_service.dart';
import 'package:smart_school_teacher/features/timetable/teacher_timetable_screen.dart';
import 'package:smart_school_teacher/features/timetable/teacher_timetable_service.dart';
import 'package:smart_school_teacher/features/profile/profile_service.dart';
import 'package:smart_school_teacher/features/school/school_info_service.dart';
import 'package:smart_school_teacher/main.dart';

void _phoneSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.6;
  addTearDown(tester.view.reset);
}

/// Home with test data, same language set-up as the app
Widget _home({MyProfile? profile, SchoolInfo? schoolInfo}) => ListenableBuilder(
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
          loadProfile: () async => profile,
          loadSchoolInfo: () async => schoolInfo,
          loadAcademicYears: () async => _years,
        ),
      ),
    );

/// Two academic years: 2026-2027 is the current one
final _years = [
  AcademicYear(id: 'y-2026', name: '2026-2027', startDate: DateTime(DateTime.now().year - 1, 1, 1), endDate: DateTime(DateTime.now().year + 1, 12, 31)),
  AcademicYear(id: 'y-2025', name: '2025-2026', startDate: DateTime(2000, 9, 1), endDate: DateTime(2001, 6, 30), isActive: false),
];

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await AcademicYearController.instance.reset();
  });

  testWidgets('New teacher: intro -> login, empty form is validated',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(const SmartSchoolTeacherApp());

    expect(find.text('مرحباً بك في تطبيق المعلم'), findsOneWidget);
    await tester.tap(find.text('تخطي'));
    await tester.pumpAndSettle();

    expect(find.text('تطبيق المعلم'), findsOneWidget);
    expect(find.text('تسجيل الدخول'), findsOneWidget);
    await tester.tap(find.text('دخول'));
    await tester.pump();
    expect(find.text('من فضلك أدخل اسم المستخدم'), findsOneWidget);
    expect(find.text('من فضلك أدخل كلمة المرور'), findsOneWidget);
  });

  testWidgets('Intro goes through the three teacher slides to login',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(const SmartSchoolTeacherApp());

    await tester.tap(find.text('التالي'));
    await tester.pumpAndSettle();
    expect(find.text('إدارة الفصل بسهولة'), findsOneWidget);
    await tester.tap(find.text('التالي'));
    await tester.pumpAndSettle();
    expect(find.text('تواصل دائم'), findsOneWidget);
    await tester.tap(find.text('ابدأ الآن'));
    await tester.pumpAndSettle();
    expect(find.text('تطبيق المعلم'), findsOneWidget);
  });

  test('Only accounts linked to a teacher count as teachers', () {
    final teacher = AuthSession.fromLoginResult({
      'userId': 'u1',
      'loginName': 'teacher1',
      'token': 't',
      'userProfile': {'firstName': 'منى', 'lastName': 'علي', 'teacherId': '7d1c'},
    });
    final parent = AuthSession.fromLoginResult({
      'userId': 'u2',
      'loginName': 'parent1',
      'token': 't',
      'userProfile': {'firstName': 'أحمد', 'parentId': '9a2b'},
    });
    expect(teacher.isTeacher, isTrue);
    expect(teacher.teacherId, '7d1c');
    expect(parent.isTeacher, isFalse);
    // Kept on the device with the teacher id
    expect(AuthSession.fromJson(teacher.toJson()).teacherId, '7d1c');
  });

  testWidgets('Home greets the teacher and shows the services',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(
      profile: const MyProfile(firstName: 'منى', lastName: 'علي', loginName: 'mona'),
      schoolInfo: const SchoolInfo(nameAr: 'مدرسة النور', nameEn: 'Al Noor School'),
    ));
    await tester.pumpAndSettle();

    expect(find.text('منى علي'), findsOneWidget);
    expect(find.text('معلم'), findsOneWidget);
    expect(find.text('مدرسة النور'), findsOneWidget);
    expect(find.text('إعلان هام'), findsOneWidget);
    expect(find.text('الخدمات'), findsOneWidget);
    expect(find.text('جدول حصصي'), findsOneWidget);
    expect(find.text('الحضور والغياب'), findsOneWidget);
    expect(find.text('رصد الدرجات'), findsOneWidget);

    // Services that are not ready yet say so
    await tester.tap(find.text('رصد الدرجات'));
    await tester.pump();
    expect(find.text('خدمة "رصد الدرجات" ستتوفر قريباً.'), findsOneWidget);
  });

  testWidgets('Bottom bar stays pinned on pages opened from the menu',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home());
    await tester.pumpAndSettle();

    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    expect(find.text('المظهر'), findsOneWidget);
    expect(find.text('اللغة'), findsOneWidget);
    await tester.tap(find.text('عن المدرسة'));
    await tester.pumpAndSettle();

    expect(find.text('لم تُضف بيانات المدرسة بعد.'), findsOneWidget);
    expect(find.text('الرئيسية'), findsOneWidget);

    await tester.tap(find.text('الرئيسية'));
    await tester.pumpAndSettle();
    expect(find.text('الخدمات'), findsOneWidget);
  });

  testWidgets('More menu switches the teacher app to English (LTR)',
      (WidgetTester tester) async {
    addTearDown(() => AppLanguage.instance.setLanguage(AppLanguage.arabic));
    _phoneSize(tester);
    await tester.pumpWidget(_home(
        profile: const MyProfile(firstName: 'Mona', lastName: 'Ali', loginName: 'mona')));
    await tester.pumpAndSettle();

    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('English'));
    await tester.pumpAndSettle();
    await tester.tapAt(const Offset(20, 20));
    await tester.pumpAndSettle();

    expect(find.text('Services'), findsOneWidget);
    expect(find.text('My timetable'), findsOneWidget);
    expect(find.text('Teacher'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('Services'))), TextDirection.ltr);
  });

  group('Homework', () {
    const math = TeacherSubject(
      subjectId: 'sub-math',
      subjectNameAr: 'رياضيات',
      subjectNameEn: 'Math',
      gradeId: 'g1',
      gradeNameAr: 'الصف الأول',
      gradeNameEn: 'Grade 1',
      sections: [
        TeacherSection(id: 's-a', nameAr: 'فصل أ', nameEn: 'Class A'),
        TeacherSection(id: 's-b', nameAr: 'فصل ب', nameEn: 'Class B'),
      ],
    );
    const classes = TeacherClasses(subjects: [math]);

    TeacherHomework homework(String id, String title, {List<PublishedSection> published = const []}) => TeacherHomework(
          id: id,
          subjectId: 'sub-math',
          subjectNameAr: 'رياضيات',
          subjectNameEn: 'Math',
          gradeId: 'g1',
          gradeNameAr: 'الصف الأول',
          gradeNameEn: 'Grade 1',
          title: title,
          description: 'حل التمارين صفحة 20',
          dueDate: DateTime.now().add(const Duration(days: 2)),
          questions: const [HomeworkQuestion(header: 'احسب', items: ['2 + 3', '4 + 5'])],
          publishedSections: published,
        );

    testWidgets('Home tile opens the homework list; publishing sends the chosen classes',
        (WidgetTester tester) async {
      _phoneSize(tester);
      var list = [homework('h1', 'واجب الجمع')];
      List<String>? publishedTo;
      final api = HomeworkApi(
        loadClasses: () async => classes,
        loadHomeworks: () async => list,
        publish: (id, sections) async {
          publishedTo = sections;
          list = [
            homework('h1', 'واجب الجمع',
                published: [PublishedSection(sectionId: 's-a', nameAr: 'فصل أ', nameEn: 'Class A', publishedOn: DateTime.now())])
          ];
          return sections.length;
        },
      );

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
          home: HomeScreen(loadProfile: () async => null, loadSchoolInfo: () async => null, homeworkApi: api),
        ),
      ));
      await tester.pumpAndSettle();

      await tester.ensureVisible(find.text('الواجبات'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('الواجبات'));
      await tester.pumpAndSettle();

      expect(find.text('واجب الجمع'), findsOneWidget);
      expect(find.text('غير منشور'), findsOneWidget);
      // The bottom bar stays on the homework page
      expect(find.text('الرئيسية'), findsOneWidget);

      await tester.tap(find.text('نشر'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('فصل أ'));
      await tester.pump();
      await tester.tap(find.byKey(const ValueKey('confirm-publish')));
      await tester.pumpAndSettle();

      expect(publishedTo, ['s-a']);
      expect(find.text('منشور'), findsOneWidget);
      expect(find.text('فصل أ'), findsOneWidget); // published class chip on the card
    });

    testWidgets('New homework form saves with questions and publishes to the chosen class',
        (WidgetTester tester) async {
      _phoneSize(tester);
      Map<String, Object?>? created;
      List<String>? publishedTo;
      final api = HomeworkApi(
        create: ({required subjectId, required gradeId, required title, required description, required dueDate, questions = const []}) async {
          created = {'subject': subjectId, 'grade': gradeId, 'title': title, 'questions': questions.length, 'items': questions.first.items};
          return 'new-id';
        },
        publish: (id, sections) async {
          publishedTo = [id, ...sections];
          return sections.length;
        },
      );

      await tester.pumpWidget(MaterialApp(home: HomeworkFormScreen(api: api, classes: classes)));
      await tester.pumpAndSettle();

      // One subject: chosen already
      await tester.enterText(find.byKey(const ValueKey('homework-title')), 'واجب الطرح');
      await tester.tap(find.byKey(const ValueKey('add-question')));
      await tester.pump();
      await tester.enterText(find.widgetWithText(TextFormField, 'نص السؤال'), 'أوجد الناتج');
      await tester.enterText(find.widgetWithText(TextFormField, 'النقاط أو الخيارات (كل سطر نقطة) - اختياري'), '5 - 2\n9 - 4');
      await tester.ensureVisible(find.text('فصل ب'));
      await tester.tap(find.text('فصل ب'));
      await tester.pump();
      expect(find.text('حفظ ونشر'), findsOneWidget);

      await tester.ensureVisible(find.byKey(const ValueKey('save-homework')));
      await tester.tap(find.byKey(const ValueKey('save-homework')));
      await tester.pumpAndSettle();

      expect(created?['subject'], 'sub-math');
      expect(created?['grade'], 'g1');
      expect(created?['title'], 'واجب الطرح');
      expect(created?['questions'], 1);
      expect(created?['items'], ['5 - 2', '9 - 4']);
      expect(publishedTo, ['new-id', 's-b']);
    });

    testWidgets('New homework needs a title', (WidgetTester tester) async {
      _phoneSize(tester);
      var called = false;
      final api = HomeworkApi(
        create: ({required subjectId, required gradeId, required title, required description, required dueDate, questions = const []}) async {
          called = true;
          return 'x';
        },
      );
      await tester.pumpWidget(MaterialApp(home: HomeworkFormScreen(api: api, classes: classes)));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('save-homework')));
      await tester.pumpAndSettle();
      expect(find.text('من فضلك أدخل عنوان الواجب'), findsOneWidget);
      expect(called, isFalse);
    });
  });

  testWidgets('Teacher home shows the current academic year and the menu can change it',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_home(profile: const MyProfile(firstName: 'منى', lastName: 'علي', loginName: 'mona')));
    await tester.pumpAndSettle();

    expect(find.text('2026-2027'), findsOneWidget);
    expect(AcademicYearController.instance.withYear(Uri.parse('/api/homework/my')).toString(),
        '/api/homework/my?academicYearId=y-2026');

    await tester.tap(find.text('المزيد'));
    await tester.pumpAndSettle();
    expect(find.text('السنة الدراسية'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('academic-year')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('2025-2026').last);
    await tester.pumpAndSettle();
    expect(AcademicYearController.instance.selectedId, 'y-2025');
  });

  group('Timetable', () {
    TeacherLesson lesson(String id, String subject, int start, int end, String section) => TeacherLesson(
          id: id,
          start: start,
          end: end,
          subjectNameAr: subject,
          subjectNameEn: subject,
          gradeNameAr: 'الصف الأول',
          sectionNameAr: section,
        );

    // Tuesday (2) 10:00: science now (9:30-10:15), English next; Sunday has Quran
    final week = TeacherTimetable(today: 2, now: 600, days: [
      TeacherTimetableDay(dayOfWeek: 0, lessons: [lesson('s1', 'قرآن', 450, 495, 'فصل ب')]),
      TeacherTimetableDay(dayOfWeek: 2, lessons: [
        lesson('t1', 'رياضيات', 480, 525, 'فصل أ'),
        lesson('t2', 'علوم', 570, 615, 'فصل أ'),
        lesson('t3', 'إنجليزي', 630, 675, 'فصل ب'),
      ]),
    ]);

    testWidgets('Home tile opens my timetable: lesson now, next, and another day', (WidgetTester tester) async {
      _phoneSize(tester);
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
            loadProfile: () async => null,
            loadSchoolInfo: () async => null,
            loadAcademicYears: () async => _years,
            loadTimetable: () async => week,
          ),
        ),
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('جدول حصصي'));
      await tester.pumpAndSettle();

      // Today card: science now, English next
      final today = find.byKey(const ValueKey('teacher-today-card'));
      expect(find.descendant(of: today, matching: find.text('الآن')), findsOneWidget);
      expect(find.descendant(of: today, matching: find.text('علوم')), findsOneWidget);
      expect(find.descendant(of: today, matching: find.text('1/3')), findsOneWidget);
      // Today's list with states
      expect(find.text('اليوم - الثلاثاء'), findsOneWidget);
      expect(find.text('انتهت'), findsOneWidget); // math
      expect(find.text('التالية'), findsOneWidget); // English
      // Subtitle: the chosen academic year
      expect(find.text('2026-2027'), findsOneWidget);
      // Bottom bar stays
      expect(find.text('الرئيسية'), findsOneWidget);

      // Sunday
      await tester.tap(find.byKey(const ValueKey('day-0')));
      await tester.pumpAndSettle();
      expect(find.text('قرآن'), findsOneWidget);
      expect(find.text('الصف الأول · فصل ب'), findsOneWidget);
    });

    testWidgets('No lessons in the year / semester shows a message', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(
        home: TeacherTimetableScreen(
          loadTimetable: () async => const TeacherTimetable(today: 2, now: 600, days: []),
        ),
      ));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد حصص لك في هذه السنة / الفصل الدراسي.'), findsOneWidget);
    });

    test('Timetable JSON: day names or numbers, times as minutes', () {
      final t = TeacherTimetable.fromJson({
        'today': 'Tuesday',
        'now': '10:05:00',
        'days': [
          {
            'dayOfWeek': 2,
            'slots': [
              {'id': 'a', 'startTime': '09:30:00', 'endTime': '10:15:00', 'subjectNameAr': 'علوم', 'subjectNameEn': 'Science', 'sectionNameAr': 'فصل أ'},
            ],
          },
        ],
      });
      expect(t.today, 2);
      expect(t.now, 605);
      expect(t.dayOf(2)!.lessons.single.start, 570);
      expect(t.dayOf(2)!.lessons.single.subject, 'علوم');
    });
  });
}
