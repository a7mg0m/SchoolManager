import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

/**
 * API Service
 * Centralized HTTP communication layer
 * Handles GET, POST, PUT, PATCH, DELETE requests with consistent configuration
 * Uses environment.gatewayUrl as base URL
 */
@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.gatewayUrl;

  constructor(private http: HttpClient) {}

  /**
   * Perform GET request
   * @param endpoint - API endpoint path (e.g., '/api/users')
   * @param params - Optional query parameters
   * @param options - Optional additional HTTP options
   */
  get<T>(
    endpoint: string,
    params?: HttpParams | Record<string, string | string[]>,
    options?: { headers?: HttpHeaders; skipErrorHandler?: boolean }
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    let httpParams: HttpParams | undefined;

    if (params instanceof HttpParams) {
      httpParams = params;
    } else if (params) {
      httpParams = this.buildHttpParams(params);
    }

    return this.http.get<T>(url, {
      params: httpParams,
      headers: options?.headers,
      withCredentials: false
    });
  }

  /**
   * Perform POST request
   * @param endpoint - API endpoint path
   * @param body - Request body
   * @param options - Optional additional HTTP options
   */
  post<T, B = any>(
    endpoint: string,
    body: B,
    options?: { headers?: HttpHeaders; skipErrorHandler?: boolean }
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    return this.http.post<T>(url, body, {
      headers: options?.headers,
      withCredentials: false
    });
  }

  /**
   * Perform PUT request
   * @param endpoint - API endpoint path
   * @param body - Request body
   * @param options - Optional additional HTTP options
   */
  put<T, B = any>(
    endpoint: string,
    body: B,
    options?: { headers?: HttpHeaders; skipErrorHandler?: boolean }
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    return this.http.put<T>(url, body, {
      headers: options?.headers,
      withCredentials: false
    });
  }

  /**
   * Perform PATCH request
   * @param endpoint - API endpoint path
   * @param body - Request body
   * @param options - Optional additional HTTP options
   */
  patch<T, B = any>(
    endpoint: string,
    body: B,
    options?: { headers?: HttpHeaders; skipErrorHandler?: boolean }
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    return this.http.patch<T>(url, body, {
      headers: options?.headers,
      withCredentials: false
    });
  }

  /**
   * Perform DELETE request
   * @param endpoint - API endpoint path
   * @param options - Optional additional HTTP options
   */
  delete<T>(
    endpoint: string,
    options?: { headers?: HttpHeaders; skipErrorHandler?: boolean }
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    return this.http.delete<T>(url, {
      headers: options?.headers,
      withCredentials: false
    });
  }

  /**
   * Build full URL from endpoint
   * @param endpoint - API endpoint path
   */
  private buildUrl(endpoint: string): string {
    // Remove leading slash if present to avoid double slashes
    const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    return `${this.baseUrl}${cleanEndpoint}`;
  }

  /**
   * Build HttpParams from object
   * @param params - Parameter object
   */
  private buildHttpParams(params: Record<string, string | string[]>): HttpParams {
    let httpParams = new HttpParams();
    Object.keys(params).forEach(key => {
      const value = params[key];
      if (Array.isArray(value)) {
        value.forEach(v => {
          httpParams = httpParams.append(key, v);
        });
      } else {
        httpParams = httpParams.set(key, value);
      }
    });
    return httpParams;
  }
}
