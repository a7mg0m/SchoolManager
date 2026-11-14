import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  EmployeeGroupListResponse,
  EmployeeGroupSingleResponse,
  CreateEmployeeGroupRequest,
  UpdateEmployeeGroupRequest
} from '../models/employee-group.model';

/**
 * Employee Group Service
 * Handles all employee-group API operations (Identity service)
 */
@Injectable({
  providedIn: 'root'
})
export class EmployeeGroupService {
  private readonly endpoint = '/api/employee-group';

  constructor(private apiService: ApiService) {}

  /**
   * Get all employee groups (with employees count)
   */
  getAll(): Observable<EmployeeGroupListResponse> {
    return this.apiService.get<EmployeeGroupListResponse>(`${this.endpoint}/get-all`);
  }

  /**
   * Get active employee groups only (e.g. for dropdowns)
   */
  getActive(): Observable<EmployeeGroupListResponse> {
    return this.apiService.get<EmployeeGroupListResponse>(`${this.endpoint}/get-active`);
  }

  /**
   * Get a single employee group by ID
   * @param id - Employee group ID
   */
  getById(id: string): Observable<EmployeeGroupSingleResponse> {
    return this.apiService.get<EmployeeGroupSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /**
   * Create a new employee group
   * @param request - The employee group data to create
   */
  create(request: CreateEmployeeGroupRequest): Observable<EmployeeGroupSingleResponse> {
    return this.apiService.post<EmployeeGroupSingleResponse>(`${this.endpoint}/create`, request);
  }

  /**
   * Update an existing employee group
   * @param request - The employee group data to update
   */
  update(request: UpdateEmployeeGroupRequest): Observable<EmployeeGroupSingleResponse> {
    return this.apiService.put<EmployeeGroupSingleResponse>(`${this.endpoint}/update`, request);
  }

  /**
   * Activate an employee group
   * @param id - Employee group ID
   */
  activate(id: string): Observable<EmployeeGroupSingleResponse> {
    return this.apiService.put<EmployeeGroupSingleResponse>(`${this.endpoint}/activate/${id}`, {});
  }

  /**
   * Deactivate an employee group
   * @param id - Employee group ID
   */
  deactivate(id: string): Observable<EmployeeGroupSingleResponse> {
    return this.apiService.put<EmployeeGroupSingleResponse>(`${this.endpoint}/deactivate/${id}`, {});
  }

  /**
   * Delete an employee group (fails while it still has employees)
   * @param id - Employee group ID
   */
  delete(id: string): Observable<EmployeeGroupSingleResponse> {
    return this.apiService.delete<EmployeeGroupSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
