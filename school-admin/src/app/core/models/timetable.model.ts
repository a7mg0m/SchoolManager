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
 * Timetable Model
 */
export interface Timetable {
  id: string;
  stageId: string;
  gradeId: string;
  academicYearId: string;
  slots: TimetableSlot[];
}

/**
 * Request model for creating a timetable
 */
export interface CreateTimetableRequest {
  stageId: string;
  gradeId: string;
  academicYearId: string;
  slots: TimetableSlot[];
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
  slots: TimetableSlot[];
}

export type TimetableListResponse = ApiResponse<Timetable[]>;

export type TimetableSingleResponse = ApiResponse<Timetable>;
