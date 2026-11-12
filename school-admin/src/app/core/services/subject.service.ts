import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  SubjectListResponse,
  SubjectSingleResponse,
  CreateSubjectRequest,
  UpdateSubjectRequest
} from '../models/subject.model';

/**
 * Subject Service
 * Handles all subject-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class SubjectService {
  private readonly subjectEndpoint = '/api/subject';

  constructor(private apiService: ApiService) {}

  /**
   * Get all subjects from the API
   * @returns Observable of subjects list response
   */
  getAll(): Observable<SubjectListResponse> {
    return this.apiService.get<SubjectListResponse>(
      `${this.subjectEndpoint}/get-all`
    );
  }

  /**
   * Get a single subject by ID
   * @param id - Subject ID
   * @returns Observable of subject response
   */
  getById(id: string): Observable<SubjectSingleResponse> {
    return this.apiService.get<SubjectSingleResponse>(
      `${this.subjectEndpoint}/get/${id}`
    );
  }

  /**
   * Create a new subject
   * @param request - The subject data to create
   * @returns Observable of subject response
   */
  create(request: CreateSubjectRequest): Observable<SubjectSingleResponse> {
    return this.apiService.post<SubjectSingleResponse>(
      `${this.subjectEndpoint}/create`,
      request
    );
  }

  /**
   * Update an existing subject
   * @param request - The subject data to update
   * @returns Observable of subject response
   */
  update(request: UpdateSubjectRequest): Observable<SubjectSingleResponse> {
    return this.apiService.put<SubjectSingleResponse>(
      `${this.subjectEndpoint}/update`,
      request
    );
  }

  /**
   * Delete a subject by ID
   * @param id - Subject ID
   * @returns Observable of response
   */
  delete(id: string): Observable<SubjectSingleResponse> {
    return this.apiService.delete<SubjectSingleResponse>(
      `${this.subjectEndpoint}/delete/${id}`
    );
  }
}
