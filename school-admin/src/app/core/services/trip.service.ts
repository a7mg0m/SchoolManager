import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import {
  CreateTripRequest,
  MarkTripStudentRequest,
  TripListResponse,
  TripSingleResponse,
  TripTrackingResponse,
  UpdateTripLocationRequest
} from '../models/transport.model';

/**
 * TripService
 * Talks to the Attendance service through the gateway (/api/trip)
 */
@Injectable({
  providedIn: 'root'
})
export class TripService {
  private readonly endpoint = '/api/trip';

  constructor(private apiService: ApiService) {}

  /** POST /api/trip/create */
  create(request: CreateTripRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/trip/start/{id} */
  start(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/start/${id}`, {});
  }

  /** PUT /api/trip/complete/{id} */
  complete(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/complete/${id}`, {});
  }

  /** PUT /api/trip/cancel/{id} */
  cancel(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/cancel/${id}`, {});
  }

  /** PUT /api/trip/location/{id} (driver) */
  updateLocation(id: string, request: UpdateTripLocationRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/location/${id}`, request);
  }

  /** PUT /api/trip/mark-student/{tripId} */
  markStudent(tripId: string, request: MarkTripStudentRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/mark-student/${tripId}`, request);
  }

  /** GET /api/trip/get/{id} */
  /** Transport report: the trips of a period (yyyy-MM-dd, at most a year), of one bus when given */
  getReport(from: string, to: string, busId?: string | null): Observable<TripListResponse> {
    const bus = busId ? `&busId=${busId}` : '';
    return this.apiService.get<TripListResponse>(`${this.endpoint}/report?from=${from}&to=${to}${bus}`);
  }

  getById(id: string): Observable<TripSingleResponse> {
    return this.apiService.get<TripSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/trip/get-by-date?date=yyyy-MM-dd&busId= */
  getByDate(date: string, busId?: string | null): Observable<TripListResponse> {
    const params: Record<string, string> = { date };
    if (busId) params['busId'] = busId;
    return this.apiService.get<TripListResponse>(`${this.endpoint}/get-by-date`, params);
  }

  /** GET /api/trip/my-child/{studentId} (parent accounts only) */
  getMyChildCurrentTrip(studentId: string): Observable<TripTrackingResponse> {
    return this.apiService.get<TripTrackingResponse>(`${this.endpoint}/my-child/${studentId}`);
  }
}
