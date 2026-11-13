import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  ExamResultListResponse,
  SaveExamResultsRequest,
  SaveExamResultsResponse
} from '../models/exam-result.model';

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

  getByExam(examId: string): Observable<ExamResultListResponse> {
    return this.apiService.get<ExamResultListResponse>(`${this.endpoint}/get-by-exam/${examId}`);
  }

  /** Create / update / remove (score = null) many results of one exam at once */
  saveBulk(request: SaveExamResultsRequest): Observable<SaveExamResultsResponse> {
    return this.apiService.post<SaveExamResultsResponse>(`${this.endpoint}/save-bulk`, request);
  }
}
