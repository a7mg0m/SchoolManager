import { ApiResponse } from './stage.model';

/**
 * My Profile Model (Identity service)
 * Profile of the logged-in user: account (login, role) + user profile
 */
export interface MyProfile {
  /** UserProfile id, null when the user has no profile yet */
  id: string | null;
  userId: string;
  loginName: string;
  roleNameAr: string | null;
  roleNameEn: string | null;
  lastLogin: string | null;
  /** True when the user is an employee (email / phone / address are synced to the employee) */
  isEmployee: boolean;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  address: string | null;
  /** Image bytes as base64, null when there is no image */
  profileImage: string | null;
}

/**
 * Request model for updating my profile
 */
export interface UpdateMyProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  address: string | null;
  /** New image as base64 (no "data:" prefix); null keeps the current image */
  profileImage: string | null;
  /** True removes the current image */
  removeProfileImage: boolean;
}

/**
 * Request model for changing my password
 */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/**
 * Error keys returned by the API in `message` (translated under myProfile.errors.*)
 */
export const MY_PROFILE_ERROR_KEYS = [
  'UserNotAuthenticated',
  'UserNotFound',
  'FirstNameRequired',
  'LastNameRequired',
  'EmailRequired',
  'EmailInvalid',
  'EmailExists',
  'ProfileImageTooLarge',
  'CurrentPasswordRequired',
  'CurrentPasswordInvalid',
  'NewPasswordSameAsCurrent',
  'PasswordRequired',
  'PasswordTooShort'
] as const;

/**
 * Typed response for my profile
 */
export type MyProfileResponse = ApiResponse<MyProfile>;

/**
 * Typed response for change password
 */
export type ChangePasswordResponse = ApiResponse<null>;
