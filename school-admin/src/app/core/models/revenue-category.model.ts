import { ApiResponse } from './stage.model';

/**
 * Revenue Category Model (Financial service)
 */
export interface RevenueCategory {
  id: string;
  name: string;
  description: string | null;
}

/**
 * Request body for POST /api/revenue-category/create
 */
export interface CreateRevenueCategoryRequest {
  name: string;
  description: string | null;
}

/**
 * Request body for PUT /api/revenue-category/update/{id}
 * (the id travels in the URL, not the body)
 */
export interface UpdateRevenueCategoryRequest {
  name: string;
  description: string | null;
}

export type RevenueCategoryListResponse = ApiResponse<RevenueCategory[]>;

export type RevenueCategorySingleResponse = ApiResponse<RevenueCategory>;
