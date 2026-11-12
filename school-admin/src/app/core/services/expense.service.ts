import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  ExpenseListResponse,
  ExpenseSingleResponse,
  SaveExpenseRequest
} from '../models/expense.model';

/**
 * Expense Service
 * Talks to the Financial service through the gateway (/api/expense)
 */
@Injectable({
  providedIn: 'root'
})
export class ExpenseService {
  private readonly endpoint = '/api/expense';

  constructor(private apiService: ApiService) {}

  /** GET /api/expense/get-all */
  getAll(): Observable<ExpenseListResponse> {
    return this.apiService.get<ExpenseListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/expense/get/{id} */
  getById(id: string): Observable<ExpenseSingleResponse> {
    return this.apiService.get<ExpenseSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/expense/create */
  create(request: SaveExpenseRequest): Observable<ExpenseSingleResponse> {
    return this.apiService.post<ExpenseSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/expense/update/{id} */
  update(id: string, request: SaveExpenseRequest): Observable<ExpenseSingleResponse> {
    return this.apiService.put<ExpenseSingleResponse>(`${this.endpoint}/update/${id}`, request);
  }

  /** DELETE /api/expense/delete/{id} */
  delete(id: string): Observable<ExpenseSingleResponse> {
    return this.apiService.delete<ExpenseSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
