import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  RevenueCategoryListResponse,
  RevenueCategorySingleResponse,
  CreateRevenueCategoryRequest,
  UpdateRevenueCategoryRequest
} from '../models/revenue-category.model';

/**
 * Revenue Category Service
 * Talks to the Financial service through the gateway (/api/revenue-category)
 */
@Injectable({
  providedIn: 'root'
})
export class RevenueCategoryService {
  private readonly endpoint = '/api/revenue-category';

  constructor(private apiService: ApiService) {}

  /** GET /api/revenue-category/get-all */
  getAll(): Observable<RevenueCategoryListResponse> {
    return this.apiService.get<RevenueCategoryListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/revenue-category/get/{id} */
  getById(id: string): Observable<RevenueCategorySingleResponse> {
    return this.apiService.get<RevenueCategorySingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/revenue-category/create */
  create(request: CreateRevenueCategoryRequest): Observable<RevenueCategorySingleResponse> {
    return this.apiService.post<RevenueCategorySingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/revenue-category/update/{id} */
  update(id: string, request: UpdateRevenueCategoryRequest): Observable<RevenueCategorySingleResponse> {
    return this.apiService.put<RevenueCategorySingleResponse>(`${this.endpoint}/update/${id}`, request);
  }

  /** DELETE /api/revenue-category/delete/{id} */
  delete(id: string): Observable<RevenueCategorySingleResponse> {
    return this.apiService.delete<RevenueCategorySingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
