import { ApiResponse } from './stage.model';

/**
 * Mirrors SchoolManager.Notification.Domain.EventForEnum (serialized as its number).
 */
export enum EventFor {
  All = 1,
  Stage = 2,
  Grade = 3,
  Section = 4,
  Teacher = 5,
  Student = 6
}

export const EVENT_FOR_OPTIONS: { value: EventFor; key: string; icon: string }[] = [
  { value: EventFor.All, key: 'schoolEvents.eventFor.all', icon: 'pe-7s-global' },
  { value: EventFor.Stage, key: 'schoolEvents.eventFor.stage', icon: 'pe-7s-network' },
  { value: EventFor.Grade, key: 'schoolEvents.eventFor.grade', icon: 'pe-7s-study' },
  { value: EventFor.Section, key: 'schoolEvents.eventFor.section', icon: 'pe-7s-albums' },
  { value: EventFor.Teacher, key: 'schoolEvents.eventFor.teacher', icon: 'pe-7s-id' },
  { value: EventFor.Student, key: 'schoolEvents.eventFor.student', icon: 'pe-7s-user' }
];

/** A target as returned by the API (backend EventTargetDto) */
export interface EventTarget {
  id: string;
  eventId: string;
  gradeId: string | null;
  sectionId: string | null;
  teacherId: string | null;
  studentId: string | null;
}

/** A target as sent to the API (backend TargetInputDto); at least one id must be set */
export interface EventTargetInput {
  gradeId: string | null;
  sectionId: string | null;
  teacherId: string | null;
  studentId: string | null;
}

/** School event (backend SchoolEventDto) */
export interface SchoolEvent {
  id: string;
  title: string;
  description: string;
  /** yyyy-MM-dd */
  eventDate: string;
  /** HH:mm:ss */
  startTime: string;
  /** HH:mm:ss */
  endTime: string;
  imageUrl: string | null;
  eventFor: EventFor;
  academicYearId: string;
  semesterId: string;
  /** ISO date-time */
  createdOn: string;
  targets: EventTarget[];
}

/** Body for POST /api/school-event/create (backend CreateSchoolEventDto) */
export interface CreateSchoolEventRequest {
  title: string;
  description: string;
  /** yyyy-MM-dd */
  eventDate: string;
  /** HH:mm:ss */
  startTime: string;
  /** HH:mm:ss */
  endTime: string;
  imageUrl: string | null;
  eventFor: EventFor;
  academicYearId: string;
  semesterId: string;
  /** Empty for EventFor.All */
  targets: EventTargetInput[];
}

/** Body for PUT /api/school-event/update (backend UpdateSchoolEventCommand); targets replace the existing ones */
export interface UpdateSchoolEventRequest extends CreateSchoolEventRequest {
  id: string;
}

export type SchoolEventListResponse = ApiResponse<SchoolEvent[]>;

export type SchoolEventSingleResponse = ApiResponse<SchoolEvent>;
