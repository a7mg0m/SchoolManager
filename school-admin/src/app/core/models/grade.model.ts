import { ApiResponse } from './stage.model';

/**
 * Grade Model
 * Represents an educational grade within a stage
 */
export interface Grade {
  id: string;
  nameAr: string;
  nameEn: string;
  stageId: string;
  isActive: boolean;
}

/**
 * Typed response for grade list
 */
export type GradeListResponse = ApiResponse<Grade[]>;

/**
 * Typed response for grade single operation
 */
export type GradeSingleResponse = ApiResponse<Grade>;
