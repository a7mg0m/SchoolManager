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
  /** Job level (المستوى الوظيفي), free text; null when not set */
  jobLevel?: string | null;
  /** The teacher no longer works at the school (inactive: no teacher app login) */
  isServiceEnded?: boolean;
  /** Last day of service (yyyy-MM-dd) */
  serviceEndDate?: string | null;
  /** Job information, set by the school (read only in the teacher app) */
  jobTitle?: string | null;
  employeeNumber?: string | null;
  qualification?: string | null;
  specialization?: string | null;
  /** yyyy-MM-dd */
  hireDate?: string | null;
  yearsOfExperience?: number | null;
  /** Login name of the teacher's account (null for teachers saved before it existed) */
  loginName?: string | null;
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
  /** Job level, free text (null when empty) */
  jobLevel: string | null;
  /** The teacher no longer works at the school (inactive: no teacher app login) */
  isServiceEnded?: boolean;
  /** Last day of service (yyyy-MM-dd) */
  serviceEndDate?: string | null;
  /** Job information, set by the school (read only in the teacher app) */
  jobTitle?: string | null;
  employeeNumber?: string | null;
  qualification?: string | null;
  specialization?: string | null;
  /** yyyy-MM-dd */
  hireDate?: string | null;
  yearsOfExperience?: number | null;
  /** Login name of the teacher's account (required) */
  loginName: string;
  /** First password (required) */
  password: string;
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
  /** Job level, free text (null when empty) */
  jobLevel: string | null;
  /** The teacher no longer works at the school (inactive: no teacher app login) */
  isServiceEnded?: boolean;
  /** Last day of service (yyyy-MM-dd) */
  serviceEndDate?: string | null;
  /** Job information, set by the school (read only in the teacher app) */
  jobTitle?: string | null;
  employeeNumber?: string | null;
  qualification?: string | null;
  specialization?: string | null;
  /** yyyy-MM-dd */
  hireDate?: string | null;
  yearsOfExperience?: number | null;
  loginName: string;
  /** New password, or null to keep the current one */
  password: string | null;
}

/**
 * Typed response for teacher list
 */
export type TeacherListResponse = ApiResponse<Teacher[]>;

/**
 * Typed response for teacher single operation
 */
export type TeacherSingleResponse = ApiResponse<Teacher>;
