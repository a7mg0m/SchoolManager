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
  sectionId: string;
  academicYearId: string;
  parentId: string;
  isActive: boolean;
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
  sectionId: string;
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
}

/**
 * Typed response for parent list
 */
export type ParentListResponse = ApiResponse<Parent[]>;

/**
 * Typed response for parent single operation
 */
export type ParentSingleResponse = ApiResponse<Parent>;
