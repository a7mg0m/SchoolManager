import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  CreateNotificationRequest,
  NotificationListResponse,
  NotificationSingleResponse,
  NotificationTargetListResponse,
  NotificationTargetSingleResponse,
  UpdateNotificationRequest
} from '../models/notification.model';

/**
 * Notification Service
 * Talks to the Notification service through the gateway (/api/notification).
 * Validation failures come back as HTTP 400 ProblemDetails (see extractApiErrors in school-event.service).
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly endpoint = '/api/notification';

  constructor(private apiService: ApiService) {}

  /** GET /api/notification/get-all (newest first) */
  getAll(): Observable<NotificationListResponse> {
    return this.apiService.get<NotificationListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/notification/get/{id} */
  getById(id: string): Observable<NotificationSingleResponse> {
    return this.apiService.get<NotificationSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** POST /api/notification/create */
  create(request: CreateNotificationRequest): Observable<NotificationSingleResponse> {
    return this.apiService.post<NotificationSingleResponse>(`${this.endpoint}/create`, request);
  }

  /** PUT /api/notification/update */
  update(request: UpdateNotificationRequest): Observable<NotificationSingleResponse> {
    return this.apiService.put<NotificationSingleResponse>(`${this.endpoint}/update`, request);
  }

  /** DELETE /api/notification/delete/{id} */
  delete(id: string): Observable<NotificationSingleResponse> {
    return this.apiService.delete<NotificationSingleResponse>(`${this.endpoint}/delete/${id}`);
  }
}

/**
 * Notification Target Service (read-only; targets are saved with their notification)
 * /api/notification-target
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationTargetService {
  private readonly endpoint = '/api/notification-target';

  constructor(private apiService: ApiService) {}

  /** GET /api/notification-target/get-all */
  getAll(): Observable<NotificationTargetListResponse> {
    return this.apiService.get<NotificationTargetListResponse>(`${this.endpoint}/get-all`);
  }

  /** GET /api/notification-target/get/{id} */
  getById(id: string): Observable<NotificationTargetSingleResponse> {
    return this.apiService.get<NotificationTargetSingleResponse>(`${this.endpoint}/get/${id}`);
  }

  /** GET /api/notification-target/get-by-notification/{notificationId} */
  getByNotification(notificationId: string): Observable<NotificationTargetListResponse> {
    return this.apiService.get<NotificationTargetListResponse>(`${this.endpoint}/get-by-notification/${notificationId}`);
  }
}
