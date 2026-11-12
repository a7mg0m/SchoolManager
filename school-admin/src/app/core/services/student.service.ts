import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { StudentListResponse, StudentSingleResponse } from '../models/student.model';

/**
 * Student Service (read-only lookups)
 * Students are created/edited through their parent (ParentService).
 */
@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private readonly endpoint = '/api/student';

  constructor(private apiService: ApiService) {}

  /** GET /api/student/get-all */
  getAll(): Observable<StudentListResponse> {
    return this.apiService.get<StudentListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/student/get/{id} */
  getById(id: string): Observable<StudentSingleResponse> {
    return this.apiService.get<StudentSingleResponse>(`${this.endpoint}/get/${id}`);
  }
}
