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
