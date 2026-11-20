import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  CreateStudentVaccinationRequest,
  UpdateStudentVaccinationRequest,
  StudentVaccinationListResponse,
  StudentVaccinationSingleResponse
} from '../models/student-vaccination.model';

/**
 * StudentVaccinationService
 * Handles the students' vaccine doses (school clinic)
 */
@Injectable({
  providedIn: 'root'
})
export class StudentVaccinationService {
  private readonly endpoint = '/api/student-vaccination';

  constructor(private apiService: ApiService) {}

  /** GET /api/student-vaccination/get-all */
  getAll(): Observable<StudentVaccinationListResponse> {
    return this.apiService.get<StudentVaccinationListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/student-vaccination/get/{id} */
  getById(id: string): Observable<StudentVaccinationSingleResponse> {
    return this.apiService.get<StudentVaccinationSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/student-vaccination/create */
  create(request: CreateStudentVaccinationRequest): Observable<StudentVaccinationSingleResponse> {
    return this.apiService.post<StudentVaccinationSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/student-vaccination/update */
  update(request: UpdateStudentVaccinationRequest): Observable<StudentVaccinationSingleResponse> {
    return this.apiService.put<StudentVaccinationSingleResponse>(`${this.endpoint}/update`, request);
  }

  /** GET /api/student-vaccination/get-by-student/{studentId} */
  getByStudent(studentId: string): Observable<StudentVaccinationListResponse> {
    return this.apiService.get<StudentVaccinationListResponse>(`${this.endpoint}/get-by-student/${studentId}`);
  }

  /** DELETE /api/student-vaccination/delete/{id} */
  delete(id: string): Observable<StudentVaccinationSingleResponse> {
    return this.apiService.delete<StudentVaccinationSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
