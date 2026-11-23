import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/stage.model';
import {
  DailyAttendanceListResponse,
  DailyAttendanceSingleResponse,
  PeriodAttendanceListResponse,
  RecordPeriodAttendanceRequest,
  SetDailyAttendanceRequest
} from '../models/attendance.model';

/**
 * DailyAttendanceService
 * Talks to the Attendance service through the gateway (/api/daily-attendance)
 */
@Injectable({
  providedIn: 'root'
})
export class DailyAttendanceService {
  private readonly endpoint = '/api/daily-attendance';

  constructor(private apiService: ApiService) {}

  /** POST /api/daily-attendance/record-period (teacher accounts only) */
  recordPeriod(request: RecordPeriodAttendanceRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/record-period`, request);
  }

  /** POST /api/daily-attendance/set-day (insertedId: id of the day) */
  setDay(request: SetDailyAttendanceRequest): Observable<ApiResponse<null>> {
    return this.apiService.post<ApiResponse<null>>(`${this.endpoint}/set-day`, request);
  }

  /** PUT /api/daily-attendance/check-out/{id}?time=HH:mm */
  checkOut(id: string, time: string): Observable<ApiResponse<null>> {
    return this.apiService.put<ApiResponse<null>>(`${this.endpoint}/check-out/${id}?time=${encodeURIComponent(time)}`, {});
  }

  /** DELETE /api/daily-attendance/delete/{id} */
  delete(id: string): Observable<ApiResponse<null>> {
    return this.apiService.delete<ApiResponse<null>>(`${this.endpoint}/delete/${id}`);
  }

  /** GET /api/daily-attendance/get/{id} */
  getById(id: string): Observable<DailyAttendanceSingleResponse> {
    return this.apiService.get<DailyAttendanceSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/daily-attendance/get-by-section/{sectionId}?date=yyyy-MM-dd */
  getBySection(sectionId: string, date: string): Observable<DailyAttendanceListResponse> {
    return this.apiService.get<DailyAttendanceListResponse>(`${this.endpoint}/get-by-section/${sectionId}`, { date });
  }

  /** GET /api/daily-attendance/get-by-period/{timetableSlotId}?date=yyyy-MM-dd (teacher accounts only) */
  getByPeriod(timetableSlotId: string, date: string): Observable<PeriodAttendanceListResponse> {
    return this.apiService.get<PeriodAttendanceListResponse>(`${this.endpoint}/get-by-period/${timetableSlotId}`, { date });
  }

  /** GET /api/daily-attendance/get-by-student/{studentId}?from=yyyy-MM-dd&to=yyyy-MM-dd */
  getByStudent(studentId: string, from: string, to: string): Observable<DailyAttendanceListResponse> {
    return this.apiService.get<DailyAttendanceListResponse>(`${this.endpoint}/get-by-student/${studentId}`, { from, to });
  }

  /** GET /api/daily-attendance/my-child/{studentId}?from=&to= (parent accounts only) */
  getMyChild(studentId: string, from: string, to: string): Observable<DailyAttendanceListResponse> {
    return this.apiService.get<DailyAttendanceListResponse>(`${this.endpoint}/my-child/${studentId}`, { from, to });
  }
}
