import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  SchoolInfo,
  SchoolInfoResponse,
  SchoolInfoSingleResponse,
  CreateSchoolInfoRequest,
  UpdateSchoolInfoRequest
} from '../models/school-info.model';

/**
 * School Info Service
 * Handles all school information-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class SchoolInfoService {
  private readonly schoolInfoEndpoint = '/api/school-info';

  constructor(private apiService: ApiService) {}

  /**
   * Get school information
   * @returns Observable of school info response
   */
  get(): Observable<SchoolInfoSingleResponse> {
    return this.apiService.get<SchoolInfoSingleResponse>(
      `${this.schoolInfoEndpoint}/get`
    );
  }

  /**
   * Create new school information
   * @param request - The school info data to create
   * @returns Observable of school info response
   */
  create(request: CreateSchoolInfoRequest): Observable<SchoolInfoSingleResponse> {
    return this.apiService.post<SchoolInfoSingleResponse>(
      `${this.schoolInfoEndpoint}/create`,
      request
    );
  }

  /**
   * Update existing school information
   * @param request - The school info data to update
   * @returns Observable of school info response
   */
  update(request: UpdateSchoolInfoRequest): Observable<SchoolInfoSingleResponse> {
    return this.apiService.put<SchoolInfoSingleResponse>(
      `${this.schoolInfoEndpoint}/update`,
      request
    );
  }
}
