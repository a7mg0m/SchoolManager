import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { Grade, GradeListResponse, GradeSingleResponse } from '../models/grade.model';
import { StageListResponse } from '../models/stage.model';

/**
 * Grade Service
 * Handles all grade-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class GradeService {
  private readonly gradeEndpoint = '/api/grade';
  private readonly stageEndpoint = '/api/stage';

  constructor(private apiService: ApiService) {}

  /**
   * Get active stages from the API
   * @returns Observable of active stages list response
   */
  getActiveStages(): Observable<StageListResponse> {
    return this.apiService.get<StageListResponse>(`${this.stageEndpoint}/get-active`);
  }

  /**
   * Get active grades from the API
   * @returns Observable of active grades list response
   */
  getActiveGrades(): Observable<GradeListResponse> {
    return this.apiService.get<GradeListResponse>(`${this.gradeEndpoint}/get-active`);
  }

  /**
   * Get grades by stage ID from the API
   * @param stageId - Stage ID
   * @returns Observable of grades list response
   */
  getGradesByStage(stageId: string): Observable<GradeListResponse> {
    return this.apiService.get<GradeListResponse>(
      `${this.gradeEndpoint}/get-by-stage/${stageId}`
    );
  }

  /**
   * Activate a grade by ID
   * @param id - Grade ID
   * @returns Observable of grade response
   */
  activateGrade(id: string): Observable<GradeSingleResponse> {
    return this.apiService.put<GradeSingleResponse>(
      `${this.gradeEndpoint}/activate/${id}`,
      {}
    );
  }

  /**
   * Deactivate a grade by ID
   * @param id - Grade ID
   * @returns Observable of grade response
   */
  deactivateGrade(id: string): Observable<GradeSingleResponse> {
    return this.apiService.put<GradeSingleResponse>(
      `${this.gradeEndpoint}/deactivate/${id}`,
      {}
    );
  }
}
