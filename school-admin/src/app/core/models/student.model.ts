import { ApiResponse } from './stage.model';
import { ParentStudent } from './parent.model';

/**
 * Student (Management service StudentDto).
 * Same shape as ParentStudent plus the stage.
 */
export interface Student extends ParentStudent {
  stageId: string;
}

export type StudentListResponse = ApiResponse<Student[]>;

export type StudentSingleResponse = ApiResponse<Student>;
