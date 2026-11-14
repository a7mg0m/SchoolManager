import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  MyProfileResponse,
  UpdateMyProfileRequest,
  ChangePasswordRequest,
  ChangePasswordResponse
} from '../models/my-profile.model';

/**
 * My Profile Service
 * Profile and password of the logged-in user (the API reads the user from the JWT)
 */
@Injectable({
  providedIn: 'root'
})
export class MyProfileService {
  private readonly profileEndpoint = '/api/user-profile';
  private readonly userEndpoint = '/api/user';

  constructor(private apiService: ApiService) {}

  /**
   * Get the logged-in user's profile
   */
  getMyProfile(): Observable<MyProfileResponse> {
    return this.apiService.get<MyProfileResponse>(`${this.profileEndpoint}/my-profile`);
  }

  /**
   * Update the logged-in user's profile (returns the updated profile)
   * @param request - The profile data to save
   */
  updateMyProfile(request: UpdateMyProfileRequest): Observable<MyProfileResponse> {
    return this.apiService.put<MyProfileResponse>(`${this.profileEndpoint}/update-my-profile`, request);
  }

  /**
   * Change the logged-in user's password
   * @param request - Current and new password
   */
  changePassword(request: ChangePasswordRequest): Observable<ChangePasswordResponse> {
    return this.apiService.put<ChangePasswordResponse>(`${this.userEndpoint}/change-password`, request);
  }
}
