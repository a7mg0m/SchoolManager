import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  TeacherListResponse,
  TeacherSingleResponse,
  CreateTeacherRequest,
  UpdateTeacherRequest
} from '../models/teacher.model';
import { ApiResponse } from '../models/stage.model';

/**
 * Teacher Service
 * Handles all teacher-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class TeacherService {
  private readonly teacherEndpoint = '/api/teacher';

  constructor(private apiService: ApiService) {}

  /**
   * Get all teachers from the API
   * @returns Observable of teachers list response
   */
  getAll(): Observable<TeacherListResponse> {
    return this.apiService.get<TeacherListResponse>(
      `${this.teacherEndpoint}/get-all`
    );
  }

  /**
   * Get a single teacher by ID
   * @param id - Teacher ID
   * @returns Observable of teacher response
   */
  getById(id: string): Observable<TeacherSingleResponse> {
    return this.apiService.get<TeacherSingleResponse>(
      `${this.teacherEndpoint}/get/${id}`
    );
  }

  /**
   * Create a new teacher
   * @param request - The teacher data to create
   * @returns Observable of teacher response
   */
  create(request: CreateTeacherRequest): Observable<TeacherSingleResponse> {
    return this.apiService.post<TeacherSingleResponse>(
      `${this.teacherEndpoint}/create`,
      request
    );
  }

  /**
   * Update an existing teacher
   * @param request - The teacher data to update
   * @returns Observable of teacher response
   */
  update(request: UpdateTeacherRequest): Observable<TeacherSingleResponse> {
    return this.apiService.put<TeacherSingleResponse>(
      `${this.teacherEndpoint}/update`,
      request
    );
  }

  /**
   * Is the login name free? (Identity service: no employee, teacher or parent uses it)
   * @param teacherId - The teacher being edited (its own login name is allowed)
   */
  isLoginNameAvailable(loginName: string, teacherId?: string | null): Observable<ApiResponse<boolean>> {
    const params = new URLSearchParams({ loginName });
    if (teacherId) {
      params.set('userId', teacherId);
    }
    return this.apiService.get<ApiResponse<boolean>>(`/api/user/login-name-available?${params.toString()}`);
  }

  /**
   * Delete a teacher by ID
   * @param id - Teacher ID
   * @returns Observable of response
   */
  delete(id: string): Observable<TeacherSingleResponse> {
    return this.apiService.delete<TeacherSingleResponse>(
      `${this.teacherEndpoint}/delete/${id}`
    );
  }
}
