import { ApiResponse } from './stage.model';

/**
 * Vaccination Campaign Model (School clinic)
 */
export interface VaccinationCampaign {
  id: string;
  vaccineId: string;
  title: string;
  /** yyyy-MM-dd */
  startDate: string;
  /** yyyy-MM-dd */
  endDate: string;
  isActive: boolean;
}

/** Body for POST /api/vaccination-campaign/create */
export interface CreateVaccinationCampaignRequest {
  vaccineId: string;
  title: string;
  startDate: string;
  endDate: string;
}

/** Body for PUT /api/vaccination-campaign/update */
export interface UpdateVaccinationCampaignRequest extends CreateVaccinationCampaignRequest {
  id: string;
}

export type VaccinationCampaignListResponse = ApiResponse<VaccinationCampaign[]>;

export type VaccinationCampaignSingleResponse = ApiResponse<VaccinationCampaign>;
