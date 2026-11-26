import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { SetStudentStatusRequest, StudentListResponse, StudentSingleResponse, PromoteStudentsRequest, AssignSectionRequest } from '../models/student.model';
import { ApiResponse } from '../models/stage.model';

/**
 * Student Service (read-only lookups)
 * Students are created/edited through their parent (ParentService).
 */
@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private readonly endpoint = '/api/student';

  constructor(private apiService: ApiService) {}

  /** GET /api/student/get-all */
  getAll(): Observable<StudentListResponse> {
    return this.apiService.get<StudentListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/student/get/{id} */
  /** Current / graduated / transferred */
  setStatus(request: SetStudentStatusRequest): Observable<StudentSingleResponse> {
    return this.apiService.put<StudentSingleResponse>('/api/student/set-status', request);
  }

  /** End-of-year promotion of the approved students; data = the number moved */
  promote(request: PromoteStudentsRequest): Observable<ApiResponse<number>> {
    return this.apiService.put<ApiResponse<number>>(`${this.endpoint}/promote`, request);
  }

  /** Places students of a grade in one of its sections (or out of their section); data = the number moved */
  assignSection(request: AssignSectionRequest): Observable<ApiResponse<number>> {
    return this.apiService.put<ApiResponse<number>>(`${this.endpoint}/assign-section`, request);
  }

  getById(id: string): Observable<StudentSingleResponse> {
    return this.apiService.get<StudentSingleResponse>(`${this.endpoint}/get/${id}`);
  }
}
