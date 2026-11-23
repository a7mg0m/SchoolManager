import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import { CreateEvaluationRequestRequest, EvaluationRequestListResponse } from '../models/attendance.model';

/**
 * EvaluationRequestService
 * Talks to the Attendance service through the gateway (/api/evaluation-request).
 * Parents create / cancel requests and teachers see the pending ones (mobile apps).
 */
@Injectable({
  providedIn: 'root'
})
export class EvaluationRequestService {
  private readonly endpoint = '/api/evaluation-request';

  constructor(private apiService: ApiService) {}

  /** POST /api/evaluation-request/create (parent accounts only) */
  create(request: CreateEvaluationRequestRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/evaluation-request/cancel/{id} (the parent who made it) */
  cancel(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/cancel/${id}`, {});
  }

  /** GET /api/evaluation-request/my-pending (teacher accounts only) */
  getMyPending(): Observable<EvaluationRequestListResponse> {
    return this.apiService.get<EvaluationRequestListResponse>(`${this.endpoint}/my-pending`);
  }

  /** GET /api/evaluation-request/my-requests?studentId= (parent accounts only) */
  getMyRequests(studentId?: string | null): Observable<EvaluationRequestListResponse> {
    return this.apiService.get<EvaluationRequestListResponse>(
      `${this.endpoint}/my-requests`, studentId ? { studentId } : undefined);
  }
}
