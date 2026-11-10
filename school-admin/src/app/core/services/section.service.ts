import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  SectionListResponse,
  SectionSingleResponse,
  CreateSectionRequest,
  UpdateSectionRequest
} from '../models/section.model';

/**
 * Section Service
 * Handles all section-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class SectionService {
  private readonly sectionEndpoint = '/api/section';

  constructor(private apiService: ApiService) {}

  /**
   * Get all sections from the API
   * @returns Observable of sections list response
   */
  getAll(): Observable<SectionListResponse> {
    return this.apiService.get<SectionListResponse>(
      `${this.sectionEndpoint}/get-all`
    );
  }

  /**
   * Get a single section by ID
   * @param id - Section ID
   * @returns Observable of section response
   */
  getById(id: string): Observable<SectionSingleResponse> {
    return this.apiService.get<SectionSingleResponse>(
      `${this.sectionEndpoint}/get/${id}`
    );
  }

  /**
   * Create a new section
   * @param request - The section data to create
   * @returns Observable of section response
   */
  create(request: CreateSectionRequest): Observable<SectionSingleResponse> {
    return this.apiService.post<SectionSingleResponse>(
      `${this.sectionEndpoint}/create`,
      request
    );
  }

  /**
   * Update an existing section
   * @param request - The section data to update
   * @returns Observable of section response
   */
  update(request: UpdateSectionRequest): Observable<SectionSingleResponse> {
    return this.apiService.put<SectionSingleResponse>(
      `${this.sectionEndpoint}/update`,
      request
    );
  }

  /**
   * Delete a section by ID
   * @param id - Section ID
   * @returns Observable of response
   */
  delete(id: string): Observable<SectionSingleResponse> {
    return this.apiService.delete<SectionSingleResponse>(
      `${this.sectionEndpoint}/delete/${id}`
    );
  }
}
