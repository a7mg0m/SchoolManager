import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import {
  GroupPermissionsResponse,
  SaveGroupPermissionsRequest,
  SaveGroupPermissionsResponse,
  MyMenuResponse
} from '../models/group-permission.model';

/**
 * Group Permission Service
 * Permissions of employee groups on menu items (Identity service)
 */
@Injectable({
  providedIn: 'root'
})
export class GroupPermissionService {
  private readonly endpoint = '/api/group-permission';

  constructor(private apiService: ApiService) {}

  /**
   * Whole menu tree with the permissions of one employee group
   * @param groupId - Employee group ID
   */
  getByGroup(groupId: string): Observable<GroupPermissionsResponse> {
    return this.apiService.get<GroupPermissionsResponse>(`${this.endpoint}/get-by-group/${groupId}`);
  }

  /**
   * Replace all permissions of a group (items left out lose access)
   * @param request - Group and its permissions
   */
  save(request: SaveGroupPermissionsRequest): Observable<SaveGroupPermissionsResponse> {
    return this.apiService.put<SaveGroupPermissionsResponse>(`${this.endpoint}/save`, request);
  }

  /**
   * Menu of the logged-in user with their permissions
   */
  getMyMenu(): Observable<MyMenuResponse> {
    return this.apiService.get<MyMenuResponse>(`${this.endpoint}/my-menu`);
  }
}
