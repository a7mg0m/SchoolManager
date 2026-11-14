import { ApiResponse } from './stage.model';

/**
 * Employee Group Model (Identity service)
 * A group of employees, e.g. Administration, Accounting, Security
 */
export interface EmployeeGroup {
  id: string;
  nameAr: string;
  nameEn: string;
  description: string | null;
  isActive: boolean;
  /** Number of employees in the group (read only) */
  employeesCount: number;
}

/**
 * Request model for creating a new employee group
 */
export interface CreateEmployeeGroupRequest {
  nameAr: string;
  nameEn: string;
  description: string | null;
  isActive: boolean;
}

/**
 * Request model for updating an employee group
 * (active status is changed through activate / deactivate)
 */
export interface UpdateEmployeeGroupRequest {
  id: string;
  nameAr: string;
  nameEn: string;
  description: string | null;
}

/**
 * Error keys returned by the API in `message` (translated under employeeGroups.errors.*)
 */
export const EMPLOYEE_GROUP_ERROR_KEYS = [
  'EmployeeGroupNameExists',
  'EmployeeGroupNameRequired',
  'EmployeeGroupNotFound',
  'EmployeeGroupHasEmployees'
] as const;

/**
 * Typed response for employee group list
 */
export type EmployeeGroupListResponse = ApiResponse<EmployeeGroup[]>;

/**
 * Typed response for employee group single operation
 */
export type EmployeeGroupSingleResponse = ApiResponse<EmployeeGroup>;
