/**
 * Stage Model
 * Represents an educational stage (e.g., Nursery, Primary, Preparatory, Secondary)
 */
export interface Stage {
  id: string;
  nameAr: string;
  nameEn: string;
  isActive: boolean;
}

/**
 * API Response for stage operations
 */
export interface ApiResponse<T> {
  status: number;
  insertedId: string | null;
  message: string;
  data: T;
  errors: any;
}

/**
 * Typed response for stage list
 */
export type StageListResponse = ApiResponse<Stage[]>;

/**
 * Typed response for stage single operation
 */
export type StageSingleResponse = ApiResponse<Stage>;
