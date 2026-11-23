import { ApiResponse } from './stage.model';

/*
 * Attendance service (gateway: /api/daily-attendance, /api/student-evaluation, /api/evaluation-request).
 * Enums mirror SchoolManager.Attendance.Domain and are serialized by the API as their numbers.
 * Dates are "yyyy-MM-dd" (DateOnly), times "HH:mm:ss" (TimeOnly).
 */

/** Mirrors SchoolManager.Attendance.Domain.DailyAttendanceStatus */
export enum DailyAttendanceStatus {
  Present = 0,
  Absent = 1,
  PartiallyPresent = 2,
  Excused = 3
}

export const DAILY_ATTENDANCE_STATUSES: { value: DailyAttendanceStatus; key: string; badge: string }[] = [
  { value: DailyAttendanceStatus.Present, key: 'attendance.dailyStatus.present', badge: 'bg-success' },
  { value: DailyAttendanceStatus.Absent, key: 'attendance.dailyStatus.absent', badge: 'bg-danger' },
  { value: DailyAttendanceStatus.PartiallyPresent, key: 'attendance.dailyStatus.partiallyPresent', badge: 'bg-warning text-dark' },
  { value: DailyAttendanceStatus.Excused, key: 'attendance.dailyStatus.excused', badge: 'bg-info text-dark' }
];

/** Mirrors SchoolManager.Attendance.Domain.PeriodAttendanceStatus */
export enum PeriodAttendanceStatus {
  Present = 0,
  Absent = 1,
  Late = 2,
  Excused = 3
}

export const PERIOD_ATTENDANCE_STATUSES: { value: PeriodAttendanceStatus; key: string; badge: string }[] = [
  { value: PeriodAttendanceStatus.Present, key: 'attendance.periodStatus.present', badge: 'bg-success' },
  { value: PeriodAttendanceStatus.Absent, key: 'attendance.periodStatus.absent', badge: 'bg-danger' },
  { value: PeriodAttendanceStatus.Late, key: 'attendance.periodStatus.late', badge: 'bg-warning text-dark' },
  { value: PeriodAttendanceStatus.Excused, key: 'attendance.periodStatus.excused', badge: 'bg-info text-dark' }
];

/** Mirrors SchoolManager.Attendance.Domain.AttendanceSource */
export enum AttendanceSource {
  Teacher = 0,
  Admin = 1
}

export const ATTENDANCE_SOURCES: { value: AttendanceSource; key: string }[] = [
  { value: AttendanceSource.Teacher, key: 'attendance.source.teacher' },
  { value: AttendanceSource.Admin, key: 'attendance.source.admin' }
];

/** Mirrors SchoolManager.Attendance.Domain.EvaluationRating (1..5) */
export enum EvaluationRating {
  Weak = 1,
  Acceptable = 2,
  Good = 3,
  VeryGood = 4,
  Excellent = 5
}

export const EVALUATION_RATINGS: { value: EvaluationRating; key: string; badge: string }[] = [
  { value: EvaluationRating.Weak, key: 'attendance.rating.weak', badge: 'bg-danger' },
  { value: EvaluationRating.Acceptable, key: 'attendance.rating.acceptable', badge: 'bg-warning text-dark' },
  { value: EvaluationRating.Good, key: 'attendance.rating.good', badge: 'bg-info text-dark' },
  { value: EvaluationRating.VeryGood, key: 'attendance.rating.veryGood', badge: 'bg-primary' },
  { value: EvaluationRating.Excellent, key: 'attendance.rating.excellent', badge: 'bg-success' }
];

/** Mirrors SchoolManager.Attendance.Domain.HealthStatus */
export enum HealthStatus {
  Good = 0,
  Tired = 1,
  Sick = 2
}

export const HEALTH_STATUSES: { value: HealthStatus; key: string; badge: string }[] = [
  { value: HealthStatus.Good, key: 'attendance.health.good', badge: 'bg-success' },
  { value: HealthStatus.Tired, key: 'attendance.health.tired', badge: 'bg-warning text-dark' },
  { value: HealthStatus.Sick, key: 'attendance.health.sick', badge: 'bg-danger' }
];

/** Mirrors SchoolManager.Attendance.Domain.EvaluationRequestStatus */
export enum EvaluationRequestStatus {
  Pending = 0,
  Evaluated = 1,
  Cancelled = 2
}

export const EVALUATION_REQUEST_STATUSES: { value: EvaluationRequestStatus; key: string; badge: string }[] = [
  { value: EvaluationRequestStatus.Pending, key: 'attendance.requestStatus.pending', badge: 'bg-warning text-dark' },
  { value: EvaluationRequestStatus.Evaluated, key: 'attendance.requestStatus.evaluated', badge: 'bg-success' },
  { value: EvaluationRequestStatus.Cancelled, key: 'attendance.requestStatus.cancelled', badge: 'bg-secondary' }
];

