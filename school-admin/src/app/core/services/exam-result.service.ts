import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  ExamResultListResponse,
  SaveExamResultsRequest,
  SaveExamResultsResponse
} from '../models/exam-result.model';

/** One exam's results in numbers (GET /api/exam-result/summary) */
export interface ExamResultSummary {
  examId: string;
  count: number;
  passed: number;
  averageScore: number;
}

/**
 * Exam Result Service
 * Student scores for an exam (ExamsHomework service, via gateway /api/exam-result)
 */
@Injectable({
  providedIn: 'root'
})
export class ExamResultService {
  private readonly endpoint = '/api/exam-result';

  constructor(private apiService: ApiService) {}

  /** Every result of every exam (home page pass rates) */
  /** Home dashboard: per exam with results, the count, passes and average score */
  getSummary(): Observable<{ status: number; data: ExamResultSummary[] }> {
    return this.apiService.get<{ status: number; data: ExamResultSummary[] }>('/api/exam-result/summary');
  }

  getAll(): Observable<ExamResultListResponse> {
    return this.apiService.get<ExamResultListResponse>(`${this.endpoint}/get-all`);
  }

  getByExam(examId: string): Observable<ExamResultListResponse> {
    return this.apiService.get<ExamResultListResponse>(`${this.endpoint}/get-by-exam/${examId}`);
  }

  /** Create / update / remove (score = null) many results of one exam at once */
  saveBulk(request: SaveExamResultsRequest): Observable<SaveExamResultsResponse> {
    return this.apiService.post<SaveExamResultsResponse>(`${this.endpoint}/save-bulk`, request);
  }
}
