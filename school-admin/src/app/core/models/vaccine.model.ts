import { ApiResponse } from './stage.model';

/**
 * Vaccine Model (School clinic)
 */
export interface Vaccine {
  id: string;
  nameAr: string;
  nameEn: string;
  /** How many doses make the full vaccine (1 or more) */
  totalDoses: number;
  isMandatory: boolean;
  isActive: boolean;
}

/** Body for POST /api/vaccine/create */
export interface CreateVaccineRequest {
  nameAr: string;
  nameEn: string;
  totalDoses: number;
  isMandatory: boolean;
}

/** Body for PUT /api/vaccine/update */
export interface UpdateVaccineRequest extends CreateVaccineRequest {
  id: string;
}

export type VaccineListResponse = ApiResponse<Vaccine[]>;

export type VaccineSingleResponse = ApiResponse<Vaccine>;
