import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  ParentListResponse,
  ParentSingleResponse,
  CreateParentRequest,
  UpdateParentRequest
} from '../models/parent.model';
import { ApiResponse } from '../models/stage.model';

/**
 * Parent Service
 * Handles all parent-related API operations (students are saved with their parent)
 */
@Injectable({
  providedIn: 'root'
})
export class ParentService {
  private readonly parentEndpoint = '/api/parent';

  constructor(private apiService: ApiService) {}

  /**
   * Get all parents (with their students) from the API
   * @returns Observable of parents list response
   */
  getAll(): Observable<ParentListResponse> {
    return this.apiService.get<ParentListResponse>(
      `${this.parentEndpoint}/get-all`
    );
  }

  /**
   * Get a single parent (with students) by ID
   * @param id - Parent ID
   * @returns Observable of parent response
   */
  getById(id: string): Observable<ParentSingleResponse> {
    return this.apiService.get<ParentSingleResponse>(
      `${this.parentEndpoint}/get/${id}`
    );
  }

  /**
   * Create a new parent together with its students
   * @param request - The parent data to create
   * @returns Observable of parent response
   */
  create(request: CreateParentRequest): Observable<ParentSingleResponse> {
    return this.apiService.post<ParentSingleResponse>(
      `${this.parentEndpoint}/create`,
      request
    );
  }

  /**
   * Update a parent and synchronize its students
   * (new students are added, missing ones are deleted)
   * @param request - The parent data to update
   * @returns Observable of parent response
   */
  update(request: UpdateParentRequest): Observable<ParentSingleResponse> {
    return this.apiService.put<ParentSingleResponse>(
      `${this.parentEndpoint}/update`,
      request
    );
  }

  /**
   * Whether a login name is free in the Identity service (logins of employees, teachers and parents).
   * The parent's own account (same id as the parent) is ignored.
   * @returns Observable of response whose data is true when the name is free
   */
  isLoginNameAvailable(loginName: string, parentId?: string | null): Observable<ApiResponse<boolean>> {
    const params = new URLSearchParams({ loginName });
    if (parentId) {
      params.set('userId', parentId);
    }
    return this.apiService.get<ApiResponse<boolean>>(`/api/user/login-name-available?${params.toString()}`);
  }

  /**
   * Delete a parent by ID (the backend also deletes its students)
   * @param id - Parent ID
   * @returns Observable of response
   */
  delete(id: string): Observable<ParentSingleResponse> {
    return this.apiService.delete<ParentSingleResponse>(
      `${this.parentEndpoint}/delete/${id}`
    );
  }
}
