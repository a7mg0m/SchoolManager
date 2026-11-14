import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  EmployeeListResponse,
  EmployeeSingleResponse,
  CreateEmployeeRequest,
  UpdateEmployeeRequest
} from '../models/employee.model';

/**
 * Employee Service
 * Handles all employee API operations (Identity service).
 * Create / update / delete / activate also affect the employee's User and UserProfile.
 */
@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly endpoint = '/api/employee';

  constructor(private apiService: ApiService) {}

  /**
   * Get all employees (with group name and login name)
   */
  getAll(): Observable<EmployeeListResponse> {
    return this.apiService.get<EmployeeListResponse>(`${this.endpoint}/get-all`);
  }

  /**
   * Get a single employee by ID
   * @param id - Employee ID
   */
  getById(id: string): Observable<EmployeeSingleResponse> {
    return this.apiService.get<EmployeeSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /**
   * Get the employees of one group
   * @param groupId - Employee group ID
   */
  getByGroup(groupId: string): Observable<EmployeeListResponse> {
    return this.apiService.get<EmployeeListResponse>(`${this.endpoint}/get-by-group/${groupId}`);
  }

  /**
   * Search employees by name, national id, phone, email or job title
   * @param searchTerm - Text to search for
   */
  search(searchTerm: string): Observable<EmployeeListResponse> {
    return this.apiService.get<EmployeeListResponse>(
      `${this.endpoint}/search?searchTerm=${encodeURIComponent(searchTerm)}`
    );
  }

  /**
   * Create a new employee (and its User with role Admin and UserProfile)
   * @param request - The employee data to create
   */
  create(request: CreateEmployeeRequest): Observable<EmployeeSingleResponse> {
    return this.apiService.post<EmployeeSingleResponse>(`${this.endpoint}/create`, request);
  }

  /**
   * Update an existing employee (and its User and UserProfile)
   * @param request - The employee data to update
   */
  update(request: UpdateEmployeeRequest): Observable<EmployeeSingleResponse> {
    return this.apiService.put<EmployeeSingleResponse>(`${this.endpoint}/update`, request);
  }

  /**
   * Activate an employee (and its login)
   * @param id - Employee ID
   */
  activate(id: string): Observable<EmployeeSingleResponse> {
    return this.apiService.put<EmployeeSingleResponse>(`${this.endpoint}/activate/${id}`, {});
  }

  /**
   * Deactivate an employee (its login is deactivated too)
   * @param id - Employee ID
   */
  deactivate(id: string): Observable<EmployeeSingleResponse> {
    return this.apiService.put<EmployeeSingleResponse>(`${this.endpoint}/deactivate/${id}`, {});
  }

  /**
   * Delete an employee together with its User and UserProfile
   * @param id - Employee ID
   */
  delete(id: string): Observable<EmployeeSingleResponse> {
    return this.apiService.delete<EmployeeSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
