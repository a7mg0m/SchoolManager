import { TranslateService } from '@ngx-translate/core';

/**
 * The Attendance API returns either a key ("NotATeacher", "StudentNotFound", validator keys) or an English
 * DomainException sentence. Sentences are mapped to a key here so they can be translated
 * (attendance.errors.<key>).
 */
const MESSAGE_KEYS: Record<string, string> = {
  'Academic year is required.': 'AcademicYearIdRequired',
  'Attendance status is not valid.': 'StatusInvalid',
  'Check-out time cannot be before the check-in time.': 'CheckOutBeforeCheckIn',
  'Another teacher already evaluated this student in this lesson.': 'AlreadyEvaluatedByAnotherTeacher',
  'Evaluation is required.': 'EvaluationRequired',
  'Evaluation request not found.': 'EvaluationRequestNotFound',
  'Grade is required.': 'GradeIdRequired',
  'Health status is not valid.': 'HealthStatusInvalid',
  'Only a parent can do this.': 'NotAParent',
  'Only a parent can see their evaluation requests.': 'NotAParent',
  'Only a pending request can be cancelled.': 'OnlyPendingRequestCancelled',
  'Only a pending request can be evaluated.': 'OnlyPendingRequestEvaluated',
  'Only a teacher can evaluate a student.': 'NotATeacher',
  'Only a teacher can record lesson attendance.': 'NotATeacher',
  'Only a teacher can see evaluation requests.': 'NotATeacher',
  "Only school staff can set a student's attendance.": 'NotSchoolStaff',
  'Only the parent who made the request can cancel it.': 'NotRequestOwner',
  'Only the teacher who made the evaluation can change it.': 'NotEvaluationOwnerChange',
  'Only the teacher who made the evaluation can delete it.': 'NotEvaluationOwnerDelete',
  'Parent is required.': 'ParentIdRequired',
  'Ratings must be from Weak (1) to Excellent (5).': 'RatingsInvalid',
  'Section is required.': 'SectionIdRequired',
  'Student is required.': 'StudentIdRequired',
  'Subject is required.': 'SubjectIdRequired',
  'Teacher is required.': 'TeacherIdRequired',
  'Timetable slot is required.': 'TimetableSlotIdRequired',
  'The evaluation request is not for this student and teacher.': 'EvaluationRequestMismatch',
  'The student is not one of your children.': 'StudentNotFound',
  'There is already a pending evaluation request for this student to this teacher.': 'PendingRequestExists',
  'Your children could not be checked right now. Please try again later.': 'ManagementUnavailable'
};

/** Sentences with a number in them: the number is passed to the translation as {{max}} */
const MESSAGE_PATTERNS: { pattern: RegExp; key: string }[] = [
  { pattern: /^Absence reason cannot exceed (\d+) characters\.$/, key: 'AbsenceReasonTooLong' },
  { pattern: /^Comment cannot exceed (\d+) characters\.$/, key: 'CommentTooLong' },
  { pattern: /^Note cannot exceed (\d+) characters\.$/, key: 'NoteTooLong' },
  { pattern: /^Notes cannot exceed (\d+) characters\.$/, key: 'NotesTooLong' }
];

/**
 * Text of an API error of the attendance pages (e.g. "Student is required." -> attendance.errors.StudentIdRequired),
 * or the fallback message when the error is unknown / missing.
 */
export function attendanceApiError(translate: TranslateService, key: string | null | undefined, fallbackKey: string): string {
  if (key) {
    let errorKey = MESSAGE_KEYS[key] ?? key;
    let params: Record<string, string> = {};
    for (const p of MESSAGE_PATTERNS) {
      const match = p.pattern.exec(key);
      if (match) {
        errorKey = p.key;
        params = { max: match[1] };
        break;
      }
    }
    const translationKey = `attendance.errors.${errorKey}`;
    const text = translate.instant(translationKey, params);
    if (text !== translationKey) return text;
  }
  return translate.instant(fallbackKey);
}
