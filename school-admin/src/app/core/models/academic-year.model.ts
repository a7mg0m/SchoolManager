/**
 * Semester Model
 * Represents a semester within an academic year
 */
export interface AcademicYearSemester {
  id?: string;
  name: string;
  startDate: string;
  endDate: string;
}

/**
 * Academic Year Model
 * Represents an educational academic year with start and end dates
 */
export interface AcademicYear {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  semesters: AcademicYearSemester[];
}

/**
 * Request model for creating a new academic year
 */
export interface CreateAcademicYearRequest {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  semesters: AcademicYearSemester[];
}

/**
 * Request model for updating an academic year
 */
export interface UpdateAcademicYearRequest {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  semesters: AcademicYearSemester[];
}

/**
 * API Response for academic year operations
 */
export interface ApiResponse<T> {
  status: number;
  insertedId: string | null;
  message: string;
  data: T;
  errors: any;
}

/**
 * Typed response for academic year list
 */
export type AcademicYearListResponse = ApiResponse<AcademicYear[]>;

/**
 * Typed response for academic year single operation
 */
export type AcademicYearSingleResponse = ApiResponse<AcademicYear>;
