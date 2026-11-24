import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import { BusListResponse, BusSingleResponse, CreateBusRequest, UpdateBusRequest } from '../models/transport.model';

/**
 * BusService
 * Talks to the Attendance service through the gateway (/api/bus)
 */
@Injectable({
  providedIn: 'root'
})
export class BusService {
  private readonly endpoint = '/api/bus';

  constructor(private apiService: ApiService) {}

  /**
   * Is the login name free? (Identity service: no employee, teacher, parent or other driver uses it)
   * @param busId - The bus being edited (its driver's own login name is allowed; the driver's user id is the bus id)
   */
  isLoginNameAvailable(loginName: string, busId?: string | null): Observable<ApiResponse<boolean>> {
    const params = new URLSearchParams({ loginName });
    if (busId) {
      params.set('userId', busId);
    }
    return this.apiService.get<ApiResponse<boolean>>(`/api/user/login-name-available?${params.toString()}`);
  }

  /** GET /api/bus/get-all */
  getAll(): Observable<BusListResponse> {
    return this.apiService.get<BusListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/bus/get-active */
  getActive(): Observable<BusListResponse> {
    return this.apiService.get<BusListResponse>(`${this.endpoint}/get-active`);
  }

  /** GET /api/bus/get/{id} */
  getById(id: string): Observable<BusSingleResponse> {
    return this.apiService.get<BusSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/bus/get-by-route/{busRouteId} */
  getByRoute(busRouteId: string): Observable<BusListResponse> {
    return this.apiService.get<BusListResponse>(`${this.endpoint}/get-by-route/${busRouteId}`);
  }

  /** POST /api/bus/create */
  create(request: CreateBusRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/bus/update */
  update(request: UpdateBusRequest): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/update`, request);
  }

  /** PUT /api/bus/activate/{id} */
  activate(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/activate/${id}`, {});
  }

  /** PUT /api/bus/deactivate/{id} */
  deactivate(id: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/deactivate/${id}`, {});
  }

  /** DELETE /api/bus/delete/{id} (not while the bus has active assignments) */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.apiService.delete<ApiResponse<null>>(`${this.endpoint}/delete/${id}`);
  }
}
