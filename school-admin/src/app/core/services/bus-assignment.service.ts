import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import {
  AssignStudentsToBusRequest,
  AssignStudentsToBusResponse,
  BusAssignmentListResponse,
  BusAssignmentSingleResponse,
  ChangeBusFeeRequest,
  EndBusAssignmentRequest,
  MoveStudentToBusRequest
} from '../models/transport.model';

/**
 * BusAssignmentService
 * Talks to the Attendance service through the gateway (/api/bus-assignment).
 * Each assignment carries its own bus fee, sent to Financial as a Transportation student fee.
 */
@Injectable({
  providedIn: 'root'
})
export class BusAssignmentService {
  private readonly endpoint = '/api/bus-assignment';

  constructor(private apiService: ApiService) {}

  /** POST /api/bus-assignment/assign */
  assign(request: AssignStudentsToBusRequest): Observable<AssignStudentsToBusResponse> {
    return this.apiService.post<AssignStudentsToBusResponse>(`${this.endpoint}/assign`, request);
  }

  /** PUT /api/bus-assignment/change-fee/{id} */
  changeFee(id: string, request: ChangeBusFeeRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/change-fee/${id}`, request);
  }

  /** PUT /api/bus-assignment/move/{id} */
  move(id: string, request: MoveStudentToBusRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/move/${id}`, request);
  }

  /** PUT /api/bus-assignment/end/{id} */
  end(id: string, request: EndBusAssignmentRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/end/${id}`, request);
  }

  /** GET /api/bus-assignment/get/{id} */
  getById(id: string): Observable<BusAssignmentSingleResponse> {
    return this.apiService.get<BusAssignmentSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/bus-assignment/get-by-bus/{busId}?academicYearId= */
  getByBus(busId: string, academicYearId?: string | null): Observable<BusAssignmentListResponse> {
    return this.apiService.get<BusAssignmentListResponse>(
      `${this.endpoint}/get-by-bus/${busId}`, academicYearId ? { academicYearId } : undefined);
  }

  /** GET /api/bus-assignment/get-by-student/{studentId} */
  getByStudent(studentId: string): Observable<BusAssignmentListResponse> {
    return this.apiService.get<BusAssignmentListResponse>(`${this.endpoint}/get-by-student/${studentId}`);
  }
}
