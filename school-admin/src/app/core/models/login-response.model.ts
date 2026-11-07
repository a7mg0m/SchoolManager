/**
 * Login Data Model
 * Represents the user data from the API login response
 */
export interface LoginData {
  userId: string;
  loginName: string;
  token: string;
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
