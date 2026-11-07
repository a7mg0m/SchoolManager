import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { Stage, StageListResponse, StageSingleResponse } from '../models/stage.model';

/**
 * Stage Service
 * Handles all stage-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class StageService {
  private readonly stageEndpoint = '/api/stage';

  constructor(private apiService: ApiService) {}

  /**
   * Get all stages from the API
   * @returns Observable of stages list response
   */
  getAllStages(): Observable<StageListResponse> {
    return this.apiService.get<StageListResponse>(`${this.stageEndpoint}/get-all`);
  }

  /**
   * Activate a stage by ID
   * @param id - Stage ID
   * @returns Observable of stage response
   */
  activateStage(id: string): Observable<StageSingleResponse> {
    return this.apiService.put<StageSingleResponse>(
      `${this.stageEndpoint}/activate/${id}`,
      {}
    );
  }

  /**
   * Deactivate a stage by ID
   * @param id - Stage ID
   * @returns Observable of stage response
   */
  deactivateStage(id: string): Observable<StageSingleResponse> {
    return this.apiService.put<StageSingleResponse>(
      `${this.stageEndpoint}/deactivate/${id}`,
      {}
    );
  }
}
