import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  CreateClinicVisitRequest,
  UpdateClinicVisitRequest,
  ClinicVisitListResponse,
  ClinicVisitSingleResponse
} from '../models/clinic-visit.model';

/**
 * ClinicVisitService
 * Handles clinic visits (examination and diagnosis) with their medications
 */
@Injectable({
  providedIn: 'root'
})
export class ClinicVisitService {
  private readonly endpoint = '/api/clinic-visit';

  constructor(private apiService: ApiService) {}

  /** GET /api/clinic-visit/get-all */
  getAll(): Observable<ClinicVisitListResponse> {
    return this.apiService.get<ClinicVisitListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/clinic-visit/get/{id} */
  getById(id: string): Observable<ClinicVisitSingleResponse> {
    return this.apiService.get<ClinicVisitSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/clinic-visit/create */
  create(request: CreateClinicVisitRequest): Observable<ClinicVisitSingleResponse> {
    return this.apiService.post<ClinicVisitSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/clinic-visit/update */
  update(request: UpdateClinicVisitRequest): Observable<ClinicVisitSingleResponse> {
    return this.apiService.put<ClinicVisitSingleResponse>(`${this.endpoint}/update`, request);
  }

  /** GET /api/clinic-visit/get-by-student/{studentId} */
  getByStudent(studentId: string): Observable<ClinicVisitListResponse> {
    return this.apiService.get<ClinicVisitListResponse>(`${this.endpoint}/get-by-student/${studentId}`);
  }

  /** DELETE /api/clinic-visit/delete/{id} */
  delete(id: string): Observable<ClinicVisitSingleResponse> {
    return this.apiService.delete<ClinicVisitSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
