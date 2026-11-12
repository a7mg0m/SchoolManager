import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  FeeListResponse,
  FeeSingleResponse,
  CreateFeeRequest,
  UpdateFeeRequest
} from '../models/fee.model';

/**
 * Fee Service
 * Talks to the Financial service through the gateway (/api/fee)
 */
@Injectable({
  providedIn: 'root'
})
export class FeeService {
  private readonly endpoint = '/api/fee';

  constructor(private apiService: ApiService) {}

  /** GET /api/fee/get-all */
  getAll(): Observable<FeeListResponse> {
    return this.apiService.get<FeeListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/fee/get/{id} */
  getById(id: string): Observable<FeeSingleResponse> {
    return this.apiService.get<FeeSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/fee/create */
  create(request: CreateFeeRequest): Observable<FeeSingleResponse> {
    return this.apiService.post<FeeSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/fee/update/{id} */
  update(id: string, request: UpdateFeeRequest): Observable<FeeSingleResponse> {
    return this.apiService.put<FeeSingleResponse>(`${this.endpoint}/update/${id}`, request);
  }

  /** DELETE /api/fee/delete/{id} */
  delete(id: string): Observable<FeeSingleResponse> {
    return this.apiService.delete<FeeSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
