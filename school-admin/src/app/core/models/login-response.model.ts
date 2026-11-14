/**
 * User Profile Model
 * Profile of the logged-in user, returned by the login API
 */
export interface UserProfile {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  address: string | null;
  /** Image bytes as base64 (byte[] in the API); null or empty when there is no image */
  profileImage: string | null;
}

/**
 * Login Data Model
 * Represents the user data from the API login response
 */
export interface LoginData {
  userId: string;
  loginName: string;
  token: string;
  /** null when the user has no profile */
  userProfile: UserProfile | null;
}

/**
 * Info about the logged-in user kept by AuthService (everything from the login except the token)
 */
export interface UserInfo {
  userId: string;
  loginName: string;
  userProfile: UserProfile | null;
}

/**
 * Login Response Model
 * Represents the authentication response from the API
 */
export interface LoginResponse {
  status: number;
  insertedId: string | null;
  message: string;
  data: LoginData | null;
  errors: any | null;
}
