/**
 * Semester Model
 * Represents a semester within an academic year
 */
export interface AcademicYearSemester {
  id?: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive?: boolean;
}

/** Helpers for the semesters of an academic year */
export const AcademicYearSemesters = {
  /** Semesters with an id, in date order */
  ordered(year: AcademicYear | null | undefined): AcademicYearSemester[] {
    return [...(year?.semesters ?? [])]
      .filter(s => !!s.id)
      .sort((a, b) => a.startDate.localeCompare(b.startDate));
  },

  /** The current semester: today inside it, else an active one, else the first */
  current(year: AcademicYear | null | undefined, today = new Date()): AcademicYearSemester | undefined {
    const list = AcademicYearSemesters.ordered(year);
    const day = today.toISOString().substring(0, 10);
    return list.find(s => s.startDate.substring(0, 10) <= day && day <= s.endDate.substring(0, 10))
      ?? list.find(s => s.isActive)
      ?? list[0];
  }
};

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
