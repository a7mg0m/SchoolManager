import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  StudentFeeListResponse,
  StudentFeeSingleResponse,
  ApplyStudentFeePaymentRequest,
  AssignStudentFeesRequest,
  AssignStudentFeesResponse,
  WaiveStudentFeeRequest
} from '../models/student-fee.model';

/**
 * Student Fee Service
 * Talks to the Financial service through the gateway (/api/student-fee)
 */
@Injectable({
  providedIn: 'root'
})
export class StudentFeeService {
  private readonly endpoint = '/api/student-fee';

  constructor(private apiService: ApiService) {}

  /** GET /api/student-fee/get-all */
  getAll(): Observable<StudentFeeListResponse> {
    return this.apiService.get<StudentFeeListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/student-fee/get/{id} */
  getById(id: string): Observable<StudentFeeSingleResponse> {
    return this.apiService.get<StudentFeeSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/student-fee/get-by-student/{studentId} */
  getByStudent(studentId: string): Observable<StudentFeeListResponse> {
    return this.apiService.get<StudentFeeListResponse>(`${this.endpoint}/get-by-student/${studentId}`);
  }

  /** POST /api/student-fee/assign – assign fees to one or more students */
  assign(request: AssignStudentFeesRequest): Observable<AssignStudentFeesResponse> {
    return this.apiService.post<AssignStudentFeesResponse>(`${this.endpoint}/assign`, request);
  }

  /** PUT /api/student-fee/apply-payment/{id} */
  applyPayment(id: string, request: ApplyStudentFeePaymentRequest): Observable<StudentFeeSingleResponse> {
    return this.apiService.put<StudentFeeSingleResponse>(`${this.endpoint}/apply-payment/${id}`, request);
  }

  /** PUT /api/student-fee/waive/{id} – forgive the remaining balance */
  waive(id: string, request: WaiveStudentFeeRequest): Observable<StudentFeeSingleResponse> {
    return this.apiService.put<StudentFeeSingleResponse>(`${this.endpoint}/waive/${id}`, request);
  }

  /** PUT /api/student-fee/restore-waiver/{id} – undo a waiver */
  restoreWaiver(id: string): Observable<StudentFeeSingleResponse> {
    return this.apiService.put<StudentFeeSingleResponse>(`${this.endpoint}/restore-waiver/${id}`, {});
  }

  /** DELETE /api/student-fee/delete/{id} */
  delete(id: string): Observable<StudentFeeSingleResponse> {
    return this.apiService.delete<StudentFeeSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}
