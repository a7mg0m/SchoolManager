import { ApiResponse } from './stage.model';

/**
 * Employee Model (Identity service)
 * Every employee has a login User (role Admin) and a UserProfile, kept in sync by the API.
 */
export interface Employee {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  nationalId: string;
  phoneNumber: string;
  email: string | null;
  address: string | null;
  jobTitle: string;
  /** "yyyy-MM-dd" */
  hireDate: string;
  employeeGroupId: string;
  employeeGroupNameAr: string | null;
  employeeGroupNameEn: string | null;
  userId: string | null;
  loginName: string | null;
  isActive: boolean;
}

/**
 * Request model for creating an employee (also creates its User and UserProfile)
 */
export interface CreateEmployeeRequest {
  fullNameAr: string;
  fullNameEn: string;
  nationalId: string;
  phoneNumber: string;
  email: string;
  address: string | null;
  jobTitle: string;
  /** "yyyy-MM-dd" */
  hireDate: string;
  employeeGroupId: string;
  loginName: string;
  password: string;
  isActive: boolean;
}

/**
 * Request model for updating an employee (also updates its User and UserProfile).
 * Leave password null/empty to keep the current one.
 */
export interface UpdateEmployeeRequest {
  id: string;
  fullNameAr: string;
  fullNameEn: string;
  nationalId: string;
  phoneNumber: string;
  email: string;
  address: string | null;
  jobTitle: string;
  hireDate: string;
  employeeGroupId: string;
  loginName: string;
  password: string | null;
}

/**
 * Error keys returned by the API in `message` (translated under employees.errors.*)
 */
export const EMPLOYEE_ERROR_KEYS = [
  'EmployeeNotFound',
  'EmployeeGroupNotFound',
  'EmployeeGroupInactive',
  'EmployeeGroupRequired',
  'NationalIdExists',
  'NationalIdRequired',
  'LoginNameExists',
  'LoginNameRequired',
  'LoginNameTooShort',
  'EmailExists',
  'EmailRequired',
  'EmailInvalid',
  'PasswordRequired',
  'FullNameRequired',
  'PhoneNumberRequired',
  'JobTitleRequired',
  'HireDateRequired',
  'AdminRoleNotFound'
] as const;

/**
 * Typed response for employee list
 */
export type EmployeeListResponse = ApiResponse<Employee[]>;

/**
 * Typed response for employee single operation
 */
export type EmployeeSingleResponse = ApiResponse<Employee>;
