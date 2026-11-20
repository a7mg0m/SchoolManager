import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import {
  AcademicYear,
  AcademicYearListResponse,
  AcademicYearSingleResponse,
  CreateAcademicYearRequest,
  UpdateAcademicYearRequest
} from '../models/academic-year.model';

/**
 * Academic Year Service
 * Handles all academic year-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class AcademicYearService {
  private readonly academicYearEndpoint = '/api/academic-year';

  /** An academic year was created, updated or deleted (the header reloads its lists) */
  readonly changed$ = new Subject<void>();

  constructor(private apiService: ApiService) {}

  /**
   * Get all academic years from the API
   * @returns Observable of academic years list response
   */
  getAll(): Observable<AcademicYearListResponse> {
    return this.apiService.get<AcademicYearListResponse>(
      `${this.academicYearEndpoint}/get-all`
    );
  }

  /**
   * Get a single academic year by ID
   * @param id - Academic year ID
   * @returns Observable of academic year response
   */
  getById(id: string): Observable<AcademicYearSingleResponse> {
    return this.apiService.get<AcademicYearSingleResponse>(
      `${this.academicYearEndpoint}/get/${id}`
    );
  }

  /**
   * Create a new academic year
   * @param request - The academic year data to create
   * @returns Observable of academic year response
   */
  create(request: CreateAcademicYearRequest): Observable<AcademicYearSingleResponse> {
    return this.apiService.post<AcademicYearSingleResponse>(
      `${this.academicYearEndpoint}/create`,
      request
    ).pipe(tap(() => this.changed$.next()));
  }

  /**
   * Update an existing academic year
   * @param request - The academic year data to update
   * @returns Observable of academic year response
   */
  update(request: UpdateAcademicYearRequest): Observable<AcademicYearSingleResponse> {
    return this.apiService.put<AcademicYearSingleResponse>(
      `${this.academicYearEndpoint}/update`,
      request
    ).pipe(tap(() => this.changed$.next()));
  }

  /**
   * Delete an academic year by ID
   * @param id - Academic year ID
   * @returns Observable of response
   */
  delete(id: string): Observable<AcademicYearSingleResponse> {
    return this.apiService.delete<AcademicYearSingleResponse>(
      `${this.academicYearEndpoint}/delete/${id}`
    ).pipe(tap(() => this.changed$.next()));
  }
}