/** One lesson of a student's day */
export interface PeriodAttendance {
  id: string;
  dailyAttendanceId: string;
  studentId: string;
  timetableSlotId: string;
  subjectId: string;
  teacherId: string;
  /** "HH:mm:ss" */
  startTime: string;
  status: PeriodAttendanceStatus;
  absenceReason: string | null;
  /** ISO date-time */
  recordedAt: string;
}

/** A student's school day (status, check-in / out and the lessons recorded) */
export interface DailyAttendance {
  id: string;
  studentId: string;
  sectionId: string;
  gradeId: string;
  academicYearId: string;
  /** "yyyy-MM-dd" */
  date: string;
  status: DailyAttendanceStatus;
  source: AttendanceSource;
  /** "HH:mm:ss" */
  checkInTime: string | null;
  /** "HH:mm:ss" */
  checkOutTime: string | null;
  notes: string | null;
  attendedPeriodsCount: number;
  absentPeriodsCount: number;
  periods: PeriodAttendance[];
}

/** One student's status in a lesson (teacher app) */
export interface StudentPeriodStatus {
  studentId: string;
  status: PeriodAttendanceStatus;
  absenceReason: string | null;
}

/** Body for POST /api/daily-attendance/record-period (teacher app) */
export interface RecordPeriodAttendanceRequest {
  timetableSlotId: string;
  subjectId: string;
  sectionId: string;
  gradeId: string;
  academicYearId: string;
  date: string;
  startTime: string;
  students: StudentPeriodStatus[];
}

/** Body for POST /api/daily-attendance/set-day (creates or updates the student's day) */
export interface SetDailyAttendanceRequest {
  studentId: string;
  sectionId: string;
  gradeId: string;
  academicYearId: string;
  /** "yyyy-MM-dd" */
  date: string;
  status: DailyAttendanceStatus;
  /** "HH:mm:ss" */
  checkInTime: string | null;
  /** "HH:mm:ss" */
  checkOutTime: string | null;
  notes: string | null;
}

/** A teacher's evaluation of a student in a lesson */
export interface StudentEvaluation {
  id: string;
  studentId: string;
  teacherId: string;
  subjectId: string;
  timetableSlotId: string;
  /** "yyyy-MM-dd" */
  date: string;
  focus: EvaluationRating;
  interaction: EvaluationRating;
  behavior: EvaluationRating;
  healthStatus: HealthStatus;
  comment: string | null;
  evaluationRequestId: string | null;
}

/** Body for POST /api/student-evaluation/evaluate (teacher app) */
export interface EvaluateStudentRequest {
  studentId: string;
  subjectId: string;
  timetableSlotId: string;
  date: string;
  focus: EvaluationRating;
  interaction: EvaluationRating;
  behavior: EvaluationRating;
  healthStatus: HealthStatus;
  comment: string | null;
  evaluationRequestId: string | null;
}

/** Body for PUT /api/student-evaluation/update (teacher app) */
export interface UpdateStudentEvaluationRequest {
  id: string;
  focus: EvaluationRating;
  interaction: EvaluationRating;
  behavior: EvaluationRating;
  healthStatus: HealthStatus;
  comment: string | null;
}

/** A parent's request to a teacher to evaluate their child */
export interface EvaluationRequest {
  id: string;
  studentId: string;
  parentId: string;
  teacherId: string;
  subjectId: string | null;
  /** ISO date-time */
  requestedAt: string;
  note: string | null;
  status: EvaluationRequestStatus;
  evaluationId: string | null;
  /** ISO date-time */
  evaluatedAt: string | null;
}

/** Body for POST /api/evaluation-request/create (parent app) */
export interface CreateEvaluationRequestRequest {
  studentId: string;
  teacherId: string;
  subjectId: string | null;
  note: string | null;
}

export type DailyAttendanceListResponse = ApiResponse<DailyAttendance[]>;

export type DailyAttendanceSingleResponse = ApiResponse<DailyAttendance>;

export type PeriodAttendanceListResponse = ApiResponse<PeriodAttendance[]>;

export type StudentEvaluationListResponse = ApiResponse<StudentEvaluation[]>;

export type StudentEvaluationSingleResponse = ApiResponse<StudentEvaluation>;

export type EvaluationRequestListResponse = ApiResponse<EvaluationRequest[]>;

export type EvaluationRequestSingleResponse = ApiResponse<EvaluationRequest>;
