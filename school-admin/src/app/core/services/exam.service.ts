import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/stage.model';
import { SecondFinalStudent, PromotionFinal } from '../models/exam.model';
import {
  ExamListResponse,
  ExamSingleResponse,
  CreateExamRequest,
  UpdateExamRequest
} from '../models/exam.model';

/**
 * Exam Service
 * Handles exam CRUD operations (ExamsHomework service, via gateway /api/exam)
 */
@Injectable({
  providedIn: 'root'
})
export class ExamService {
  private readonly examEndpoint = '/api/exam';

  constructor(private apiService: ApiService) {}

  getAll(): Observable<ExamListResponse> {
    return this.apiService.get<ExamListResponse>(`${this.examEndpoint}/get-all`);
  }

  /** The Final exams of a grade in a year, with their second finals and scores: the promotion page */
  getPromotionFinals(academicYearId: string, gradeId: string): Observable<ApiResponse<PromotionFinal[]>> {
    return this.apiService.get<ApiResponse<PromotionFinal[]>>(
      `${this.examEndpoint}/promotion-finals?academicYearId=${academicYearId}&gradeId=${gradeId}`);
  }

  /** Students of a second final (those who failed its Final), with their Final score */
  getSecondFinalStudents(id: string): Observable<ApiResponse<SecondFinalStudent[]>> {
    return this.apiService.get<ApiResponse<SecondFinalStudent[]>>(`${this.examEndpoint}/second-final-students/${id}`);
  }

  getById(id: string): Observable<ExamSingleResponse> {
    return this.apiService.get<ExamSingleResponse>(`${this.examEndpoint}/get/${id}`);
  }

  create(request: CreateExamRequest): Observable<ExamSingleResponse> {
    return this.apiService.post<ExamSingleResponse>(`${this.examEndpoint}/create`, request);
  }

  update(request: UpdateExamRequest): Observable<ExamSingleResponse> {
    return this.apiService.put<ExamSingleResponse>(`${this.examEndpoint}/update`, request);
  }

  delete(id: string): Observable<ExamSingleResponse> {
    return this.apiService.delete<ExamSingleResponse>(`${this.examEndpoint}/delete/${id}`);
  }
}
