import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { ApiService } from './api.service';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SchoolInfo,
  SchoolInfoResponse,
  SchoolInfoSingleResponse,
  SchoolInfoApiResponse,
  SchoolAlbumItem,
  CreateSchoolInfoRequest,
  UpdateSchoolInfoRequest
} from '../models/school-info.model';

/**
 * School Info Service
 * Handles all school information-related API operations
 */
@Injectable({
  providedIn: 'root'
})
export class SchoolInfoService {
  private readonly schoolInfoEndpoint = '/api/school-info';

  /** School info shown in the layout (sidebar name); null until loaded or when none is set up */
  readonly current = signal<SchoolInfo | null>(null);
  /** <img> src of the school's logo, or null when it has none */
  readonly currentLogoSrc = computed(() => SchoolInfoService.toImageSrc(this.current()?.logo));
  private currentRequested = false;

  /** Convert a base64 logo from the API (or a data URL) into an <img> src */
  static toImageSrc(logo: string | null | undefined): string | null {
    if (!logo) {
      return null;
    }
    if (logo.startsWith('data:')) {
      return logo;
    }
    // Detect the image type from the base64 signature
    const mime =
      logo.startsWith('/9j/') ? 'image/jpeg' :
      logo.startsWith('R0lGOD') ? 'image/gif' :
      logo.startsWith('UklGR') ? 'image/webp' :
      logo.startsWith('PHN2Zy') || logo.startsWith('PD94bWw') ? 'image/svg+xml' :
      'image/png';
    return `data:${mime};base64,${logo}`;
  }

  constructor(private apiService: ApiService, private http: HttpClient) {}

  /**
   * Upload one image to the school album (multipart, field "files"). Emits HTTP events so the page can
   * show the upload progress; the last event's body has the new album items.
   */
  uploadAlbumFile(file: File): Observable<HttpEvent<SchoolInfoApiResponse<SchoolAlbumItem[]>>> {
    const form = new FormData();
    form.append('files', file, file.name);
    return this.http.post<SchoolInfoApiResponse<SchoolAlbumItem[]>>(
      `${environment.gatewayUrl}${this.schoolInfoEndpoint}/album/upload`,
      form,
      { reportProgress: true, observe: 'events' }
    );
  }

  /** Remove an image from the school album (the API also deletes the file) */
  deleteAlbumItem(id: string): Observable<SchoolInfoApiResponse<null>> {
    return this.apiService.delete<SchoolInfoApiResponse<null>>(`${this.schoolInfoEndpoint}/album/delete/${id}`);
  }

  /** Full address of an album file: "/upload/school-album/x.jpg" -> "http://gateway/upload/school-album/x.jpg" */
  static albumFileUrl(url: string): string {
    if (/^https?:\/\//i.test(url)) {
      return url;
    }
    const base = environment.gatewayUrl.replace(/\/+$/, '');
    return `${base}/${url.replace(/^\/+/, '')}`;
  }

  /** Load the school info for the layout once (call again with force after it may have changed) */
  loadCurrent(force = false): void {
    if (this.currentRequested && !force) {
      return;
    }
    this.currentRequested = true;
    this.get().subscribe({
      next: (response) => this.current.set(response?.status === 200 ? response.data ?? null : null),
      error: () => {
        // Not set up yet / service down: the layout falls back to the app name; retry on next call
        this.currentRequested = false;
      }
    });
  }

  /** Publish saved school info to the layout without a new request */
  setCurrent(info: SchoolInfo | null): void {
    this.current.set(info);
  }

  /**
   * Get school information
   * @returns Observable of school info response
   */
  get(): Observable<SchoolInfoSingleResponse> {
    return this.apiService.get<SchoolInfoSingleResponse>(
      `${this.schoolInfoEndpoint}/get`
    );
  }

  /**
   * Create new school information
   * @param request - The school info data to create
   * @returns Observable of school info response
   */
  create(request: CreateSchoolInfoRequest): Observable<SchoolInfoSingleResponse> {
    return this.apiService.post<SchoolInfoSingleResponse>(
      `${this.schoolInfoEndpoint}/create`,
      request
    );
  }

  /**
   * Update existing school information
   * @param request - The school info data to update
   * @returns Observable of school info response
   */
  update(request: UpdateSchoolInfoRequest): Observable<SchoolInfoSingleResponse> {
    return this.apiService.put<SchoolInfoSingleResponse>(
      `${this.schoolInfoEndpoint}/update`,
      request
    );
  }
}
