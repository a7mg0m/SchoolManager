import { ApiResponse } from './stage.model';

/**
 * Timetable Slot Model
 * day follows .NET DayOfWeek: 0 = Sunday ... 6 = Saturday
 * startTime / endTime are "HH:mm:ss" strings (TimeOnly)
 */
export interface TimetableSlot {
  id?: string | null;
  day: number;
  nameAr: string;
  nameEn: string;
  startTime: string;
  endTime: string;
  subjectId: string;
  teacherId: string;
  gradeId: string;
  sectionId: string;
}

/**
 * One day of the timetable's week (day: 0 = Sunday ... 6 = Saturday).
 * breakStart / breakEnd: "HH:mm:ss" or null; slotDuration in minutes.
 */
export interface TimetableWorkDay {
  day: number;
  isWorkDay: boolean;
  breakStart: string | null;
  breakEnd: string | null;
  slotDuration: number;
  slotCount: number;
}

/**
 * Timetable Model
 */
export interface Timetable {
  id: string;
  stageId: string;
  gradeId: string;
  academicYearId: string;
  /** null: saved before semesters (used in every semester of its year) */
  semesterId?: string | null;
  slots: TimetableSlot[];
  /** When the first period of a work day starts ("HH:mm:ss"); null when not set */
  dayStartTime?: string | null;
  /** The week settings (empty for timetables saved before them) */
  workDays?: TimetableWorkDay[];
}

/**
 * Request model for creating a timetable
 */
export interface CreateTimetableRequest {
  stageId: string;
  gradeId: string;
  academicYearId: string;
  semesterId: string;
  slots: TimetableSlot[];
  dayStartTime: string | null;
  workDays: TimetableWorkDay[];
}

/**
 * Request model for updating a timetable.
 * Slots are synced: existing ids are updated, missing ids are deleted, null ids are created.
 */
export interface UpdateTimetableRequest {
  id: string;
  stageId: string;
  gradeId: string;
  academicYearId: string;
  semesterId: string;
  slots: TimetableSlot[];
  dayStartTime: string | null;
  workDays: TimetableWorkDay[];
}

export type TimetableListResponse = ApiResponse<Timetable[]>;

export type TimetableSingleResponse = ApiResponse<Timetable>;
