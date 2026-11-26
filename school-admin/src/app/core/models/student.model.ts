import { ApiResponse } from './stage.model';
import { ParentStudent } from './parent.model';

/**
 * Student (Management service StudentDto).
 * Same shape as ParentStudent plus the stage.
 */
export interface Student extends ParentStudent {
  stageId: string;
  /** 1 Current, 2 Graduated, 3 Transferred */
  status: StudentStatus;
  /** yyyy-MM-dd: when the student graduated / was transferred */
  statusDate: string | null;
  /** E.g. the school the student moved to */
  statusNote: string | null;
}

/** Mirrors SchoolManager.Management.Domain.StudentStatus */
export enum StudentStatus {
  Current = 1,
  Graduated = 2,
  Transferred = 3
}

export const STUDENT_STATUSES: { value: StudentStatus; key: string; badge: string; icon: string }[] = [
  { value: StudentStatus.Current, key: 'students.statuses.current', badge: 'bg-success', icon: 'pe-7s-check' },
  { value: StudentStatus.Graduated, key: 'students.statuses.graduated', badge: 'bg-primary', icon: 'pe-7s-medal' },
  { value: StudentStatus.Transferred, key: 'students.statuses.transferred', badge: 'bg-warning text-dark', icon: 'pe-7s-next-2' }
];

export function studentStatusInfo(status: StudentStatus | null | undefined) {
  return STUDENT_STATUSES.find(s => s.value === status) ?? STUDENT_STATUSES[0];
}

/** Body of PUT /api/student/set-status */
export interface SetStudentStatusRequest {
  id: string;
  status: StudentStatus;
  /** yyyy-MM-dd; today when empty */
  date: string | null;
  note: string | null;
}

/** Body of PUT /api/student/promote: the approved students move to the target grade / year (no section), or graduate */
export interface PromoteStudentsRequest {
  studentIds: string[];
  fromGradeId: string;
  targetGradeId: string | null;
  targetAcademicYearId: string | null;
  graduate: boolean;
}

/** Body of PUT /api/student/assign-section (sectionId null: out of their section) */
export interface AssignSectionRequest {
  studentIds: string[];
  gradeId: string;
  sectionId: string | null;
}

export type StudentListResponse = ApiResponse<Student[]>;

export type StudentSingleResponse = ApiResponse<Student>;
