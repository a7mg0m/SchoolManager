import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_parent/core/academic_year/academic_year_controller.dart';

import 'package:smart_school_parent/features/children/children_service.dart';
import 'package:smart_school_parent/features/evaluation/evaluation_screens.dart';
import 'package:smart_school_parent/features/evaluation/evaluation_service.dart';

const _child = Child(
  id: 'c1',
  fullNameAr: 'سارة أحمد',
  fullNameEn: 'Sara Ahmed',
  gender: 'Female',
  stageName: 'ابتدائي',
  gradeName: 'الصف الثالث',
  sectionName: 'أ',
);

const _mona = ChildTeacher(id: 't1', nameAr: 'أ. منى', nameEn: 'Ms Mona', subjects: [
  TeacherSubjectRef(id: 'math', nameAr: 'رياضيات', nameEn: 'Math'),
  TeacherSubjectRef(id: 'sci', nameAr: 'علوم', nameEn: 'Science'),
]);
const _ali = ChildTeacher(id: 't2', nameAr: 'أ. علي', nameEn: 'Mr Ali', subjects: [
  TeacherSubjectRef(id: 'ar', nameAr: 'لغة عربية', nameEn: 'Arabic'),
]);

void _phoneSize(WidgetTester tester) {
  tester.view.physicalSize = const Size(1080, 2400);
  tester.view.devicePixelRatio = 2.6;
  addTearDown(tester.view.reset);
}

