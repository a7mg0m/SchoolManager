import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_teacher/core/academic_year/academic_year_controller.dart';
import 'package:smart_school_teacher/features/classes/classes_service.dart';
import 'package:smart_school_teacher/features/evaluation/evaluation_screens.dart';
import 'package:smart_school_teacher/features/evaluation/evaluation_service.dart';
import 'package:smart_school_teacher/features/homework/homework_service.dart';
import 'package:smart_school_teacher/features/timetable/teacher_timetable_service.dart';

void _phoneSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.6;
  addTearDown(tester.view.reset);
}

Widget _app(Widget screen) => MaterialApp(home: Directionality(textDirection: TextDirection.rtl, child: screen));

final _today = DateTime(2026, 11, 22);
final _todayDow = _today.weekday % 7;
final _otherDow = (_todayDow + 5) % 7;

TeacherLesson _lesson(String id, String subjectId, String subject, int start) => TeacherLesson(
      id: id,
      start: start,
      end: start + 45,
      subjectNameAr: subject,
      subjectNameEn: subject,
      gradeNameAr: 'الصف الأول',
      sectionNameAr: 'فصل أ',
      subjectId: subjectId,
      gradeId: 'g1',
      sectionId: 's-a',
    );

final _math = _lesson('l-math', 'math', 'رياضيات', 480);
final _science = _lesson('l-sci', 'sci', 'علوم', 600);

final _timetable = TeacherTimetable(today: _todayDow, now: 0, academicYearId: 'y', days: [
  TeacherTimetableDay(dayOfWeek: _todayDow, lessons: [_math]),
  TeacherTimetableDay(dayOfWeek: _otherDow, lessons: [_science]),
]);

const _class = MyClass(
  section: TeacherSection(id: 's-a', nameAr: 'فصل أ', nameEn: 'Class A'),
  gradeId: 'g1',
  gradeNameAr: 'الصف الأول',
  gradeNameEn: 'Grade 1',
  subjects: [
    TeacherSubject(subjectId: 'math', subjectNameAr: 'رياضيات', subjectNameEn: 'Math', gradeId: 'g1', gradeNameAr: 'الصف الأول', gradeNameEn: 'Grade 1'),
    TeacherSubject(subjectId: 'sci', subjectNameAr: 'علوم', subjectNameEn: 'Science', gradeId: 'g1', gradeNameAr: 'الصف الأول', gradeNameEn: 'Grade 1'),
  ],
);

