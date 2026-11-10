import { ApiResponse } from './stage.model';

/**
 * Section Model
 * Represents a class section within a grade and stage
 */
export interface Section {
  id: string;
  nameAr: string;
  nameEn: string;
  stageId: string;
  gradeId: string;
}

/**
 * Request model for creating a new section
 */
export interface CreateSectionRequest {
  id: string;
  nameAr: string;
  nameEn: string;
  stageId: string;
  gradeId: string;
}

/**
 * Request model for updating a section
 */
export interface UpdateSectionRequest {
  id: string;
  nameAr: string;
  nameEn: string;
  stageId: string;
  gradeId: string;
}

/**
 * Typed response for section list
 */
export type SectionListResponse = ApiResponse<Section[]>;

/**
 * Typed response for section single operation
 */
export type SectionSingleResponse = ApiResponse<Section>;