Widget _app(Widget screen) => MaterialApp(home: Directionality(textDirection: TextDirection.rtl, child: screen));

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await AcademicYearController.instance.reset();
  });

  testWidgets('Evaluations tab: every evaluation of the year, asked for or by the teacher alone; filters',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await AcademicYearController.instance
        .select(AcademicYear(id: 'y-2026', name: '2026-2027', startDate: DateTime(2026, 9, 1), endDate: DateTime(2027, 6, 30)));
    final asked = <String>[];

    await tester.pumpWidget(_app(EvaluationRequestsScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      api: EvaluationApi(
        loadTeachers: (_) async => const [_ali, _mona],
        loadRequests: (_) async => [
          EvaluationRequestItem(
              id: 'r1', studentId: 'c1', teacherId: 't1', subjectId: 'math', requestedAt: DateTime(2026, 11, 15), status: RequestStatus.evaluated, evaluationId: 'e1'),
        ],
        loadEvaluations: (id, from, to) async {
          asked.add('${from.toIso8601String().substring(0, 10)} ${to.toIso8601String().substring(0, 10)}');
          return [
            // Answer of the request
            ChildEvaluation(
                id: 'e1', teacherId: 't1', subjectId: 'math', date: DateTime(2026, 11, 16), focus: 5, interaction: 4, behavior: 5,
                health: ChildHealth.good, evaluationRequestId: 'r1'),
            // Ali on his own
            ChildEvaluation(
                id: 'e2', teacherId: 't2', subjectId: 'ar', date: DateTime(2026, 11, 20), focus: 3, interaction: 2, behavior: 4,
                health: ChildHealth.sick, comment: 'كانت متعبة اليوم'),
          ];
        },
      ),
    )));
    await tester.pumpAndSettle();

    // The year's range; the year in the header
    expect(asked, ['2026-09-01 2026-11-22']);
    expect(find.text('2026-2027'), findsOneWidget);
    // Both, newest first, tagged by where they came from
    expect(find.text('بطلب منك'), findsOneWidget);
    expect(find.text('من المعلم'), findsOneWidget);
    expect(find.text('رياضيات · 16 نوفمبر 2026'), findsOneWidget);
    expect(find.text('لغة عربية · 20 نوفمبر 2026'), findsOneWidget);
    expect(find.text('كانت متعبة اليوم'), findsOneWidget);
    expect(find.text('2'), findsWidgets);
    // Averages: focus (5+3)/2
    expect(find.text('4.0'), findsOneWidget);

    // Only the teacher's own
    await tester.tap(find.byKey(const ValueKey('evaluations-filter-teacher')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('evaluation-e2')), findsOneWidget);
    expect(find.byKey(const ValueKey('evaluation-e1')), findsNothing);

    // Only the requested ones
    await tester.tap(find.byKey(const ValueKey('evaluations-filter-requested')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('evaluation-e1')), findsOneWidget);
    expect(find.byKey(const ValueKey('evaluation-e2')), findsNothing);
  });

  testWidgets('An "all subjects" request shows one answer per subject, also while it is still waiting',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await tester.pumpWidget(_app(EvaluationRequestsScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      api: EvaluationApi(
        loadTeachers: (_) async => const [_mona],
        loadRequests: (_) async => [
          // Answered in both of Mona's subjects
          EvaluationRequestItem(id: 'r1', studentId: 'c1', teacherId: 't1', requestedAt: DateTime(2026, 11, 10), status: RequestStatus.evaluated, evaluationId: 'e2'),
          // Waiting: math done, science not yet
          EvaluationRequestItem(id: 'r2', studentId: 'c1', teacherId: 't1', requestedAt: DateTime(2026, 11, 20), status: RequestStatus.pending),
        ],
        loadEvaluations: (id, from, to) async => [
          ChildEvaluation(id: 'e1', teacherId: 't1', subjectId: 'math', date: DateTime(2026, 11, 11), focus: 4, interaction: 4, behavior: 4,
              health: ChildHealth.good, evaluationRequestId: 'r1'),
          ChildEvaluation(id: 'e2', teacherId: 't1', subjectId: 'sci', date: DateTime(2026, 11, 12), focus: 3, interaction: 3, behavior: 3,
              health: ChildHealth.good, evaluationRequestId: 'r1'),
          ChildEvaluation(id: 'e3', teacherId: 't1', subjectId: 'math', date: DateTime(2026, 11, 21), focus: 5, interaction: 5, behavior: 5,
              health: ChildHealth.good, evaluationRequestId: 'r2'),
        ],
      ),
    )));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('tab-requests')));
    await tester.pumpAndSettle();

    final answered = find.byKey(const ValueKey('request-r1'));
    expect(find.descendant(of: answered, matching: find.text('رياضيات · 11 نوفمبر 2026')), findsOneWidget);
    expect(find.descendant(of: answered, matching: find.text('علوم · 12 نوفمبر 2026')), findsOneWidget);

    final waiting = find.byKey(const ValueKey('request-r2'));
    expect(find.descendant(of: waiting, matching: find.text('بانتظار المعلم')), findsOneWidget);
    expect(find.descendant(of: waiting, matching: find.text('قيّم المعلم حتى الآن (1):')), findsOneWidget);
    expect(find.descendant(of: waiting, matching: find.text('رياضيات · 21 نوفمبر 2026')), findsOneWidget);
  });

  testWidgets('Requests list: waiting, evaluated with the ratings, cancel a waiting one', (WidgetTester tester) async {
    _phoneSize(tester);
    var requests = [
      EvaluationRequestItem(
          id: 'r1', studentId: 'c1', teacherId: 't1', subjectId: 'math', requestedAt: DateTime(2026, 11, 20), status: RequestStatus.pending, note: 'كيف مستواها؟'),
      EvaluationRequestItem(
          id: 'r2', studentId: 'c1', teacherId: 't2', requestedAt: DateTime(2026, 11, 10), status: RequestStatus.evaluated, evaluationId: 'e1'),
    ];
    final cancelled = <String>[];
    DateTime? evaluationsFrom;

    await tester.pumpWidget(_app(EvaluationRequestsScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      api: EvaluationApi(
        loadTeachers: (_) async => const [_ali, _mona],
        loadRequests: (_) async => requests,
        loadEvaluations: (id, from, to) async {
          evaluationsFrom = from;
          return [
            ChildEvaluation(
                id: 'e1', teacherId: 't2', subjectId: 'ar', date: DateTime(2026, 11, 12), focus: 4, interaction: 5, behavior: 3,
                health: ChildHealth.tired, comment: 'مشاركة رائعة'),
          ];
        },
        cancel: (id) async {
          cancelled.add(id);
          requests = [
            for (final r in requests)
              r.id == id
                  ? EvaluationRequestItem(id: r.id, studentId: r.studentId, teacherId: r.teacherId, subjectId: r.subjectId, requestedAt: r.requestedAt, status: RequestStatus.cancelled)
                  : r,
          ];
        },
      ),
    )));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('tab-requests')));
    await tester.pumpAndSettle();

    // Waiting request to Mona about math, with the note
    expect(find.text('أ. منى'), findsOneWidget);
    expect(find.text('رياضيات · 20 نوفمبر 2026'), findsOneWidget);
    expect(find.text('كيف مستواها؟'), findsOneWidget);
    expect(find.text('بانتظار المعلم'), findsOneWidget);
    // Ali answered (all subjects): ratings, health and comment; evaluations asked from before the oldest request
    expect(find.text('كل المواد · 10 نوفمبر 2026'), findsOneWidget);
    expect(evaluationsFrom, DateTime(2026, 11, 1));
    expect(find.text('جيد جداً'), findsOneWidget);
    expect(find.text('ممتاز'), findsOneWidget);
    expect(find.text('متعب'), findsOneWidget);
    expect(find.text('مشاركة رائعة'), findsOneWidget);

    // Cancel the waiting one (with confirmation)
    await tester.tap(find.byKey(const ValueKey('cancel-request-r1')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('confirm-cancel-request')));
    await tester.pumpAndSettle();
    expect(cancelled, ['r1']);
    expect(find.text('ملغي'), findsOneWidget);
    expect(find.byKey(const ValueKey('cancel-request-r1')), findsNothing);
  });

  testWidgets('New request: choose a teacher and a subject, add a note, send', (WidgetTester tester) async {
    _phoneSize(tester);
    final sent = <String>[];
    var requests = <EvaluationRequestItem>[
      EvaluationRequestItem(id: 'r1', studentId: 'c1', teacherId: 't2', subjectId: 'ar', requestedAt: DateTime(2026, 11, 20), status: RequestStatus.pending),
    ];

    await tester.pumpWidget(_app(EvaluationRequestsScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      api: EvaluationApi(
        loadTeachers: (_) async => const [_ali, _mona],
        loadRequests: (_) async => requests,
        loadEvaluations: (id, from, to) async => const [],
        create: (studentId, teacherId, subjectId, note) async {
          sent.add('$studentId $teacherId $subjectId $note');
          requests = [
            EvaluationRequestItem(id: 'r2', studentId: studentId, teacherId: teacherId, subjectId: subjectId, requestedAt: DateTime(2026, 11, 22), status: RequestStatus.pending, note: note),
            ...requests,
          ];
        },
      ),
    )));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('tab-requests')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('new-evaluation-request')));
    await tester.pumpAndSettle();

    // Ali's only subject already has a waiting request: he cannot be chosen
    expect(find.text('لديك طلب بانتظار هذا المعلم في كل مواده'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('teacher-t2')));
    await tester.pump();
    final send = find.byKey(const ValueKey('send-evaluation-request'));
    expect(tester.widget<FilledButton>(send).onPressed, isNull);

    // Mona teaches two subjects: one has to be chosen (no "all subjects")
    await tester.tap(find.byKey(const ValueKey('teacher-t1')));
    await tester.pumpAndSettle();
    expect(find.text('كل المواد'), findsNothing);
    expect(tester.widget<FilledButton>(send).onPressed, isNull);
    await tester.tap(find.byKey(const ValueKey('subject-sci')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const ValueKey('evaluation-request-note')), 'هل تشارك في الحصة؟');
    await tester.tap(send);
    await tester.pumpAndSettle();

    expect(sent, ['c1 t1 sci هل تشارك في الحصة؟']);
    // Back on the list with the new request
    expect(find.text('تم إرسال الطلب إلى المعلم.'), findsOneWidget);
    expect(find.text('علوم · 22 نوفمبر 2026'), findsOneWidget);
  });

  testWidgets('A teacher with one subject sends it; an error from the API is shown', (WidgetTester tester) async {
    _phoneSize(tester);
    String? subject;
    await tester.pumpWidget(_app(NewEvaluationRequestScreen(
      child: _child,
      teachers: const [_ali],
      api: EvaluationApi(create: (s, t, subjectId, note) async {
        subject = subjectId;
        throw const EvaluationException('لديك طلب تقييم قيد الانتظار لدى هذا المعلم بالفعل.');
      }),
    )));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const ValueKey('teacher-t2')));
    await tester.pumpAndSettle();
    // No subject choice for one subject
    expect(find.byKey(const ValueKey('subject-all')), findsNothing);
    await tester.tap(find.byKey(const ValueKey('send-evaluation-request')));
    await tester.pumpAndSettle();

    expect(subject, 'ar');
    expect(find.text('لديك طلب تقييم قيد الانتظار لدى هذا المعلم بالفعل.'), findsOneWidget);
  });

  test('JSON: teachers of the child, requests (UTC times) and evaluations', () {
    final teacher = ChildTeacher.fromJson({
      'teacherId': 't1',
      'nameAr': 'أ. منى',
      'nameEn': 'Ms Mona',
      'subjects': [
        {'subjectId': 'math', 'nameAr': 'رياضيات', 'nameEn': 'Math'},
      ],
    });
    expect(teacher.subjectName('math'), 'رياضيات');
    expect(teacher.subjectName(null), isNull);

    final request = EvaluationRequestItem.fromJson({
      'id': 'r',
      'studentId': 'c1',
      'teacherId': 't1',
      'subjectId': null,
      'requestedAt': '2026-11-20T08:00:00',
      'note': '  ',
      'status': 1,
      'evaluationId': 'e',
    });
    expect(request.status, RequestStatus.evaluated);
    expect(request.note, isNull);
    expect(request.subjectId, isNull);
    expect(request.requestedAt, DateTime.utc(2026, 11, 20, 8).toLocal());

    final evaluation = ChildEvaluation.fromJson({
      'id': 'e',
      'teacherId': 't1',
      'subjectId': 'math',
      'date': '2026-11-21',
      'focus': 5,
      'interaction': 2,
      'behavior': 9,
      'healthStatus': 2,
      'comment': null,
      'evaluationRequestId': 'r',
    });
    expect(evaluation.behavior, 5);
    expect(evaluation.health, ChildHealth.sick);
    expect(evaluation.evaluationRequestId, 'r');
  });
}
