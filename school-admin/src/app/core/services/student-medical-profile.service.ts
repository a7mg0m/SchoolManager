import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  SaveStudentMedicalProfileRequest,
  StudentMedicalProfileListResponse,
  StudentMedicalProfileSingleResponse
} from '../models/student-medical-profile.model';

/**
 * StudentMedicalProfileService
 * Handles the medical profile of students (one per student)
 */
@Injectable({
  providedIn: 'root'
})
export class StudentMedicalProfileService {
  private readonly endpoint = '/api/student-medical-profile';

  constructor(private apiService: ApiService) {}

  /** GET /api/student-medical-profile/get-all */
  getAll(): Observable<StudentMedicalProfileListResponse> {
    return this.apiService.get<StudentMedicalProfileListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/student-medical-profile/get/{studentId} (data null when the student has none yet) */
  getByStudent(studentId: string): Observable<StudentMedicalProfileSingleResponse> {
    return this.apiService.get<StudentMedicalProfileSingleResponse>(`${this.endpoint}/get/${studentId}`);
  }

  /** POST /api/student-medical-profile/create: creates the profile, or updates the student's existing one */
  save(request: SaveStudentMedicalProfileRequest): Observable<StudentMedicalProfileSingleResponse> {
    return this.apiService.post<StudentMedicalProfileSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** DELETE /api/student-medical-profile/delete/{id} */
  delete(id: string): Observable<StudentMedicalProfileSingleResponse> {
    return this.apiService.delete<StudentMedicalProfileSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
