import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  CreateVaccinationCampaignRequest,
  UpdateVaccinationCampaignRequest,
  VaccinationCampaignListResponse,
  VaccinationCampaignSingleResponse
} from '../models/vaccination-campaign.model';

/**
 * VaccinationCampaignService
 * Handles all vaccination campaign API operations (school clinic)
 */
@Injectable({
  providedIn: 'root'
})
export class VaccinationCampaignService {
  private readonly endpoint = '/api/vaccination-campaign';

  constructor(private apiService: ApiService) {}

  /** GET /api/vaccination-campaign/get-all */
  getAll(): Observable<VaccinationCampaignListResponse> {
    return this.apiService.get<VaccinationCampaignListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/vaccination-campaign/get-active */
  getActive(): Observable<VaccinationCampaignListResponse> {
    return this.apiService.get<VaccinationCampaignListResponse>(`${this.endpoint}/get-active`);
  }

  /** PUT /api/vaccination-campaign/activate/{id} */
  activate(id: string): Observable<VaccinationCampaignSingleResponse> {
    return this.apiService.put<VaccinationCampaignSingleResponse>(`${this.endpoint}/activate/${id}`, {});
  }

  /** PUT /api/vaccination-campaign/deactivate/{id} */
  deactivate(id: string): Observable<VaccinationCampaignSingleResponse> {
    return this.apiService.put<VaccinationCampaignSingleResponse>(`${this.endpoint}/deactivate/${id}`, {});
  }

  /** GET /api/vaccination-campaign/get/{id} */
  getById(id: string): Observable<VaccinationCampaignSingleResponse> {
    return this.apiService.get<VaccinationCampaignSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/vaccination-campaign/create */
  create(request: CreateVaccinationCampaignRequest): Observable<VaccinationCampaignSingleResponse> {
    return this.apiService.post<VaccinationCampaignSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/vaccination-campaign/update */
  update(request: UpdateVaccinationCampaignRequest): Observable<VaccinationCampaignSingleResponse> {
    return this.apiService.put<VaccinationCampaignSingleResponse>(`${this.endpoint}/update`, request);
  }

  /** DELETE /api/vaccination-campaign/delete/{id} */
  delete(id: string): Observable<VaccinationCampaignSingleResponse> {
    return this.apiService.delete<VaccinationCampaignSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
