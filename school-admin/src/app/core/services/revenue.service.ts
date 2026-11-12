import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  RevenueListResponse,
  RevenueSingleResponse,
  SaveRevenueRequest
} from '../models/revenue.model';

/**
 * Revenue Service
 * Talks to the Financial service through the gateway (/api/revenue)
 */
@Injectable({
  providedIn: 'root'
})
export class RevenueService {
  private readonly endpoint = '/api/revenue';

  constructor(private apiService: ApiService) {}

  /** GET /api/revenue/get-all */
  getAll(): Observable<RevenueListResponse> {
    return this.apiService.get<RevenueListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/revenue/get/{id} */
  getById(id: string): Observable<RevenueSingleResponse> {
    return this.apiService.get<RevenueSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/revenue/create */
  create(request: SaveRevenueRequest): Observable<RevenueSingleResponse> {
    return this.apiService.post<RevenueSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/revenue/update/{id} */
  update(id: string, request: SaveRevenueRequest): Observable<RevenueSingleResponse> {
    return this.apiService.put<RevenueSingleResponse>(`${this.endpoint}/update/${id}`, request);
  }

  /** DELETE /api/revenue/delete/{id} */
  delete(id: string): Observable<RevenueSingleResponse> {
    return this.apiService.delete<RevenueSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
