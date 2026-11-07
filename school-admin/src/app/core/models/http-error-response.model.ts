/**
 * HTTP Error Response Model
 * Represents standardized error responses from the API
 */
export interface HttpErrorResponse {
  statusCode: number;
  message: string;
  errors?: Record<string, string | string[]>;
  timestamp?: string;
  path?: string;
}
