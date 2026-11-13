import { ApiResponse } from './stage.model';

/**
 * Exam Result Model (backend ExamResultDto)
 */
export interface ExamResult {
  id: string;
  examId: string;
  studentId: string;
  score: number;
  /** score >= exam.successScore (computed by the server) */
  isPassed: boolean;
}

/** One student's score in a bulk save. score = null removes the existing result. */
export interface StudentScore {
  studentId: string;
  score: number | null;
}

/** Request for POST /api/exam-result/save-bulk */
export interface SaveExamResultsRequest {
  examId: string;
  results: StudentScore[];
}

export interface SaveExamResultsSummary {
  created: number;
  updated: number;
  removed: number;
}

export type ExamResultListResponse = ApiResponse<ExamResult[]>;

export type SaveExamResultsResponse = ApiResponse<SaveExamResultsSummary>;