const _students = [
  ClassStudent(id: 'st1', nameAr: 'أحمد', nameEn: 'Ahmed'),
  ClassStudent(id: 'st2', nameAr: 'بسمة', nameEn: 'Basma'),
  ClassStudent(id: 'st3', nameAr: 'تامر', nameEn: 'Tamer'),
];

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await AcademicYearController.instance.reset();
  });

  testWidgets('An "all subjects" request stays open until every subject the teacher teaches is evaluated',
      (WidgetTester tester) async {
    _phoneSize(tester);
    var pending = [
      PendingEvaluationRequest(id: 'r1', studentId: 'st2', requestedAt: _today.subtract(const Duration(days: 3)), note: 'كيف مستواها؟'),
    ];
    final linked = <StudentEvaluation>[];
    final saved = <String>[];
    EvaluationInput? input;

    await tester.pumpWidget(_app(EvaluationsScreen(
      today: _today,
      api: EvaluationApi(
        teacherId: 'me',
        loadTimetable: () async => _timetable,
        loadClasses: () async => const [_class],
        loadStudents: (_) async => _students,
        loadPending: () async => pending,
        loadLessonEvaluations: (l, d) async => const [],
        loadStudentEvaluations: (id, from, to) async => linked,
        save: (lesson, date, i) async {
          saved.add('${lesson.id} ${date.toIso8601String().substring(0, 10)} ${i.completeRequest}');
          input = i;
          linked.add(StudentEvaluation(
              id: 'e-${lesson.subjectId}', studentId: i.studentId, teacherId: 'me', subjectId: lesson.subjectId, date: date,
              evaluationRequestId: i.evaluationRequestId, focus: i.focus, interaction: i.interaction, behavior: i.behavior, health: i.health));
          // The API closes the request only with the last subject
          if (i.completeRequest) pending = [];
        },
      ),
    )));
    await tester.pumpAndSettle();

    // The request: student, class, any subject, note, how long ago
    expect(find.text('بسمة'), findsOneWidget);
    expect(find.text('الصف الأول - فصل أ · كل المواد'), findsOneWidget);
    expect(find.text('كيف مستواها؟'), findsOneWidget);
    expect(find.text('منذ 3 أيام'), findsOneWidget);
    expect(find.text('تم 0 من 2 مواد · المتبقي:'), findsOneWidget);
    expect(find.byKey(const ValueKey('remaining-r1-math')), findsOneWidget);
    expect(find.byKey(const ValueKey('remaining-r1-sci')), findsOneWidget);

    // Two subjects with this class: one evaluation each
    await tester.tap(find.byKey(const ValueKey('answer-r1')));
    await tester.pumpAndSettle();
    expect(find.text('تم تقييم 0 من 2 مواد'), findsOneWidget);

    // Science first: the request stays open
    await tester.tap(find.byKey(const ValueKey('request-subject-sci')));
    await tester.pumpAndSettle();
    expect(find.text('ولي الأمر: كيف مستواها؟'), findsOneWidget);
    expect(find.text('حفظ هذه المادة'), findsOneWidget);
    // Saving without ratings is refused
    await tester.tap(find.byKey(const ValueKey('save-evaluation')));
    await tester.pump();
    expect(find.text('قيّم التركيز والتفاعل والسلوك أولاً.'), findsOneWidget);
    expect(saved, isEmpty);

    await tester.tap(find.byKey(const ValueKey('rate-focus-4')));
    await tester.tap(find.byKey(const ValueKey('rate-interaction-5')));
    await tester.tap(find.byKey(const ValueKey('rate-behavior-3')));
    await tester.tap(find.byKey(const ValueKey('health-tired')));
    await tester.pump();
    expect(find.text('جيد جداً'), findsOneWidget);
    await tester.enterText(find.byKey(const ValueKey('evaluation-comment')), 'تشارك بحماس');
    await tester.tap(find.byKey(const ValueKey('save-evaluation')));
    await tester.pumpAndSettle();

    final scienceDay = lastDateOn(_otherDow, _today);
    final scienceDate = scienceDay.toIso8601String().substring(0, 10);
    expect(saved, ['l-sci $scienceDate false']);
    expect(input!.toJson(_science, scienceDay), {
      'studentId': 'st2',
      'subjectId': 'sci',
      'timetableSlotId': 'l-sci',
      'date': scienceDate,
      'focus': 4,
      'interaction': 5,
      'behavior': 3,
      'healthStatus': 1,
      'comment': 'تشارك بحماس',
      'evaluationRequestId': 'r1',
      'completeRequest': false,
    });
    // Still on the subjects: 1 of 2
    expect(find.text('تم تقييم 1 من 2 مواد'), findsOneWidget);

    // Math, the last subject: closes the request
    await tester.tap(find.byKey(const ValueKey('request-subject-math')));
    await tester.pumpAndSettle();
    expect(find.text('حفظ وإرسال لولي الأمر'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('rate-focus-5')));
    await tester.tap(find.byKey(const ValueKey('rate-interaction-5')));
    await tester.tap(find.byKey(const ValueKey('rate-behavior-5')));
    await tester.tap(find.byKey(const ValueKey('save-evaluation')));
    await tester.pumpAndSettle();

    expect(saved.last, 'l-math ${_today.toIso8601String().substring(0, 10)} true');
    expect(find.text('تم تقييم كل المواد وإرسالها لولي الأمر.'), findsOneWidget);
    expect(find.text('لا توجد طلبات تقييم بانتظارك.'), findsOneWidget);
  });

  testWidgets('A request about a subject goes straight to its lesson', (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(EvaluationsScreen(
      today: _today,
      api: EvaluationApi(
        teacherId: 'me',
        loadTimetable: () async => _timetable,
        loadClasses: () async => const [_class],
        loadStudents: (_) async => _students,
        loadPending: () async => [PendingEvaluationRequest(id: 'r1', studentId: 'st1', subjectId: 'math', requestedAt: _today)],
        loadLessonEvaluations: (l, d) async => const [],
        loadStudentEvaluations: (id, from, to) async => const [],
      ),
    )));
    await tester.pumpAndSettle();
    expect(find.text('الصف الأول - فصل أ · رياضيات'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('answer-r1')));
    await tester.pumpAndSettle();
    // No lesson choice: the math lesson today
    expect(find.text('تقييم الطالب'), findsOneWidget);
    expect(find.textContaining('رياضيات'), findsWidgets);
  });

  testWidgets('Evaluate a lesson: the class with its evaluations; another teacher\'s cannot be changed', (WidgetTester tester) async {
    _phoneSize(tester);
    final saved = <EvaluationInput>[];
    var evaluations = const [
      StudentEvaluation(id: 'e1', studentId: 'st1', teacherId: 'me', focus: 5, interaction: 5, behavior: 5, health: StudentHealth.good),
      StudentEvaluation(id: 'e3', studentId: 'st3', teacherId: 'other', focus: 2, interaction: 2, behavior: 2, health: StudentHealth.sick),
    ];

    await tester.pumpWidget(_app(EvaluationsScreen(
      today: _today,
      api: EvaluationApi(
        teacherId: 'me',
        loadTimetable: () async => _timetable,
        loadClasses: () async => const [_class],
        loadStudents: (_) async => _students,
        loadPending: () async => [PendingEvaluationRequest(id: 'r9', studentId: 'st2', requestedAt: _today)],
        loadLessonEvaluations: (l, d) async => evaluations,
        loadStudentEvaluations: (id, from, to) async => const [],
        save: (lesson, date, i) async {
          saved.add(i);
          evaluations = [...evaluations, StudentEvaluation(id: 'e2', studentId: i.studentId, teacherId: 'me', focus: i.focus, interaction: i.interaction, behavior: i.behavior, health: i.health)];
        },
      ),
    )));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('tab-lessons')));
    await tester.pumpAndSettle();
    // Today's math lesson, one parent request in the class
    expect(find.text('رياضيات'), findsOneWidget);
    expect(find.text('1 طلب'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('eval-lesson-l-math')));
    await tester.pumpAndSettle();
    expect(find.text('تم تقييم 2 من 3 طالب'), findsOneWidget);
    expect(find.text('طلب ولي الأمر · كل المواد'), findsOneWidget);
    expect(find.text('· معلم آخر'), findsOneWidget);

    // Another teacher's evaluation
    await tester.tap(find.byKey(const ValueKey('eval-student-st3')));
    await tester.pump();
    expect(find.text('قيّم معلم آخر هذا الطالب في هذه الحصة.'), findsOneWidget);

    // My saved one opens prefilled
    await tester.tap(find.byKey(const ValueKey('eval-student-st1')));
    await tester.pumpAndSettle();
    expect(find.text('تعديل التقييم'), findsNWidgets(1));
    expect(find.text('ممتاز'), findsNWidgets(3));
    await tester.pageBack();
    await tester.pumpAndSettle();

    // Basma (requested): evaluate
    await tester.tap(find.byKey(const ValueKey('eval-student-st2')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('rate-focus-3')));
    await tester.tap(find.byKey(const ValueKey('rate-interaction-3')));
    await tester.tap(find.byKey(const ValueKey('rate-behavior-4')));
    await tester.tap(find.byKey(const ValueKey('save-evaluation')));
    await tester.pumpAndSettle();

    expect(saved.single.studentId, 'st2');
    expect(saved.single.evaluationRequestId, 'r9');
    expect(saved.single.completeRequest, isFalse);
    expect(find.text('تم تقييم 3 من 3 طالب'), findsOneWidget);
    // Still waiting for the teacher's other subjects
    expect(find.text('طلب ولي الأمر · كل المواد'), findsOneWidget);
  });

  testWidgets('The last subject of an "all subjects" request, saved from the lesson tab, closes it', (WidgetTester tester) async {
    _phoneSize(tester);
    // Science is already evaluated for the request; math is left
    final science = StudentEvaluation(
        id: 'e-sci', studentId: 'st2', teacherId: 'me', subjectId: 'sci', timetableSlotId: 'l-sci', date: lastDateOn(_otherDow, _today),
        evaluationRequestId: 'r1', focus: 4, interaction: 4, behavior: 4, health: StudentHealth.good);
    var pending = [PendingEvaluationRequest(id: 'r1', studentId: 'st2', requestedAt: _today.subtract(const Duration(days: 2)))];
    final saved = <EvaluationInput>[];

    await tester.pumpWidget(_app(EvaluationsScreen(
      today: _today,
      api: EvaluationApi(
        teacherId: 'me',
        loadTimetable: () async => _timetable,
        loadClasses: () async => const [_class],
        loadStudents: (_) async => _students,
        loadPending: () async => pending,
        loadLessonEvaluations: (l, d) async => const [],
        loadStudentEvaluations: (id, from, to) async => [science],
        save: (lesson, date, i) async {
          saved.add(i);
          if (i.completeRequest) pending = [];
        },
      ),
    )));
    await tester.pumpAndSettle();

    // The card: 1 of 2 done, math left
    expect(find.text('تم 1 من 2 مواد · المتبقي:'), findsOneWidget);
    expect(find.byKey(const ValueKey('remaining-r1-math')), findsOneWidget);
    expect(find.byKey(const ValueKey('remaining-r1-sci')), findsNothing);

    // Math from today's lesson: the last subject
    await tester.tap(find.byKey(const ValueKey('tab-lessons')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('eval-lesson-l-math')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('eval-student-st2')));
    await tester.pumpAndSettle();
    expect(find.text('حفظ وإرسال لولي الأمر'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('rate-focus-5')));
    await tester.tap(find.byKey(const ValueKey('rate-interaction-5')));
    await tester.tap(find.byKey(const ValueKey('rate-behavior-5')));
    await tester.tap(find.byKey(const ValueKey('save-evaluation')));
    await tester.pumpAndSettle();

    expect(saved.single.evaluationRequestId, 'r1');
    expect(saved.single.completeRequest, isTrue);
    expect(find.text('طلب ولي الأمر · كل المواد'), findsNothing);

    // Back to the requests: none left
    await tester.pageBack();
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('tab-requests')));
    await tester.pumpAndSettle();
    expect(find.text('لا توجد طلبات تقييم بانتظارك.'), findsOneWidget);
  });

  testWidgets('A request whose subjects are all evaluated already is closed when the list loads', (WidgetTester tester) async {
    _phoneSize(tester);
    final evaluations = [
      StudentEvaluation(
          id: 'e-math', studentId: 'st2', teacherId: 'me', subjectId: 'math', timetableSlotId: 'l-math', date: _today,
          evaluationRequestId: 'r1', focus: 5, interaction: 4, behavior: 5, health: StudentHealth.good, comment: 'ممتازة'),
      StudentEvaluation(
          id: 'e-sci', studentId: 'st2', teacherId: 'me', subjectId: 'sci', timetableSlotId: 'l-sci', date: lastDateOn(_otherDow, _today),
          evaluationRequestId: 'r1', focus: 4, interaction: 4, behavior: 4, health: StudentHealth.good),
    ];
    var pending = [PendingEvaluationRequest(id: 'r1', studentId: 'st2', requestedAt: _today.subtract(const Duration(days: 2)))];
    final saved = <String>[];

    await tester.pumpWidget(_app(EvaluationsScreen(
      today: _today,
      api: EvaluationApi(
        teacherId: 'me',
        loadTimetable: () async => _timetable,
        loadClasses: () async => const [_class],
        loadStudents: (_) async => _students,
        loadPending: () async => pending,
        loadLessonEvaluations: (l, d) async => const [],
        loadStudentEvaluations: (id, from, to) async => evaluations,
        save: (lesson, date, i) async {
          saved.add('${lesson.id} ${i.evaluationRequestId} ${i.completeRequest} ${i.focus} ${i.comment}');
          pending = [];
        },
      ),
    )));
    await tester.pumpAndSettle();

    // One evaluation saved again (unchanged) as the last subject: the request closes
    expect(saved, ['l-math r1 true 5 ممتازة']);
    expect(find.text('لا توجد طلبات تقييم بانتظارك.'), findsOneWidget);
  });

  test('Last date of a lesson day and the JSON', () {
    final sunday = DateTime(2026, 11, 22); // a Sunday
    expect(sunday.weekday % 7, 0);
    expect(lastDateOn(0, sunday), sunday);
    expect(lastDateOn(4, sunday), DateTime(2026, 11, 19)); // Thursday before
    expect(lastDateOn(1, sunday), DateTime(2026, 11, 16)); // Monday before

    final r = PendingEvaluationRequest.fromJson({'id': 'r', 'studentId': 's', 'subjectId': null, 'requestedAt': '2026-11-20T08:00:00', 'note': ''});
    expect(r.subjectId, isNull);
    expect(r.note, isNull);
    expect(r.requestedAt, DateTime.utc(2026, 11, 20, 8).toLocal());

    final e = StudentEvaluation.fromJson({
      'id': 'e', 'studentId': 's', 'teacherId': 't', 'focus': 4, 'interaction': 5, 'behavior': 0, 'healthStatus': 2, 'comment': ' ',
    });
    expect(e.behavior, 1);
    expect(e.health, StudentHealth.sick);
    expect(e.comment, isNull);
  });
}
