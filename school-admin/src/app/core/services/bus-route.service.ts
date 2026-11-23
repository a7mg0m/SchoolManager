import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import {
  BusRouteListResponse,
  BusRouteSingleResponse,
  CreateBusRouteRequest,
  UpdateBusRouteRequest
} from '../models/transport.model';

/**
 * BusRouteService
 * Talks to the Attendance service through the gateway (/api/bus-route)
 */
@Injectable({
  providedIn: 'root'
})
export class BusRouteService {
  private readonly endpoint = '/api/bus-route';

  constructor(private apiService: ApiService) {}

  /** GET /api/bus-route/get-all */
  getAll(): Observable<BusRouteListResponse> {
    return this.apiService.get<BusRouteListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/bus-route/get-active */
  getActive(): Observable<BusRouteListResponse> {
    return this.apiService.get<BusRouteListResponse>(`${this.endpoint}/get-active`);
  }

  /** GET /api/bus-route/get/{id} */
  getById(id: string): Observable<BusRouteSingleResponse> {
    return this.apiService.get<BusRouteSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/bus-route/create */
  create(request: CreateBusRouteRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/bus-route/update (the stops sent replace the saved ones) */
  update(request: UpdateBusRouteRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/update`, request);
  }

  /** PUT /api/bus-route/activate/{id} */
  activate(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/activate/${id}`, {});
  }

  /** PUT /api/bus-route/deactivate/{id} */
  deactivate(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/deactivate/${id}`, {});
  }

  /** DELETE /api/bus-route/delete/{id} (not while buses use the route) */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.apiService.delete<ApiResponse<null>>(`${this.endpoint}/delete/${id}`);
  }
}
