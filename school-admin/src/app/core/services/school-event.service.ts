import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  CreateSchoolEventRequest,
  SchoolEventListResponse,
  SchoolEventSingleResponse,
  UpdateSchoolEventRequest
} from '../models/school-event.model';

/**
 * School Event Service
 * Talks to the Notification service through the gateway (/api/school-event).
 * Validation failures come back as HTTP 400 ProblemDetails (see extractApiErrors).
 */
@Injectable({
  providedIn: 'root'
})
export class SchoolEventService {
  private readonly endpoint = '/api/school-event';

  constructor(private apiService: ApiService) {}

  /** GET /api/school-event/get-all (newest event date first) */
  getAll(): Observable<SchoolEventListResponse> {
    return this.apiService.get<SchoolEventListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/school-event/get/{id} */
  getById(id: string): Observable<SchoolEventSingleResponse> {
    return this.apiService.get<SchoolEventSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/school-event/create */
  create(request: CreateSchoolEventRequest): Observable<SchoolEventSingleResponse> {
    return this.apiService.post<SchoolEventSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/school-event/update */
  update(request: UpdateSchoolEventRequest): Observable<SchoolEventSingleResponse> {
    return this.apiService.put<SchoolEventSingleResponse>(`${this.endpoint}/update`, request);
  }

  /** DELETE /api/school-event/delete/{id} */
  delete(id: string): Observable<SchoolEventSingleResponse> {
    return this.apiService.delete<SchoolEventSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}

/**
 * The Notification API reports validation errors as ProblemDetails:
 * { title, errors: { Title: ["TitleRequired"], ... } }. Returns the error keys.
 */
export function extractApiErrors(error: any): string[] {
  const errors = error?.error?.errors;
  if (errors && typeof errors === 'object') {
    return Object.values(errors).flat().map(String);
  }
  return [];
}
