import { ApiResponse } from './stage.model';

/**
 * Subject Model
 * Mirrors the backend SubjectDto
 */
export interface Subject {
  id: string;
  nameAr: string;
  nameEn: string;
  gradeId: string;
  teacherIds: string[];
  isActive: boolean;
}

/**
 * Request model for creating a new subject (backend CreateSubjectDto)
 */
export interface CreateSubjectRequest {
  id: string;
  nameAr: string;
  nameEn: string;
  gradeId: string;
  teacherIds: string[];
}

/**
 * Request model for updating a subject (backend UpdateSubjectDto)
 */
export interface UpdateSubjectRequest {
  id: string;
  nameAr: string;
  nameEn: string;
  gradeId: string;
  teacherIds: string[];
}

/**
 * Typed response for subject list
 */
export type SubjectListResponse = ApiResponse<Subject[]>;

/**
 * Typed response for subject single operation
 */
export type SubjectSingleResponse = ApiResponse<Subject>;
