import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
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
