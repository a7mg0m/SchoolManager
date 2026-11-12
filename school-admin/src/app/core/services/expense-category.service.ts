import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  ExpenseCategoryListResponse,
  ExpenseCategorySingleResponse,
  CreateExpenseCategoryRequest,
  UpdateExpenseCategoryRequest
} from '../models/expense-category.model';

/**
 * Expense Category Service
 * Talks to the Financial service through the gateway (/api/expense-category)
 */
@Injectable({
  providedIn: 'root'
})
export class ExpenseCategoryService {
  private readonly endpoint = '/api/expense-category';

  constructor(private apiService: ApiService) {}

  /** GET /api/expense-category/get-all */
  getAll(): Observable<ExpenseCategoryListResponse> {
    return this.apiService.get<ExpenseCategoryListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/expense-category/get/{id} */
  getById(id: string): Observable<ExpenseCategorySingleResponse> {
    return this.apiService.get<ExpenseCategorySingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/expense-category/create */
  create(request: CreateExpenseCategoryRequest): Observable<ExpenseCategorySingleResponse> {
    return this.apiService.post<ExpenseCategorySingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/expense-category/update/{id} */
  update(id: string, request: UpdateExpenseCategoryRequest): Observable<ExpenseCategorySingleResponse> {
    return this.apiService.put<ExpenseCategorySingleResponse>(`${this.endpoint}/update/${id}`, request);
  }

  /** DELETE /api/expense-category/delete/{id} */
  delete(id: string): Observable<ExpenseCategorySingleResponse> {
    return this.apiService.delete<ExpenseCategorySingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
