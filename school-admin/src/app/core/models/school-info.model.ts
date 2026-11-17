/**
 * School Info Model
 * Represents school information and settings with bilingual support
 */
export interface SchoolInfo {
  id?: string;
  nameAr: string;
  nameEn: string;
  address?: string;
  phone?: string;
  email?: string;
  logo?: string | null;
  /** School album (returned by the API; managed with the album upload / delete endpoints) */
  album?: SchoolAlbumItem[];
}

/**
 * A file of the school album (backend SchoolAlbumItemDto).
 * url is a path on the server, e.g. "/upload/school-album/3f2a….jpg" (served through the gateway).
 */
export interface SchoolAlbumItem {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  /** Bytes */
  size: number;
  uploadedOn: string;
}

/**
 * Request model for creating a new school info
 */
export interface CreateSchoolInfoRequest {
  nameAr: string;
  nameEn: string;
  address?: string;
  phone?: string;
  email?: string;
  logo?: string | null;
}

/**
 * Request model for updating school info
 */
export interface UpdateSchoolInfoRequest {
  id: string;
  nameAr: string;
  nameEn: string;
  address?: string;
  phone?: string;
  email?: string;
  logo?: string | null;
}

/**
 * API Response wrapper returned by the backend
 */
export interface SchoolInfoApiResponse<T> {
  status: number;
  insertedId: string | null;
  message: string;
  data: T | null;
  errors: any;
}

/**
 * API Response for single school info
 */
export type SchoolInfoResponse = SchoolInfoApiResponse<SchoolInfo>;

/**
 * API Response for school info operations
 */
export type SchoolInfoSingleResponse = SchoolInfoApiResponse<SchoolInfo>;
