import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  CreateVaccineRequest,
  UpdateVaccineRequest,
  VaccineListResponse,
  VaccineSingleResponse
} from '../models/vaccine.model';

/**
 * VaccineService
 * Handles all vaccine API operations (school clinic)
 */
@Injectable({
  providedIn: 'root'
})
export class VaccineService {
  private readonly endpoint = '/api/vaccine';

  constructor(private apiService: ApiService) {}

  /** GET /api/vaccine/get-all */
  getAll(): Observable<VaccineListResponse> {
    return this.apiService.get<VaccineListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/vaccine/get-active */
  getActive(): Observable<VaccineListResponse> {
    return this.apiService.get<VaccineListResponse>(`${this.endpoint}/get-active`);
  }

  /** PUT /api/vaccine/activate/{id} */
  activate(id: string): Observable<VaccineSingleResponse> {
    return this.apiService.put<VaccineSingleResponse>(`${this.endpoint}/activate/${id}`, {});
  }

  /** PUT /api/vaccine/deactivate/{id} */
  deactivate(id: string): Observable<VaccineSingleResponse> {
    return this.apiService.put<VaccineSingleResponse>(`${this.endpoint}/deactivate/${id}`, {});
  }

  /** GET /api/vaccine/get/{id} */
  getById(id: string): Observable<VaccineSingleResponse> {
    return this.apiService.get<VaccineSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/vaccine/create */
  create(request: CreateVaccineRequest): Observable<VaccineSingleResponse> {
    return this.apiService.post<VaccineSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/vaccine/update */
  update(request: UpdateVaccineRequest): Observable<VaccineSingleResponse> {
    return this.apiService.put<VaccineSingleResponse>(`${this.endpoint}/update`, request);
  }

  /** DELETE /api/vaccine/delete/{id} */
  delete(id: string): Observable<VaccineSingleResponse> {
    return this.apiService.delete<VaccineSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
