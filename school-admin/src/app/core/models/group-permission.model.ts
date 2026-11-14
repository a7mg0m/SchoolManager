import { ApiResponse } from './stage.model';

/**
 * Permission flags on one menu item. Add / Edit / Delete imply View.
 */
export interface PermissionFlags {
  canView: boolean;
  canAdd: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export type PermissionKey = keyof PermissionFlags;

export const PERMISSION_KEYS: PermissionKey[] = ['canView', 'canAdd', 'canEdit', 'canDelete'];

/**
 * Menu item with permission flags and children (Identity service)
 */
export interface PermissionMenuItem extends PermissionFlags {
  menuItemId: string;
  nameAr: string;
  nameEn: string;
  icon: string | null;
  route: string | null;
  order: number;
  isActive: boolean;
  children: PermissionMenuItem[];
}

/**
 * The whole menu with the permissions of one employee group
 */
export interface GroupPermissions {
  employeeGroupId: string;
  employeeGroupNameAr: string;
  employeeGroupNameEn: string;
  menuItems: PermissionMenuItem[];
}

/**
 * Permission of a group on one menu item (save request)
 */
export interface GroupPermissionItem extends PermissionFlags {
  menuItemId: string;
}

/**
 * Request: full set of permissions of a group (items left out lose access)
 */
export interface SaveGroupPermissionsRequest {
  employeeGroupId: string;
  permissions: GroupPermissionItem[];
}

export interface SaveGroupPermissionsSummary {
  created: number;
  updated: number;
  removed: number;
}

/**
 * Menu of the logged-in user with their permissions
 */
export interface MyMenu {
  /** SystemAdmin / Admin that is not an employee: whole menu, every permission */
  isFullAccess: boolean;
  employeeGroupId: string | null;
  menuItems: PermissionMenuItem[];
}

/**
 * Error keys returned by the API in `message` (translated under groupPermissions.errors.*)
 */
export const GROUP_PERMISSION_ERROR_KEYS = [
  'EmployeeGroupNotFound',
  'EmployeeGroupRequired',
  'GroupPermissionMenuItemRequired',
  'GroupPermissionDuplicateMenuItem'
] as const;

export type GroupPermissionsResponse = ApiResponse<GroupPermissions>;
export type SaveGroupPermissionsResponse = ApiResponse<SaveGroupPermissionsSummary>;
export type MyMenuResponse = ApiResponse<MyMenu>;
