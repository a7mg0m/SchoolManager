import { ApiResponse } from './stage.model';

/**
 * Exam type — mirrors backend enum SchoolManager.ExamsHomework.Domain.ExamType
 */
export enum ExamType {
  Quiz = 1,
  Monthly = 2,
  MidTerm = 3,
  Final = 4
}

/** Exam types with their translation keys, for selects and badges */
export const EXAM_TYPES: { value: ExamType; key: string; badge: string }[] = [
  { value: ExamType.Quiz, key: 'exams.types.quiz', badge: 'bg-info' },
  { value: ExamType.Monthly, key: 'exams.types.monthly', badge: 'bg-primary' },
  { value: ExamType.MidTerm, key: 'exams.types.midTerm', badge: 'bg-warning' },
  { value: ExamType.Final, key: 'exams.types.final', badge: 'bg-danger' }
];

/**
 * Exam Model (backend ExamDto)
 * examDate: "yyyy-MM-dd" (DateOnly)
 * startTime / endTime: "HH:mm:ss" (TimeOnly)
 * duration: minutes
 * weight: full mark, successScore: pass mark
 */
export interface Exam {
  id: string;
  subjectId: string;
  stageId: string;
  gradeId: string;
  /** null = exam for all sections of the grade */
  sectionId: string | null;
  academicYearId: string;
  semesterId: string;
  examType: ExamType;
  examDate: string;
  startTime: string;
  endTime: string;
  duration: number;
  /** Full mark (maximum score) */
  weight: number;
  /** Minimum score to pass (0..weight) */
  successScore: number;
  answerModelUrl: string | null;
}

/**
 * Request model for creating an exam (backend CreateExamDto)
 */
export interface CreateExamRequest {
  subjectId: string;
  stageId: string;
  gradeId: string;
  /** null = exam for all sections of the grade */
  sectionId: string | null;
  academicYearId: string;
  semesterId: string;
  examType: ExamType;
  examDate: string;
  startTime: string;
  endTime: string;
  duration: number;
  /** Full mark (maximum score) */
  weight: number;
  /** Minimum score to pass (0..weight) */
  successScore: number;
  answerModelUrl: string | null;
}

/**
 * Request model for updating an exam (backend UpdateExamCommand)
 */
export interface UpdateExamRequest extends CreateExamRequest {
  id: string;
}

export type ExamListResponse = ApiResponse<Exam[]>;

export type ExamSingleResponse = ApiResponse<Exam>;
