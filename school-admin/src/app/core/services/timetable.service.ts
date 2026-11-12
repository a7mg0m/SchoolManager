import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  TimetableListResponse,
  TimetableSingleResponse,
  CreateTimetableRequest,
  UpdateTimetableRequest
} from '../models/timetable.model';

/**
 * Timetable Service
 * Handles all timetable-related API operations (timetable + its slots)
 */
@Injectable({
  providedIn: 'root'
})
export class TimetableService {
  private readonly timetableEndpoint = '/api/timetable';

  constructor(private apiService: ApiService) {}

  getAll(): Observable<TimetableListResponse> {
    return this.apiService.get<TimetableListResponse>(`${this.timetableEndpoint}/get-all`);
  }

  getById(id: string): Observable<TimetableSingleResponse> {
    return this.apiService.get<TimetableSingleResponse>(`${this.timetableEndpoint}/get/${id}`);
  }

  create(request: CreateTimetableRequest): Observable<TimetableSingleResponse> {
    return this.apiService.post<TimetableSingleResponse>(`${this.timetableEndpoint}/create`, request);
  }

  update(request: UpdateTimetableRequest): Observable<TimetableSingleResponse> {
    return this.apiService.put<TimetableSingleResponse>(`${this.timetableEndpoint}/update`, request);
  }

  delete(id: string): Observable<TimetableSingleResponse> {
    return this.apiService.delete<TimetableSingleResponse>(`${this.timetableEndpoint}/delete/${id}`);
  }
}
