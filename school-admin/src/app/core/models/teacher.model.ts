import { ApiResponse } from './stage.model';

/**
 * Teacher gender values (stored as plain strings by the backend)
 */
export type TeacherGender = 'Male' | 'Female';

/**
 * Teacher Model
 * Mirrors the backend TeacherDto
 */
export interface Teacher {
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
  isActive: boolean;
}

/**
 * Request model for creating a new teacher (backend CreateTeacherDto)
 */
export interface CreateTeacherRequest {
  fullNameAr: string;
  fullNameEn: string;
  /** ISO date (yyyy-MM-dd) */
  dateOfBirth: string;
  nationalId: string;
  gender: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  isActive: boolean;
}

/**
 * Request model for updating a teacher (backend UpdateTeacherDto).
 * Date of birth, national ID and gender cannot be changed after creation.
 */
export interface UpdateTeacherRequest {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  phoneNumber: string;
  email: string | null;
  address: string;
  isActive: boolean;
}

/**
 * Typed response for teacher list
 */
export type TeacherListResponse = ApiResponse<Teacher[]>;

/**
 * Typed response for teacher single operation
 */
export type TeacherSingleResponse = ApiResponse<Teacher>;
