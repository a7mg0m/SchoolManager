import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:smart_school_parent/core/academic_year/academic_year_controller.dart';
import 'package:smart_school_parent/features/attendance/attendance_screen.dart';
import 'package:smart_school_parent/features/attendance/attendance_service.dart';
import 'package:smart_school_parent/features/children/children_service.dart';
import 'package:smart_school_parent/features/schedule/schedule_service.dart';

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

Widget _app(AttendanceScreen screen) => MaterialApp(
      home: Directionality(textDirection: TextDirection.rtl, child: screen),
    );

final _year = AcademicYear(id: 'y-2026', name: '2026-2027', startDate: DateTime(2026, 9, 1), endDate: DateTime(2027, 6, 30));

const _timetable = ChildTimetable(today: 0, now: 0, days: [
  TimetableDay(dayOfWeek: 0, slots: [
    ScheduleSlot(id: 'slot-math', name: '', start: 480, end: 525, subject: 'رياضيات', teacher: 'أ. منى'),
  ]),
]);

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await AcademicYearController.instance.reset();
  });

  testWidgets('Attendance of the selected child in the chosen year: rate, statuses, filters and lessons',
      (WidgetTester tester) async {
    _phoneSize(tester);
    await AcademicYearController.instance.select(_year);
    final asked = <String>[];

    await tester.pumpWidget(_app(AttendanceScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      loadTimetable: (_) async => _timetable,
      loadAttendance: (id, from, to) async {
        asked.add('$id ${attendanceDate(from)} ${attendanceDate(to)}');
        return [
          AttendanceDay(date: DateTime(2026, 11, 22), status: DayStatus.present, attendedLessons: 1, lessons: const [
            AttendanceLesson(timetableSlotId: 'slot-math', subjectId: 'm', start: 480, status: LessonStatus.late),
          ]),
          AttendanceDay(date: DateTime(2026, 11, 19), status: DayStatus.absent, absentLessons: 1, lessons: const [
            AttendanceLesson(timetableSlotId: 'slot-other', subjectId: 'x', start: 570, status: LessonStatus.absent, absenceReason: 'مريضة'),
          ]),
          AttendanceDay(date: DateTime(2026, 11, 18), status: DayStatus.present),
          AttendanceDay(date: DateTime(2026, 10, 5), status: DayStatus.excused, notes: 'سفر مع العائلة'),
        ];
      },
    )));
    await tester.pumpAndSettle();

    // From the year's start to today
    expect(asked, ['c1 2026-09-01 2026-11-22']);
    expect(find.text('2026-2027'), findsOneWidget);
    expect(find.text('سارة أحمد · الصف الثالث - أ'), findsOneWidget);
    // 2 of 4 days attended (present / partly), 1 missed and 1 late lesson
    expect(find.byKey(const ValueKey('attendance-rate')), findsOneWidget);
    expect(find.text('50%'), findsOneWidget);
    expect(find.text('1 حصة غياب · 1 حصة تأخير'), findsOneWidget);
    // Grouped by month, newest first
    expect(find.text('نوفمبر 2026'), findsOneWidget);
    expect(find.text('أكتوبر 2026'), findsOneWidget);

    // Lessons of a day: name from the timetable, unknown slot as "lesson"
    await tester.tap(find.byKey(const ValueKey('attendance-day-2026-11-22')));
    await tester.pumpAndSettle();
    expect(find.text('رياضيات'), findsOneWidget);
    expect(find.text('أ. منى'), findsOneWidget);
    expect(find.text('متأخر'), findsOneWidget);

    // Absences only: the absent and the excused days
    await tester.tap(find.byKey(const ValueKey('attendance-filter-absences')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('attendance-day-2026-11-19')), findsOneWidget);
    expect(find.byKey(const ValueKey('attendance-day-2026-10-05')), findsOneWidget);
    expect(find.byKey(const ValueKey('attendance-day-2026-11-18')), findsNothing);

    await tester.tap(find.byKey(const ValueKey('attendance-day-2026-11-19')));
    await tester.pumpAndSettle();
    expect(find.text('حصة'), findsOneWidget);
    expect(find.text('مريضة'), findsOneWidget);

    // Late only
    await tester.tap(find.byKey(const ValueKey('attendance-filter-late')));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('attendance-day-2026-11-22')), findsOneWidget);
    expect(find.byKey(const ValueKey('attendance-day-2026-11-19')), findsNothing);
  });

  testWidgets('Another academic year reloads its own days; a past year goes to its end', (WidgetTester tester) async {
    _phoneSize(tester);
    await AcademicYearController.instance.select(_year);
    final asked = <String>[];

    await tester.pumpWidget(_app(AttendanceScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      loadTimetable: (_) async => _timetable,
      loadAttendance: (id, from, to) async {
        asked.add('${attendanceDate(from)} ${attendanceDate(to)}');
        return const [];
      },
    )));
    await tester.pumpAndSettle();
    expect(find.text('لا يوجد حضور مسجل لـ سارة أحمد في هذه السنة الدراسية.'), findsOneWidget);

    await AcademicYearController.instance
        .select(AcademicYear(id: 'y-2025', name: '2025-2026', startDate: DateTime(2025, 9, 1), endDate: DateTime(2026, 6, 30)));
    await tester.pumpAndSettle();
    expect(asked, ['2026-09-01 2026-11-22', '2025-09-01 2026-06-30']);
    expect(find.text('2025-2026'), findsOneWidget);
  });

  testWidgets('A failure shows the message and can be tried again', (WidgetTester tester) async {
    _phoneSize(tester);
    var calls = 0;
    await tester.pumpWidget(_app(AttendanceScreen(
      child: _child,
      now: DateTime(2026, 11, 22),
      loadTimetable: (_) async => _timetable,
      loadAttendance: (id, from, to) async {
        calls++;
        if (calls == 1) throw const AttendanceException('تعذر تحميل الحضور.');
        return const [];
      },
    )));
    await tester.pumpAndSettle();
    expect(find.text('تعذر تحميل الحضور.'), findsOneWidget);

    await tester.tap(find.text('إعادة المحاولة'));
    await tester.pumpAndSettle();
    expect(calls, 2);
    expect(find.text('تعذر تحميل الحضور.'), findsNothing);
  });

  test('Attendance JSON: day status, times, lessons by time', () {
    final day = AttendanceDay.fromJson({
      'date': '2026-11-19',
      'status': 2,
      'checkInTime': '07:40:00',
      'checkOutTime': null,
      'notes': ' ',
      'attendedPeriodsCount': 1,
      'absentPeriodsCount': 1,
      'periods': [
        {'timetableSlotId': 'b', 'subjectId': 's', 'startTime': '10:00:00', 'status': 1, 'absenceReason': 'خرج مبكراً'},
        {'timetableSlotId': 'a', 'subjectId': 's', 'startTime': '08:00:00', 'status': 2},
      ],
    });
    expect(day.date, DateTime(2026, 11, 19));
    expect(day.status, DayStatus.partiallyPresent);
    expect(day.checkIn, 460);
    expect(day.checkOut, isNull);
    expect(day.notes, isNull);
    expect(day.lessons.map((l) => l.timetableSlotId).toList(), ['a', 'b']);
    expect(day.lateLessons, 1);
    expect(day.lessons.last.absenceReason, 'خرج مبكراً');
  });
}
