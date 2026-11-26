import { ApiResponse } from './stage.model';

/**
 * Student as returned inside a parent (backend StudentDto)
 */
export interface ParentStudent {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  /** ISO date (yyyy-MM-dd) */
  dateOfBirth: string;
  nationalId: string;
  gender: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  gradeId: string;
  sectionId: string | null;
  academicYearId: string;
  parentId: string;
  isActive: boolean;
  /** 1 Current, 2 Graduated, 3 Transferred (missing: current) */
  status?: number;
}

/** A student still at the school (not graduated / transferred) */
export function isCurrentStudent(s: { status?: number | null }): boolean {
  return (s.status ?? 1) === 1;
}

/**
 * Parent Model (backend ParentDto)
 */
export interface Parent {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  /** Login name of the parent's account (null for parents saved before it existed: they use the phone number) */
  loginName: string | null;
  students: ParentStudent[];
}

/**
 * Student payload sent with a parent create/update (backend ParentStudentDto).
 * `id` is null for a new student and the existing ID for a student being kept/updated.
 * On update the backend ignores dateOfBirth, nationalId and gender for existing students.
 */
export interface ParentStudentDto {
  id: string | null;
  fullNameAr: string;
  fullNameEn: string;
  /** ISO date (yyyy-MM-dd) */
  dateOfBirth: string;
  nationalId: string;
  gender: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  gradeId: string;
  sectionId: string | null;
  academicYearId: string;
}

/**
 * Request model for creating a parent (backend CreateParentDto)
 */
export interface CreateParentRequest {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  students: ParentStudentDto[];
  /** Login name of the parent's account */
  loginName: string;
  /** First password of the account (required on create) */
  password: string;
}

/**
 * Request model for updating a parent (backend UpdateParentDto).
 * Students missing from the list are deleted by the backend.
 */
export interface UpdateParentRequest {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  students: ParentStudentDto[];
  /** Login name of the parent's account */
  loginName: string;
  /** New password, or null to keep the current one */
  password: string | null;
}

/**
 * Typed response for parent list
 */
export type ParentListResponse = ApiResponse<Parent[]>;

/**
 * Typed response for parent single operation
 */
export type ParentSingleResponse = ApiResponse<Parent>;
