import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { DashboardSummaryResponse } from '../models/dashboard.model';

/**
 * Home page figures counted by the Attendance service (via gateway /api/attendance-dashboard):
 * the day's attendance and the transport (buses, riders, trips, drivers without a login).
 */
@Injectable({
  providedIn: 'root'
})
export class AttendanceDashboardService {
  private readonly endpoint = '/api/attendance-dashboard';

  constructor(private apiService: ApiService) {}

  /**
   * @param date - "yyyy-MM-dd" (the server uses today when omitted)
   * @param academicYearId - year of the attendance and bus riders; null = every year
   */
  getSummary(date?: string | null, academicYearId?: string | null): Observable<DashboardSummaryResponse> {
    const params = new URLSearchParams();
    if (date) params.set('date', date);
    if (academicYearId) params.set('academicYearId', academicYearId);
    const query = params.toString();
    return this.apiService.get<DashboardSummaryResponse>(`${this.endpoint}/summary${query ? '?' + query : ''}`);
  }
}
