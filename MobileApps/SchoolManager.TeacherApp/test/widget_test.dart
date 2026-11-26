import 'dart:typed_data';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_teacher/features/attendance/attendance_screens.dart';
import 'package:smart_school_teacher/features/attendance/attendance_service.dart';
import 'package:smart_school_teacher/features/chat/chat_media.dart';
import 'package:smart_school_teacher/features/activity/student_activity_screen.dart';
import 'package:smart_school_teacher/features/activity/student_activity_service.dart';
import 'package:smart_school_teacher/features/chat/chat_screens.dart';
import 'package:smart_school_teacher/features/notifications/notifications_screen.dart';
import 'package:smart_school_teacher/features/notifications/notifications_service.dart';
import 'package:smart_school_teacher/features/chat/chat_service.dart';
import 'package:smart_school_teacher/core/academic_year/academic_year_controller.dart';
import 'package:smart_school_teacher/core/academic_year/academic_year_picker.dart';
import 'package:smart_school_teacher/core/auth/auth_service.dart';
import 'package:smart_school_teacher/core/i18n/app_language.dart';
import 'package:smart_school_teacher/features/classes/classes_screens.dart';
import 'package:smart_school_teacher/features/classes/classes_service.dart';
import 'package:smart_school_teacher/features/events/events_screen.dart';
import 'package:smart_school_teacher/features/parents/parents_screen.dart';
import 'package:smart_school_teacher/features/parents/parents_service.dart';
import 'package:smart_school_teacher/features/events/events_service.dart';
import 'package:smart_school_teacher/features/exams/exam_screens.dart';
import 'package:smart_school_teacher/features/exams/exam_service.dart';
import 'package:smart_school_teacher/features/home/home_screen.dart';
import 'package:smart_school_teacher/features/homework/homework_details_screen.dart';
import 'package:smart_school_teacher/features/homework/homework_screens.dart';
import 'package:smart_school_teacher/features/homework/homework_service.dart';
import 'package:smart_school_teacher/features/timetable/teacher_timetable_screen.dart';
import 'package:smart_school_teacher/features/timetable/teacher_timetable_service.dart';
import 'package:smart_school_teacher/features/profile/edit_profile_screen.dart';
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
    // No upcoming event: no important notice
    expect(find.text('إعلان هام'), findsNothing);
    expect(find.text('الخدمات'), findsOneWidget);
    expect(find.text('جدول حصصي'), findsOneWidget);
    expect(find.text('الحضور والغياب'), findsOneWidget);
    expect(find.text('الاختبارات والدرجات'), findsOneWidget);

    expect(find.text('تقييم الطلاب'), findsOneWidget);
  });

  testWidgets('Important notice shows the next upcoming event; tapping it opens the events',
      (WidgetTester tester) async {
    _phoneSize(tester);
    final today = DateTime.now();
    final day = DateTime(today.year, today.month, today.day);
    SchoolEvent event(String id, String title, int inDays, EventAudience audience) => SchoolEvent(
          id: id,
          title: title,
          description: '',
          date: day.add(Duration(days: inDays)),
          start: 13 * 60 + 30,
          end: 15 * 60,
          audience: audience,
        );
    await tester.pumpWidget(MaterialApp(
      home: HomeScreen(
        loadProfile: () async => null,
        loadSchoolInfo: () async => null,
        loadEvents: () async => [
          event('e1', 'اجتماع المعلمين', 3, EventAudience.teacher),
          event('e2', 'حفل التكريم', 10, EventAudience.all),
          event('e3', 'ورشة سابقة', -1, EventAudience.teacher),
        ],
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('إعلان هام'), findsOneWidget);
    expect(find.text('اجتماع المعلمين'), findsOneWidget);
    expect(find.textContaining('بعد 3 أيام · 1:30'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('announcement')));
    await tester.pumpAndSettle();
    expect(find.byType(EventsScreen), findsOneWidget);
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

  testWidgets('Job level is shown on the home header and read only in the profile',
      (WidgetTester tester) async {
    _phoneSize(tester);
    const profile = MyProfile(firstName: 'منى', lastName: 'علي', loginName: 'mona', jobLevel: 'معلم أول');
    await tester.pumpWidget(_home(profile: profile));
    await tester.pumpAndSettle();

    // The chip shows the job level instead of the plain "Teacher"
    expect(find.text('معلم أول'), findsOneWidget);
    expect(find.text('معلم'), findsNothing);

    // Profile: the job level is in a disabled, read-only field
    await tester.pumpWidget(MaterialApp(
      locale: AppLanguage.instance.value,
      supportedLocales: AppLanguage.supported,
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: const EditProfileScreen(profile: profile),
    ));
    await tester.pumpAndSettle();

    final finder = find.byKey(const ValueKey('jobLevelField'));
    expect(finder, findsOneWidget);
    expect(find.text('المستوى الوظيفي'), findsOneWidget);
    final field = tester.widget<TextField>(find.descendant(of: finder, matching: find.byType(TextField)));
    expect(field.readOnly, isTrue);
    expect(field.enabled, isFalse);
    expect(field.controller?.text, 'معلم أول');
  });

  testWidgets('No job level: the home chip says Teacher and the profile has no job level field',
      (WidgetTester tester) async {
    _phoneSize(tester);
    const profile = MyProfile(firstName: 'منى', lastName: 'علي', loginName: 'mona');
    await tester.pumpWidget(MaterialApp(
      locale: AppLanguage.instance.value,
      supportedLocales: AppLanguage.supported,
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: const EditProfileScreen(profile: profile),
    ));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('jobLevelField')), findsNothing);
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
      // A header with several sub-questions under it (a new question starts with one sub-question field)
      await tester.enterText(find.byKey(const ValueKey('question-0-header')), 'أوجد الناتج');
      await tester.enterText(find.byKey(const ValueKey('question-0-item-0')), '5 - 2');
      await tester.ensureVisible(find.byKey(const ValueKey('question-0-add-item')));
      await tester.tap(find.byKey(const ValueKey('question-0-add-item')));
      await tester.pump();
      await tester.enterText(find.byKey(const ValueKey('question-0-item-1')), '9 - 4');
      await tester.ensureVisible(find.byKey(const ValueKey('question-0-add-item')));
      await tester.tap(find.byKey(const ValueKey('question-0-add-item')));
      await tester.pump();
      await tester.enterText(find.byKey(const ValueKey('question-0-item-2')), '7 - 1');
      expect(find.text('أ)'), findsOneWidget);
      expect(find.text('ج)'), findsOneWidget);
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
      expect(created?['items'], ['5 - 2', '9 - 4', '7 - 1']);
      expect(publishedTo, ['new-id', 's-b']);
    });

    testWidgets('Edit homework: fields are filled, sub-questions can be added and removed, then saved',
        (WidgetTester tester) async {
      _phoneSize(tester);
      Map<String, Object?>? updated;
      final api = HomeworkApi(
        update: (id, {required title, required description, required dueDate, questions = const []}) async {
          updated = {'id': id, 'title': title, 'questions': questions.map((q) => '${q.header}: ${q.items.join(' | ')}').toList()};
        },
      );

      await tester.pumpWidget(MaterialApp(
        home: HomeworkFormScreen(api: api, classes: classes, homework: homework('h1', 'واجب الجمع')),
      ));
      await tester.pumpAndSettle();

      // Filled from the saved homework; the subject cannot change
      expect(find.text('تعديل الواجب'), findsOneWidget);
      expect(find.text('واجب الجمع'), findsOneWidget);
      expect(find.byKey(const ValueKey('homework-subject')), findsNothing);
      expect(find.text('رياضيات - الصف الأول'), findsOneWidget);
      // Saved question "احسب" with sub-questions 2 + 3 / 4 + 5; no publish chips when editing
      expect(find.text('احسب'), findsOneWidget);
      expect(find.text('4 + 5'), findsOneWidget);
      expect(find.text('النشر الآن لفصول (اختياري)'), findsNothing);

      // Remove the first sub-question, add another one
      await tester.tap(find.byTooltip('حذف السؤال الفرعي').first);
      await tester.pump();
      await tester.ensureVisible(find.byKey(const ValueKey('question-0-add-item')));
      await tester.tap(find.byKey(const ValueKey('question-0-add-item')));
      await tester.pump();
      await tester.enterText(find.byKey(const ValueKey('question-0-item-1')), '6 + 1');

      await tester.ensureVisible(find.byKey(const ValueKey('save-homework')));
      await tester.tap(find.byKey(const ValueKey('save-homework')));
      await tester.pumpAndSettle();

      expect(updated?['id'], 'h1');
      expect(updated?['title'], 'واجب الجمع');
      expect(updated?['questions'], ['احسب: 4 + 5 | 6 + 1']);
    });

    testWidgets('Tapping a homework opens its details page; edit from there opens the form',
        (WidgetTester tester) async {
      _phoneSize(tester);
      final api = HomeworkApi(
        loadClasses: () async => classes,
        loadHomeworks: () async => [
          homework('h1', 'واجب الجمع',
              published: [PublishedSection(sectionId: 's-a', nameAr: 'فصل أ', nameEn: 'Class A', publishedOn: DateTime.now())])
        ],
      );
      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(loadProfile: () async => null, loadSchoolInfo: () async => null, homeworkApi: api),
      ));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('الواجبات'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('الواجبات'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('واجب الجمع'));
      await tester.pumpAndSettle();
      expect(find.byType(HomeworkDetailsScreen), findsOneWidget);
      expect(find.text('حل التمارين صفحة 20'), findsOneWidget);
      expect(find.text('احسب'), findsOneWidget);
      expect(find.text('2 + 3'), findsOneWidget);
      expect(find.text('4 + 5'), findsOneWidget);
      expect(find.text('منشور في'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('details-edit')));
      await tester.pumpAndSettle();
      expect(find.byType(HomeworkDetailsScreen), findsNothing);
      expect(find.text('تعديل الواجب'), findsOneWidget);
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

  group('Exams', () {
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
    final today = DateTime.now();

    TeacherExam exam(String id, TeacherExamType type, {int? month, int results = 0}) => TeacherExam(
          id: id,
          subjectId: 'sub-math',
          subjectNameAr: 'رياضيات',
          subjectNameEn: 'Math',
          gradeId: 'g1',
          gradeNameAr: 'الصف الأول',
          gradeNameEn: 'Grade 1',
          sectionId: 's-a',
          sectionNameAr: 'فصل أ',
          sectionNameEn: 'Class A',
          type: type,
          month: month,
          date: DateTime(today.year, today.month, today.day),
          startMinutes: 9 * 60,
          endMinutes: 9 * 60 + 45,
          weight: 20,
          successScore: 10,
          resultCount: results,
        );

    const scores = ExamScores(weight: 20, successScore: 10, students: [
      ExamStudentScore(studentId: 'st-1', nameAr: 'سارة أحمد', nameEn: 'Sara Ahmed', score: 18),
      ExamStudentScore(studentId: 'st-2', nameAr: 'عمر خالد', nameEn: 'Omar Khaled'),
    ]);

    testWidgets('Exams of my subjects by the school or other teachers are listed read-only; scores can be entered',
        (WidgetTester tester) async {
      _phoneSize(tester);
      final mid = TeacherExam(
        id: 'e-mid',
        subjectId: 'sub-math',
        subjectNameAr: 'رياضيات',
        subjectNameEn: 'Math',
        gradeId: 'g1',
        gradeNameAr: 'الصف الأول',
        gradeNameEn: 'Grade 1',
        // Whole grade
        sectionId: '',
        sectionNameAr: '',
        sectionNameEn: '',
        type: TeacherExamType.monthly,
        month: 11,
        date: DateTime(today.year, today.month, today.day),
        startMinutes: 9 * 60,
        endMinutes: 11 * 60,
        weight: 50,
        successScore: 25,
        isMine: false,
        addedByTeacher: false,
      );
      final otherTeacher = TeacherExam.fromJson({
        'id': 'e-other',
        'subjectId': 'sub-math',
        'subjectNameAr': 'رياضيات',
        'gradeId': 'g1',
        'gradeNameAr': 'الصف الأول',
        'sectionId': 's-b',
        'sectionNameAr': 'فصل ب',
        'examType': 1,
        'examDate': '2026-11-20',
        'startTime': '10:00:00',
        'endTime': '10:30:00',
        'weight': 10,
        'successScore': 5,
        'isMine': false,
        'addedByTeacher': true,
      });
      String? scoresOf;
      final api = ExamApi(
        loadClasses: () async => classes,
        loadExams: () async => [exam('e1', TeacherExamType.quiz), mid, otherTeacher],
        loadScores: (id) async {
          scoresOf = id;
          return scores;
        },
      );
      await tester.pumpWidget(MaterialApp(home: ExamListScreen(api: api)));
      await tester.pumpAndSettle();

      // My quiz can be edited / deleted; the others cannot and say who added them
      expect(find.byKey(const ValueKey('edit-exam-e1')), findsOneWidget);
      expect(find.byKey(const ValueKey('edit-exam-e-mid')), findsNothing);
      expect(find.byKey(const ValueKey('delete-exam-e-other')), findsNothing);
      expect(find.text('من إدارة المدرسة'), findsOneWidget);
      expect(find.text('أضافه معلم آخر'), findsOneWidget);
      expect(find.text('رياضيات · الصف الأول · كل الفصول'), findsOneWidget);
      // Quizzes and monthly exams only: no mid-term / final filter
      expect(find.byKey(const ValueKey('exam-filter-school')), findsNothing);

      // Filter: mine
      await tester.tap(find.byKey(const ValueKey('exam-filter-mine')));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('scores-e1')), findsOneWidget);
      expect(find.byKey(const ValueKey('scores-e-mid')), findsNothing);
      await tester.tap(find.byKey(const ValueKey('exam-filter-all')));
      await tester.pumpAndSettle();

      // The school's exam: scores open
      await tester.tap(find.byKey(const ValueKey('scores-e-mid')));
      await tester.pumpAndSettle();
      expect(scoresOf, 'e-mid');
      expect(find.text('سارة أحمد'), findsOneWidget);
    });

    testWidgets('The new exam form offers only quiz and monthly', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(home: ExamFormScreen(api: const ExamApi(), classes: classes)));
      await tester.pumpAndSettle();
      expect(find.text('اختبار قصير'), findsOneWidget);
      expect(find.text('امتحان شهري'), findsOneWidget);
    });

    test('Only quizzes and monthly exams are shown (mid-term = 3, final = 4 are not)', () {
      expect(TeacherExamType.isShown(1), isTrue);
      expect(TeacherExamType.isShown(2), isTrue);
      expect(TeacherExamType.isShown(3), isFalse);
      expect(TeacherExamType.isShown(4), isFalse);
      expect(TeacherExamType.values, [TeacherExamType.quiz, TeacherExamType.monthly]);
    });

    testWidgets('Home tile opens the exams; filter by type; scores are entered and only the changes saved',
        (WidgetTester tester) async {
      _phoneSize(tester);
      Map<String, double?>? saved;
      final api = ExamApi(
        loadClasses: () async => classes,
        loadExams: () async => [exam('e1', TeacherExamType.quiz, results: 1), exam('e2', TeacherExamType.monthly, month: 11)],
        loadScores: (id) async => scores,
        saveScores: (id, s) async => saved = s,
      );
      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(loadProfile: () async => null, loadSchoolInfo: () async => null, examApi: api),
      ));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('الاختبارات والدرجات'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('الاختبارات والدرجات'));
      await tester.pumpAndSettle();

      expect(find.text('اختبار قصير'), findsOneWidget);
      expect(find.text('امتحان شهر نوفمبر'), findsOneWidget);
      expect(find.text('رُصدت 1 درجة'), findsOneWidget);
      // The bottom bar stays on the exams page
      expect(find.text('الرئيسية'), findsOneWidget);

      await tester.tap(find.text('شهرية (1)'));
      await tester.pumpAndSettle();
      expect(find.text('اختبار قصير'), findsNothing);
      expect(find.text('امتحان شهر نوفمبر'), findsOneWidget);
      await tester.tap(find.text('الكل (2)'));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('scores-e1')));
      await tester.pumpAndSettle();
      expect(find.text('سارة أحمد'), findsOneWidget);
      expect(find.text('ناجح'), findsWidgets); // Sara 18/20
      expect(find.text('لم تُرصد'), findsOneWidget); // Omar
      expect(find.text('1/2'), findsOneWidget);

      await tester.enterText(find.byKey(const ValueKey('score-st-2')), '7');
      await tester.pump();
      expect(find.text('راسب'), findsOneWidget);
      expect(find.text('2/2'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('save-scores')));
      await tester.pumpAndSettle();
      expect(saved, {'st-2': 7.0});
      expect(find.byType(ExamScoresScreen), findsNothing);
    });

    testWidgets('A score above the full mark is not saved', (WidgetTester tester) async {
      _phoneSize(tester);
      var called = false;
      final api = ExamApi(loadScores: (id) async => scores, saveScores: (id, s) async => called = true);
      await tester.pumpWidget(MaterialApp(home: ExamScoresScreen(api: api, exam: exam('e1', TeacherExamType.quiz))));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(const ValueKey('score-st-2')), '25');
      await tester.pump();
      expect(find.text('من 0 إلى 20'), findsOneWidget);
      await tester.tap(find.byKey(const ValueKey('save-scores')));
      await tester.pumpAndSettle();
      expect(called, isFalse);
      expect(find.text('صحّح الدرجات المظللة بالأحمر (من 0 إلى 20).'), findsOneWidget);
    });

    testWidgets('New monthly exam: month, class and marks are sent', (WidgetTester tester) async {
      _phoneSize(tester);
      ExamInput? created;
      final api = ExamApi(create: (input) async {
        created = input;
        return 'new-exam';
      });
      await tester.pumpWidget(MaterialApp(home: ExamFormScreen(api: api, classes: classes, now: DateTime(2026, 11, 20))));
      await tester.pumpAndSettle();

      // Only the two types a teacher may add
      expect(find.text('اختبار قصير'), findsOneWidget);
      expect(find.text('امتحان شهري'), findsOneWidget);
      expect(find.byKey(const ValueKey('exam-month')), findsNothing);

      await tester.tap(find.byKey(const ValueKey('exam-type-monthly')));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('exam-month')), findsOneWidget);
      expect(find.text('نوفمبر'), findsOneWidget); // month of the exam date

      // The class must be chosen
      await tester.ensureVisible(find.byKey(const ValueKey('save-exam')));
      await tester.tap(find.byKey(const ValueKey('save-exam')));
      await tester.pumpAndSettle();
      expect(find.text('اختر الفصل'), findsOneWidget);
      expect(created, isNull);

      await tester.ensureVisible(find.text('فصل ب'));
      await tester.tap(find.text('فصل ب'));
      await tester.pump();
      await tester.enterText(find.byKey(const ValueKey('exam-weight')), '30');
      await tester.enterText(find.byKey(const ValueKey('exam-pass')), '15');
      await tester.ensureVisible(find.byKey(const ValueKey('save-exam')));
      await tester.tap(find.byKey(const ValueKey('save-exam')));
      await tester.pumpAndSettle();

      expect(created?.type, TeacherExamType.monthly);
      expect(created?.month, 11);
      expect(created?.sectionId, 's-b');
      expect(created?.subjectId, 'sub-math');
      expect(created?.weight, 30);
      expect(created?.successScore, 15);
      expect(created?.toJson()['examType'], 2);
      expect(created?.toJson()['examDate'], '2026-11-20');
    });

    testWidgets('Pass mark above the full mark is refused', (WidgetTester tester) async {
      _phoneSize(tester);
      var called = false;
      final api = ExamApi(create: (input) async {
        called = true;
        return 'x';
      });
      await tester.pumpWidget(MaterialApp(home: ExamFormScreen(api: api, classes: classes)));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('فصل أ'));
      await tester.tap(find.text('فصل أ'));
      await tester.enterText(find.byKey(const ValueKey('exam-weight')), '10');
      await tester.enterText(find.byKey(const ValueKey('exam-pass')), '12');
      await tester.ensureVisible(find.byKey(const ValueKey('save-exam')));
      await tester.tap(find.byKey(const ValueKey('save-exam')));
      await tester.pumpAndSettle();
      expect(find.text('أكبر من الدرجة الكاملة'), findsOneWidget);
      expect(called, isFalse);
    });

    testWidgets('Edit exam: filled from the saved exam and updated', (WidgetTester tester) async {
      _phoneSize(tester);
      String? updatedId;
      ExamInput? updated;
      final api = ExamApi(update: (id, input) async {
        updatedId = id;
        updated = input;
      });
      await tester.pumpWidget(MaterialApp(
        home: ExamFormScreen(api: api, classes: classes, exam: exam('e2', TeacherExamType.monthly, month: 10)),
      ));
      await tester.pumpAndSettle();

      expect(find.text('تعديل الاختبار'), findsOneWidget);
      expect(find.text('رياضيات - الصف الأول'), findsOneWidget);
      expect(find.text('أكتوبر'), findsOneWidget);
      await tester.enterText(find.byKey(const ValueKey('exam-weight')), '25');
      await tester.ensureVisible(find.byKey(const ValueKey('save-exam')));
      await tester.tap(find.byKey(const ValueKey('save-exam')));
      await tester.pumpAndSettle();

      expect(updatedId, 'e2');
      expect(updated?.month, 10);
      expect(updated?.sectionId, 's-a');
      expect(updated?.weight, 25);
    });

    test('Exam JSON: type, month, times and marks', () {
      final e = TeacherExam.fromJson({
        'id': 'x',
        'examType': 2,
        'month': 3,
        'examDate': '2027-03-14',
        'startTime': '09:30:00',
        'endTime': '10:15:00',
        'weight': 20,
        'successScore': 12.5,
        'resultCount': 4,
        'averageScore': 15.25,
      });
      expect(e.type, TeacherExamType.monthly);
      expect(e.month, 3);
      expect(e.date, DateTime(2027, 3, 14));
      expect(e.startMinutes, 570);
      expect(e.endMinutes, 615);
      expect(examMark(e.successScore), '12.5');
      expect(examMark(e.weight), '20');
      expect(e.resultCount, 4);
    });
  });

  group('Events', () {
    final now = DateTime(2026, 11, 20, 10, 0);
    final today = DateTime.now();
    SchoolEvent event(String id, String title, int inDays, EventAudience audience) => SchoolEvent(
          id: id,
          title: title,
          description: 'وصف $title',
          date: DateTime(today.year, today.month, today.day).add(Duration(days: inDays)),
          start: 9 * 60,
          end: 12 * 60,
          audience: audience,
        );

    testWidgets('Home tile opens the events for everyone / for teachers: upcoming, past and details',
        (WidgetTester tester) async {
      _phoneSize(tester);
      var loads = 0;
      final events = [
        event('1', 'اجتماع المعلمين', 3, EventAudience.teacher),
        event('2', 'اليوم الرياضي', 1, EventAudience.all),
        event('3', 'حفل الافتتاح', -10, EventAudience.all),
      ];
      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(
          loadProfile: () async => null,
          loadSchoolInfo: () async => null,
          loadEvents: () async {
            loads++;
            return events;
          },
        ),
      ));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('الفعاليات'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('الفعاليات'));
      await tester.pumpAndSettle();
      expect(loads, 2); // home (important notice) + the events page

      // Upcoming: tomorrow's sports day first (big card), then the teachers' meeting
      expect(find.text('القادمة (2)'), findsOneWidget);
      expect(find.text('اليوم الرياضي'), findsOneWidget);
      expect(find.text('اجتماع المعلمين'), findsOneWidget);
      expect(find.text('للمعلمين'), findsOneWidget);
      expect(find.text('للجميع'), findsOneWidget);
      expect(find.text('حفل الافتتاح'), findsNothing);
      // The bottom bar stays on the events page
      expect(find.text('الرئيسية'), findsOneWidget);

      await tester.tap(find.text('السابقة (1)'));
      await tester.pumpAndSettle();
      expect(find.text('حفل الافتتاح'), findsOneWidget);

      await tester.tap(find.text('حفل الافتتاح'));
      await tester.pumpAndSettle();
      expect(find.text('وصف حفل الافتتاح'), findsOneWidget);
    });

    testWidgets('No events shows a message', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(home: EventsScreen(loadEvents: () async => const [], now: now)));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد فعاليات قادمة حالياً.'), findsOneWidget);
    });

    test('Event JSON: date, times and audience', () {
      final e = SchoolEvent.fromJson({
        'id': 'e',
        'title': ' اجتماع ',
        'eventDate': '2026-12-01',
        'startTime': '08:30:00',
        'endTime': '10:00:00',
        'eventFor': 5,
        'imageUrl': '',
      });
      expect(e.title, 'اجتماع');
      expect(e.date, DateTime(2026, 12, 1));
      expect(e.start, 510);
      expect(e.end, 600);
      expect(e.audience, EventAudience.teacher);
      expect(e.imageUrl, isNull);
    });
  });

  group('My classes', () {
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
    const science = TeacherSubject(
      subjectId: 'sub-sci',
      subjectNameAr: 'علوم',
      subjectNameEn: 'Science',
      gradeId: 'g1',
      gradeNameAr: 'الصف الأول',
      gradeNameEn: 'Grade 1',
      sections: [TeacherSection(id: 's-a', nameAr: 'فصل أ', nameEn: 'Class A')],
    );
    const physics = TeacherSubject(
      subjectId: 'sub-phy',
      subjectNameAr: 'فيزياء',
      subjectNameEn: 'Physics',
      gradeId: 'g2',
      gradeNameAr: 'الصف الثاني',
      gradeNameEn: 'Grade 2',
      sections: [TeacherSection(id: 's-c', nameAr: 'فصل ج', nameEn: 'Class C')],
    );

    test('One class per section with every subject taught in it', () {
      final classes = myClassesFrom(const TeacherClasses(subjects: [math, science, physics]));
      expect(classes.map((c) => c.section.id), ['s-a', 's-b', 's-c']);
      expect(classes.first.subjects.map((s) => s.subjectId), ['sub-math', 'sub-sci']);
      expect(classes[1].subjects.map((s) => s.subjectId), ['sub-math']);
      expect(classes.last.label, 'الصف الثاني - فصل ج');
    });

    testWidgets('Home tile opens my classes by grade; a class shows its students, search and details',
        (WidgetTester tester) async {
      _phoneSize(tester);
      String? asked;
      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(
          loadProfile: () async => null,
          loadSchoolInfo: () async => null,
          loadMyClasses: () async => myClassesFrom(const TeacherClasses(subjects: [math, science, physics])),
          loadClassStudents: (sectionId) async {
            asked = sectionId;
            return [
              ClassStudent(id: 'st-1', nameAr: 'سارة أحمد', nameEn: 'Sara Ahmed', gender: 'Female', dateOfBirth: DateTime(2018, 1, 5)),
              const ClassStudent(id: 'st-2', nameAr: 'عمر خالد', nameEn: 'Omar Khaled', gender: 'Male'),
              const ClassStudent(id: 'st-3', nameAr: 'يوسف علي', nameEn: 'Youssef Ali', gender: 'Male'),
            ];
          },
        ),
      ));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('فصولي'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('فصولي'));
      await tester.pumpAndSettle();

      // 3 classes in 2 grades, 3 subjects
      expect(find.text('الصف الأول'), findsOneWidget);
      expect(find.text('الصف الثاني'), findsOneWidget);
      expect(find.text('فصل أ'), findsOneWidget);
      expect(find.text('فصل ج'), findsOneWidget);
      expect(find.text('علوم'), findsOneWidget);
      expect(find.text('الرئيسية'), findsOneWidget); // bottom bar stays

      await tester.tap(find.byKey(const ValueKey('class-s-a')));
      await tester.pumpAndSettle();
      expect(asked, 's-a');
      expect(find.text('الصف الأول - فصل أ'), findsOneWidget);
      expect(find.text('سارة أحمد'), findsOneWidget);
      expect(find.text('عمر خالد'), findsOneWidget);
      expect(find.text('بنين'), findsOneWidget);

      await tester.enterText(find.byKey(const ValueKey('students-search')), 'عمر');
      await tester.pump();
      expect(find.text('عمر خالد'), findsOneWidget);
      expect(find.text('سارة أحمد'), findsNothing);
      await tester.enterText(find.byKey(const ValueKey('students-search')), 'sara');
      await tester.pump();
      expect(find.text('سارة أحمد'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('student-st-1')));
      await tester.pumpAndSettle();
      expect(find.text('Sara Ahmed'), findsOneWidget);
      expect(find.text('أنثى'), findsOneWidget);
    });

    testWidgets('No classes shows a message', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(home: MyClassesScreen(loadClasses: () async => const [])));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد فصول مسندة إليك في هذا العام / الفصل الدراسي.'), findsOneWidget);
    });

    test('Student JSON: gender, birth date and age', () {
      final s = ClassStudent.fromJson({'id': 'x', 'nameAr': 'سارة', 'nameEn': 'Sara', 'gender': 'Female', 'dateOfBirth': '2018-03-10'});
      expect(s.isFemale, isTrue);
      expect(s.dateOfBirth, DateTime(2018, 3, 10));
      expect(s.ageOn(DateTime(2026, 3, 9)), 7);
      expect(s.ageOn(DateTime(2026, 3, 10)), 8);
      expect(ClassStudent.fromJson({'id': 'y', 'dateOfBirth': '0001-01-01'}).dateOfBirth, isNull);
    });
  });

  group('Parents', () {
    const parents = [
      TeacherParent(
        id: 'p1',
        nameAr: 'أحمد محمود',
        nameEn: 'Ahmed Mahmoud',
        phoneNumber: '0501234567',
        email: 'ahmed@test.local',
        children: [
          ParentChildInfo(studentId: 'st-1', nameAr: 'سارة', nameEn: 'Sara', sectionId: 's-a', sectionNameAr: 'فصل أ', gradeNameAr: 'الصف الأول'),
          ParentChildInfo(studentId: 'st-2', nameAr: 'ليلى', nameEn: 'Laila', sectionId: 's-c', sectionNameAr: 'فصل ج', gradeNameAr: 'الصف الثاني'),
        ],
      ),
      TeacherParent(
        id: 'p2',
        nameAr: 'منى حسن',
        nameEn: 'Mona Hassan',
        phoneNumber: '0559876543',
        children: [
          ParentChildInfo(studentId: 'st-3', nameAr: 'عمر', nameEn: 'Omar', sectionId: 's-a', sectionNameAr: 'فصل أ', gradeNameAr: 'الصف الأول'),
        ],
      ),
    ];

    testWidgets('Home tile opens the parents of my students: class filter, search and call',
        (WidgetTester tester) async {
      _phoneSize(tester);
      Uri? opened;
      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(loadProfile: () async => null, loadSchoolInfo: () async => null, loadParents: () async => parents),
      ));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('أولياء الأمور'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('أولياء الأمور'));
      await tester.pumpAndSettle();

      expect(find.text('أحمد محمود'), findsOneWidget);
      expect(find.text('منى حسن'), findsOneWidget);
      expect(find.text('سارة'), findsOneWidget);
      expect(find.text(' · الصف الثاني - فصل ج'), findsOneWidget);
      expect(find.text('الرئيسية'), findsOneWidget); // bottom bar stays

      // Only the parents with a child in class ج
      await tester.ensureVisible(find.byKey(const ValueKey('parents-class-s-c')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('parents-class-s-c')));
      await tester.pumpAndSettle();
      expect(find.text('أحمد محمود'), findsOneWidget);
      expect(find.text('منى حسن'), findsNothing);
      await tester.ensureVisible(find.byKey(const ValueKey('parents-class-all')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('parents-class-all')));
      await tester.pumpAndSettle();

      // Search by a child's name
      await tester.enterText(find.byKey(const ValueKey('parents-search')), 'عمر');
      await tester.pump();
      expect(find.text('منى حسن'), findsOneWidget);
      expect(find.text('أحمد محمود'), findsNothing);
      await tester.enterText(find.byKey(const ValueKey('parents-search')), '');
      await tester.pump();

      // The call button opens the dialer (tested on the screen itself)
      await tester.pumpWidget(MaterialApp(
        home: ParentsScreen(loadParents: () async => parents, openUri: (uri) async {
          opened = uri;
          return true;
        }),
      ));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('call-p2')));
      await tester.pump();
      expect(opened, Uri(scheme: 'tel', path: '0559876543'));
      expect(find.byKey(const ValueKey('email-p2')), findsNothing); // chat instead of email
    });

    testWidgets('The chat button opens the chat with that parent', (WidgetTester tester) async {
      _phoneSize(tester);
      String? startedWith;
      String? openedRoom;
      await tester.pumpWidget(MaterialApp(
        home: ParentsScreen(
          loadParents: () async => parents,
          chatRefreshEvery: null,
          chatApi: ChatApi(
            userId: 'teacher-1',
            start: (otherUserId) async {
              startedWith = otherUserId;
              return 'room-p1';
            },
            loadMessages: (roomId) async {
              openedRoom = roomId;
              return const [];
            },
          ),
        ),
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('chat-p1')));
      await tester.pumpAndSettle();
      expect(startedWith, 'p1');
      expect(openedRoom, 'room-p1');
      expect(find.byType(ChatScreen), findsOneWidget);
      expect(find.text('أحمد محمود'), findsOneWidget);
      expect(find.text('ولي أمر سارة، ليلى'), findsOneWidget);
    });

    testWidgets('No parents shows a message', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(home: ParentsScreen(loadParents: () async => const [])));
      await tester.pumpAndSettle();
      expect(find.text('لا يوجد أولياء أمور لطلاب فصولك.'), findsOneWidget);
    });

    test('Parent JSON: phone, email and children in my classes', () {
      final p = TeacherParent.fromJson({
        'id': 'p',
        'nameAr': 'أحمد',
        'nameEn': 'Ahmed',
        'phoneNumber': ' 0501 ',
        'email': '',
        'children': [
          {'studentId': 's', 'nameAr': 'سارة', 'nameEn': 'Sara', 'sectionId': 'x', 'sectionNameAr': 'فصل أ', 'gradeNameAr': 'الصف الأول'},
        ],
      });
      expect(p.phoneNumber, '0501');
      expect(p.email, isNull);
      expect(p.children.single.classLabel, 'الصف الأول - فصل أ');
      expect(p.matches('sara'), isTrue);
      expect(p.matches('0501'), isTrue);
      expect(p.matches('xyz'), isFalse);
    });
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

  group('Attendance', () {
    TeacherLesson lesson(String id, String subject, int start, String section) => TeacherLesson(
          id: id,
          start: start,
          end: start + 45,
          subjectNameAr: subject,
          subjectNameEn: subject,
          gradeNameAr: 'الصف الأول',
          sectionNameAr: section,
          subjectId: 'sub-$id',
          gradeId: 'g1',
          sectionId: 'sec-$section',
        );

    const students = [
      ClassStudent(id: 'st1', nameAr: 'أحمد', nameEn: 'Ahmed'),
      ClassStudent(id: 'st2', nameAr: 'بسمة', nameEn: 'Basma'),
      ClassStudent(id: 'st3', nameAr: 'تامر', nameEn: 'Tamer'),
    ];

    testWidgets('Home tile opens the lessons of today; taking a lesson saves every student', (WidgetTester tester) async {
      _phoneSize(tester);
      final today = DateTime.now().weekday % 7;
      final math = lesson('l1', 'رياضيات', 480, 'فصل أ');
      final science = lesson('l2', 'علوم', 570, 'فصل ب');
      final week = TeacherTimetable(today: today, now: 600, academicYearId: 'y-2026', days: [
        TeacherTimetableDay(dayOfWeek: today, lessons: [math, science]),
      ]);
      // Science was taken already: Basma absent
      final saved = <String, List<RecordedAttendance>>{
        'l2': const [
          RecordedAttendance(studentId: 'st1', status: LessonAttendanceStatus.present),
          RecordedAttendance(studentId: 'st2', status: LessonAttendanceStatus.absent, absenceReason: 'مريضة'),
        ],
      };
      List<StudentAttendanceInput>? sent;
      String? sentYear;
      final api = AttendanceApi(
        loadTimetable: () async => week,
        loadStudents: (sectionId) async => students,
        loadRecorded: (l, date) async => saved[l.id] ?? const [],
        save: (l, date, yearId, list) async {
          sent = list;
          sentYear = yearId;
          saved[l.id] = [for (final s in list) RecordedAttendance(studentId: s.studentId, status: s.status)];
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
          home: HomeScreen(
            loadProfile: () async => null,
            loadSchoolInfo: () async => null,
            loadAcademicYears: () async => _years,
            attendanceApi: api,
          ),
        ),
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('الحضور والغياب'));
      await tester.pumpAndSettle();

      // Today's lessons with their state
      expect(find.text('رياضيات'), findsOneWidget);
      expect(find.text('لم يُؤخذ الحضور'), findsOneWidget);
      expect(find.text('تم أخذ الحضور'), findsOneWidget);
      expect(find.text('1 غائب'), findsOneWidget);
      expect(find.text('تم أخذ الحضور في 1 من 2 حصص'), findsOneWidget);

      // Math: everyone present by default, Tamer absent with a reason, Basma late
      await tester.tap(find.byKey(const ValueKey('lesson-l1')));
      await tester.pumpAndSettle();
      expect(find.text('أخذ الحضور'), findsOneWidget);
      expect(find.text('أحمد'), findsOneWidget);
      await tester.tap(find.byKey(const ValueKey('status-st3-absent')));
      await tester.pump();
      await tester.enterText(find.byKey(const ValueKey('reason-st3')), 'سفر');
      await tester.tap(find.byKey(const ValueKey('status-st2-late')));
      await tester.pump();
      expect(find.byKey(const ValueKey('reason-st2')), findsNothing);

      await tester.tap(find.byKey(const ValueKey('save-attendance')));
      await tester.pumpAndSettle();

      expect(sentYear, 'y-2026');
      expect(sent!.map((s) => '${s.studentId}:${s.status.name}').toList(), ['st1:present', 'st2:late', 'st3:absent']);
      expect(sent!.last.toJson()['absenceReason'], 'سفر');
      // Back on the lessons: both taken now
      expect(find.text('لم يُؤخذ الحضور'), findsNothing);
      expect(find.text('تم أخذ الحضور في 2 من 2 حصص'), findsOneWidget);
    });

    testWidgets('A lesson already taken opens with the saved statuses and can be updated', (WidgetTester tester) async {
      _phoneSize(tester);
      final science = lesson('l2', 'علوم', 570, 'فصل ب');
      await tester.pumpWidget(MaterialApp(
        home: TakeAttendanceScreen(
          api: AttendanceApi(
            loadStudents: (_) async => students,
            loadRecorded: (l, d) async => const [
              RecordedAttendance(studentId: 'st2', status: LessonAttendanceStatus.excused, absenceReason: 'موعد طبيب'),
            ],
          ),
          lesson: science,
          date: DateTime(2026, 11, 22),
          academicYearId: 'y-2026',
        ),
      ));
      await tester.pumpAndSettle();

      expect(find.text('تم أخذ الحضور من قبل — يمكنك تعديله'), findsOneWidget);
      expect(find.text('تحديث الحضور'), findsOneWidget);
      expect(find.text('موعد طبيب'), findsOneWidget);

      // All present clears the excuse
      await tester.tap(find.byKey(const ValueKey('all-present')));
      await tester.pump();
      expect(find.byKey(const ValueKey('reason-st2')), findsNothing);
    });

    testWidgets('A day without lessons says so and the next day cannot be after today', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(
        home: AttendanceLessonsScreen(
          api: AttendanceApi(loadTimetable: () async => const TeacherTimetable(today: 0, now: 0, days: [])),
          today: DateTime(2026, 11, 22),
        ),
      ));
      await tester.pumpAndSettle();

      expect(find.text('لا توجد لديك حصص في هذا اليوم.'), findsOneWidget);
      final next = tester.widget<IconButton>(find.byKey(const ValueKey('next-day')));
      expect(next.onPressed, isNull);
    });

    test('Lesson ids come from the timetable JSON; a present student sends no reason', () {
      final l = TeacherLesson.fromJson({
        'id': 'slot',
        'startTime': '08:00:00',
        'endTime': '08:45:00',
        'subjectId': 'sub',
        'gradeId': 'g',
        'sectionId': 'sec',
      });
      expect([l.subjectId, l.gradeId, l.sectionId], ['sub', 'g', 'sec']);
      expect(TeacherTimetable.fromJson({'academicYearId': 'y', 'days': []}).academicYearId, 'y');
      expect(minutesToTime(l.start), '08:00:00');
      expect(attendanceDate(DateTime(2026, 1, 5)), '2026-01-05');
      expect(const StudentAttendanceInput(studentId: 's', status: LessonAttendanceStatus.present, absenceReason: 'x').toJson(),
          {'studentId': 's', 'status': 0, 'absenceReason': null});
      expect(LessonAttendanceStatus.fromCode(2), LessonAttendanceStatus.late);
    });

    group('Class attendance', () {
      const math = TeacherSubject(
        subjectId: 'sub-math',
        subjectNameAr: 'رياضيات',
        subjectNameEn: 'Math',
        gradeId: 'g1',
        gradeNameAr: 'الصف الأول',
        gradeNameEn: 'Grade 1',
        sections: [TeacherSection(id: 's-a', nameAr: 'فصل أ', nameEn: 'Class A')],
      );
      final myClass = MyClass(
        section: const TeacherSection(id: 's-a', nameAr: 'فصل أ', nameEn: 'Class A'),
        gradeId: 'g1',
        gradeNameAr: 'الصف الأول',
        gradeNameEn: 'Grade 1',
        subjects: const [math],
      );
      const classStudents = [
        ClassStudent(id: 'st1', nameAr: 'أحمد', nameEn: 'Ahmed'),
        ClassStudent(id: 'st2', nameAr: 'بسمة', nameEn: 'Basma'),
        ClassStudent(id: 'st3', nameAr: 'تامر', nameEn: 'Tamer'),
      ];
      final days = [
        const StudentDay(studentId: 'st1', status: DayAttendanceStatus.present, attendedLessons: 2, lessons: [
          DayLesson(timetableSlotId: 'l1', subjectId: 'sub-math', teacherId: 't', start: 480, status: LessonAttendanceStatus.present),
          DayLesson(timetableSlotId: 'l2', subjectId: 'other', teacherId: 't2', start: 570, status: LessonAttendanceStatus.late),
        ]),
        const StudentDay(studentId: 'st2', status: DayAttendanceStatus.absent, absentLessons: 1, lessons: [
          DayLesson(timetableSlotId: 'l1', subjectId: 'sub-math', teacherId: 't', start: 480, status: LessonAttendanceStatus.absent, absenceReason: 'مريضة'),
        ]),
      ];

      testWidgets('The class page opens its attendance: summary, filters and each student\'s lessons', (WidgetTester tester) async {
        _phoneSize(tester);
        final asked = <String>[];
        await tester.pumpWidget(MaterialApp(
          home: ClassStudentsScreen(
            myClass: myClass,
            loadStudents: (_) async => classStudents,
            now: DateTime(2026, 11, 22),
            attendanceApi: AttendanceApi(
              loadStudents: (_) async => classStudents,
              loadClassDays: (sectionId, date) async {
                asked.add('$sectionId ${attendanceDate(date)}');
                return attendanceDate(date) == '2026-11-22' ? days : const [];
              },
            ),
          ),
        ));
        await tester.pumpAndSettle();

        await tester.tap(find.byKey(const ValueKey('class-attendance')));
        await tester.pumpAndSettle();

        expect(find.text('حضور الفصل'), findsOneWidget);
        expect(asked, ['s-a 2026-11-22']);
        // 3 students, 2 recorded, 1 absent, 50% attendance
        expect(find.text('50%'), findsOneWidget);
        expect(find.text('لم يُسجَّل (1)'), findsOneWidget);
        expect(find.text('غائب (1)'), findsOneWidget);

        // Only the absent
        await tester.tap(find.byKey(const ValueKey('class-filter-absent')));
        await tester.pumpAndSettle();
        expect(find.text('بسمة'), findsOneWidget);
        expect(find.text('أحمد'), findsNothing);

        // Back to all; Ahmed's lessons: mine by subject name, another teacher's as "lesson"
        await tester.tap(find.byKey(const ValueKey('class-filter-all')));
        await tester.pumpAndSettle();
        await tester.tap(find.byKey(const ValueKey('class-student-st1')));
        await tester.pumpAndSettle();
        expect(find.text('رياضيات'), findsOneWidget);
        expect(find.text('حصة'), findsOneWidget);
        expect(find.text('متأخر'), findsOneWidget);

        // The previous day has nothing recorded
        await tester.tap(find.byKey(const ValueKey('class-previous-day')));
        await tester.pumpAndSettle();
        expect(asked.last, 's-a 2026-11-21');
        expect(find.text('لم يُسجَّل حضور هذا الفصل في هذا اليوم بعد.'), findsOneWidget);
      });

      test('Student day JSON: status, times, lessons by time', () {
        final d = StudentDay.fromJson({
          'studentId': 's',
          'status': 2,
          'checkInTime': '07:45:00',
          'checkOutTime': null,
          'notes': '',
          'attendedPeriodsCount': 1,
          'absentPeriodsCount': 1,
          'periods': [
            {'timetableSlotId': 'b', 'subjectId': 'x', 'teacherId': 't', 'startTime': '09:30:00', 'status': 1, 'absenceReason': 'خرج'},
            {'timetableSlotId': 'a', 'subjectId': 'y', 'teacherId': 't', 'startTime': '08:00:00', 'status': 0},
          ],
        });
        expect(d.status, DayAttendanceStatus.partiallyPresent);
        expect(d.checkIn, 465);
        expect(d.checkOut, isNull);
        expect(d.notes, isNull);
        expect(d.lessons.map((l) => l.timetableSlotId).toList(), ['a', 'b']);
        expect(d.lessons.last.status, LessonAttendanceStatus.absent);
      });
    });
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

    testWidgets('The bottom bar opens my notifications and keeps the bar', (WidgetTester tester) async {
      _phoneSize(tester);
      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(
          loadProfile: () async => null,
          loadSchoolInfo: () async => null,
          loadNotifications: () async => notices,
        ),
      ));
      await tester.pumpAndSettle();
      await tester.tap(find.text('الإشعارات'));
      await tester.pumpAndSettle();
      expect(find.byType(NotificationsScreen), findsOneWidget);
      expect(find.text('واجب الرياضيات'), findsOneWidget);
      expect(find.text('لك'), findsNWidgets(2));
      expect(find.text('الرئيسية'), findsOneWidget);

      await tester.tap(find.text('الرئيسية'));
      await tester.pumpAndSettle();
      expect(find.byType(NotificationsScreen), findsNothing);
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

    testWidgets('Media: the mic shows until something is typed; a photo message shows as a picture; the list says Photo',
        (WidgetTester tester) async {
      _phoneSize(tester);
      final api = ChatApi(
        userId: me,
        loadChats: () async => [
          ChatSummary(roomId: 'r1', otherUserId: 'u-1', nameAr: 'منى حسن', nameEn: 'Mona',
              lastMessage: '', lastAttachmentKind: 'image', lastMessageSenderId: 'u-1', lastMessageOn: now),
        ],
        loadMessages: (roomId) async => [
          ChatMessage(id: 'p1', senderId: 'u-1', content: '', sentOn: now, attachments: const [
            ChatAttachment(fileName: 'photo.jpg', url: '/api/chat/files/r1/abc.jpg', contentType: 'image/jpeg', sizeInBytes: 2048),
          ]),
        ],
        seen: (roomId, ids) async {},
      );
      await tester.pumpWidget(MaterialApp(home: ChatListScreen(api: api, refreshEvery: null, now: now)));
      await tester.pumpAndSettle();
      // A picture without words: the list shows what it is
      expect(find.text('📷 صورة'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey('chat-r1')));
      await tester.pumpAndSettle();
      expect(find.byType(ChatMediaView), findsOneWidget);
      expect(find.byKey(const ValueKey('chat-attach')), findsOneWidget);

      // Nothing typed: the mic records a voice note; typing brings back the send button
      expect(find.byKey(const ValueKey('chat-mic')), findsOneWidget);
      expect(find.byKey(const ValueKey('chat-send')), findsNothing);
      await tester.enterText(find.byKey(const ValueKey('chat-input')), 'مرحباً');
      await tester.pump();
      expect(find.byKey(const ValueKey('chat-send')), findsOneWidget);
      expect(find.byKey(const ValueKey('chat-mic')), findsNothing);
    });

    test('Chat JSON: attachments, their kind and the list preview of a voice note', () {
      final m = ChatMessage.fromJson({
        'id': 'v',
        'senderId': 's',
        'content': '',
        'sentOn': '2026-11-20T08:00:00Z',
        'attachments': [
          {'fileName': 'voice.m4a', 'url': '/api/chat/files/r/1.m4a', 'contentType': 'audio/mp4', 'sizeInBytes': 900},
        ],
      });
      expect(m.attachments.single.kind, ChatMediaKind.audio);
      expect(m.attachments.single.sizeInBytes, 900);
      expect(chatMediaKind('video/mp4'), ChatMediaKind.video);
      final c = ChatSummary.fromJson({'roomId': 'r', 'otherUserId': 'u', 'lastMessage': '', 'lastAttachmentKind': 'audio'});
      expect(c.lastMessagePreview, '🎤 رسالة صوتية');
      final text = ChatSummary.fromJson({'roomId': 'r', 'otherUserId': 'u', 'lastMessage': 'hi', 'lastAttachmentKind': null});
      expect(text.lastMessagePreview, 'hi');
      // Files that are too large are refused before the upload
      final big = ChatOutgoingMedia(bytes: Uint8List(chatMaxImageBytes + 1), fileName: 'p.jpg', kind: ChatMediaKind.image);
      expect(big.tooLarge, isTrue);
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

  group('Student activity', () {
    testWidgets("A student's profile, the week's lessons with their reviews, the review details, other weeks, exams",
        (WidgetTester tester) async {
      _phoneSize(tester);
      final now = DateTime(2026, 11, 24, 10, 0); // Tuesday; the week started on Saturday the 21st
      final asked = <DateTime>[];
      final api = StudentActivityApi(
        loadInfo: (_) async => const StudentActivityInfo(
          student: ActivityStudent(id: 'st-1', nameAr: 'سارة أحمد', nameEn: 'Sara Ahmed', gender: 'Female', gradeName: 'الصف الأول', sectionName: 'فصل أ'),
          days: [
            ActivityTimetableDay(dayOfWeek: 0, slots: [ActivitySlot(id: 's1', start: 450, end: 495, subject: 'قرآن', teacher: 'أ. علي')]),
            ActivityTimetableDay(dayOfWeek: 2, slots: [
              ActivitySlot(id: 't1', start: 480, end: 525, subject: 'رياضيات', teacher: 'أ. منى'),
              ActivitySlot(id: 't2', start: 570, end: 615, subject: 'علوم', teacher: 'أ. سامي'),
            ]),
          ],
          teachers: [ActivityTeacher(id: 'tm', name: 'أ. منى', subjects: {'m': 'رياضيات'})],
        ),
        loadAttendance: (id, from, to) async {
          asked.add(from);
          if (from != DateTime(2026, 11, 21)) return const [];
          return [
            ActivityDay(date: DateTime(2026, 11, 22), status: ActivityDayStatus.present, lessons: const [
              ActivityLesson(timetableSlotId: 's1', subjectId: 'q', start: 450, status: ActivityLessonStatus.present),
            ]),
            ActivityDay(date: DateTime(2026, 11, 24), status: ActivityDayStatus.partiallyPresent, absentLessons: 1, lessons: const [
              ActivityLesson(timetableSlotId: 't1', subjectId: 'm', start: 480, status: ActivityLessonStatus.present),
              ActivityLesson(timetableSlotId: 't2', subjectId: 'sc', start: 570, status: ActivityLessonStatus.absent, absenceReason: 'موعد طبيب'),
            ]),
          ];
        },
        loadReviews: (id, from, to) async => from == DateTime(2026, 11, 21)
            ? [
                ActivityReview(id: 'e1', teacherId: 'tm', subjectId: 'm', date: DateTime(2026, 11, 24), focus: 5, interaction: 4,
                    behavior: 4, health: ActivityHealth.good, comment: 'مشاركة ممتازة اليوم'),
              ]
            : const [],
        loadExams: (_) async => [
          ActivityExam(id: 'x1', kind: ActivityExamKind.monthly, subject: 'رياضيات', date: DateTime(2026, 11, 10), start: 600, end: 660,
              fullMark: 20, score: 18, isPassed: true, percentage: 90),
          ActivityExam(id: 'x2', kind: ActivityExamKind.quiz, subject: 'علوم', date: DateTime(2026, 11, 30), start: 600, end: 630, fullMark: 10),
        ],
      );
      await tester.pumpWidget(MaterialApp(home: StudentActivityScreen(studentId: 'st-1', studentName: 'سارة أحمد', api: api, now: now)));
      await tester.pumpAndSettle();

      // Profile
      expect(find.byKey(const ValueKey('activity-profile')), findsOneWidget);
      expect(find.text('فصل أ'), findsOneWidget);

      // The week: Sunday and Tuesday, the review on Tuesday's math lesson
      expect(find.text('21 نوفمبر – 27 نوفمبر'), findsOneWidget);
      expect(find.text('2/2'), findsOneWidget);
      expect(find.text('قرآن'), findsOneWidget);
      expect(find.text('4.3'), findsNWidgets(2));
      expect(find.text('حاضر جزئياً'), findsOneWidget);

      // The review's details
      await tester.tap(find.text('4.3').last);
      await tester.pumpAndSettle();
      expect(find.text('التركيز'), findsOneWidget);
      expect(find.text('مشاركة ممتازة اليوم'), findsOneWidget);
      Navigator.of(tester.element(find.text('التركيز'))).pop();
      await tester.pumpAndSettle();

      // Exams
      await tester.scrollUntilVisible(find.byKey(const ValueKey('activity-exam-average')), 300);
      expect(find.text('90%'), findsOneWidget);
      expect(find.text('18/20'), findsOneWidget);

      // Previous week
      await tester.scrollUntilVisible(find.byKey(const ValueKey('activity-prev-week')), -300);
      await tester.tap(find.byKey(const ValueKey('activity-prev-week')));
      await tester.pumpAndSettle();
      expect(asked.last, DateTime(2026, 11, 14));
      expect(find.text('4.3'), findsNothing);
    });
  });

}
