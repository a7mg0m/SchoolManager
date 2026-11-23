import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import {
  EvaluateStudentRequest,
  StudentEvaluationListResponse,
  StudentEvaluationSingleResponse,
  UpdateStudentEvaluationRequest
} from '../models/attendance.model';

/**
 * StudentEvaluationService
 * Talks to the Attendance service through the gateway (/api/student-evaluation).
 * Teachers evaluate from the teacher app; the dashboard only reads.
 */
@Injectable({
  providedIn: 'root'
})
export class StudentEvaluationService {
  private readonly endpoint = '/api/student-evaluation';

  constructor(private apiService: ApiService) {}

  /** POST /api/student-evaluation/evaluate (teacher accounts only) */
  evaluate(request: EvaluateStudentRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/evaluate`, request);
  }

  /** PUT /api/student-evaluation/update (the teacher who made it) */
  update(request: UpdateStudentEvaluationRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/update`, request);
  }

  /** DELETE /api/student-evaluation/delete/{id} */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.apiService.delete<ApiResponse<null>>(`${this.endpoint}/delete/${id}`);
  }

  /** GET /api/student-evaluation/get/{id} */
  getById(id: string): Observable<StudentEvaluationSingleResponse> {
    return this.apiService.get<StudentEvaluationSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/student-evaluation/get-by-period/{timetableSlotId}?date=yyyy-MM-dd */
  getByPeriod(timetableSlotId: string, date: string): Observable<StudentEvaluationListResponse> {
    return this.apiService.get<StudentEvaluationListResponse>(`${this.endpoint}/get-by-period/${timetableSlotId}`, { date });
  }

  /** GET /api/student-evaluation/get-by-student/{studentId}?from=yyyy-MM-dd&to=yyyy-MM-dd */
  getByStudent(studentId: string, from: string, to: string): Observable<StudentEvaluationListResponse> {
    return this.apiService.get<StudentEvaluationListResponse>(`${this.endpoint}/get-by-student/${studentId}`, { from, to });
  }

  /** GET /api/student-evaluation/my-child/{studentId}?from=&to= (parent accounts only) */
  getMyChild(studentId: string, from: string, to: string): Observable<StudentEvaluationListResponse> {
    return this.apiService.get<StudentEvaluationListResponse>(`${this.endpoint}/my-child/${studentId}`, { from, to });
  }
}
