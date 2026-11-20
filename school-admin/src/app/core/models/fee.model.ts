import { ApiResponse } from './stage.model';

/**
 * Mirrors SchoolManager.Financial.Domain.FeeType.
 * Serialized by the API as its numeric value.
 */
export enum FeeType {
  Tuition = 0,
  Transportation = 1,
  Books = 2,
  Uniform = 3,
  Activity = 4,
  Registration = 5,
  Other = 6
}

export const FEE_TYPES: { value: FeeType; key: string }[] = [
  { value: FeeType.Tuition, key: 'fees.types.tuition' },
  { value: FeeType.Transportation, key: 'fees.types.transportation' },
  { value: FeeType.Books, key: 'fees.types.books' },
  { value: FeeType.Uniform, key: 'fees.types.uniform' },
  { value: FeeType.Activity, key: 'fees.types.activity' },
  { value: FeeType.Registration, key: 'fees.types.registration' },
  { value: FeeType.Other, key: 'fees.types.other' }
];

/**
 * Fee Model (Financial service).
 * gradeId refers to a Grade in the Management service.
 */
export interface Fee {
  id: string;
  gradeId: string;
  /** null: saved before academic years (shown in every year) */
  academicYearId?: string | null;
  name: string;
  type: FeeType;
  amount: number;
  currency: string;
  isMandatory: boolean;
}

/** Body for POST /api/fee/create */
export interface CreateFeeRequest {
  gradeId: string;
  academicYearId: string;
  name: string;
  type: FeeType;
  amount: number;
  currency: string;
  isMandatory: boolean;
}

/**
 * Body for PUT /api/fee/update/{id}
 * Note: the grade of an existing fee cannot be changed by the API.
 */
export interface UpdateFeeRequest {
  /** Gives a fee saved before academic years its year (ignored when it has one) */
  academicYearId?: string | null;
  name: string;
  type: FeeType;
  amount: number;
  currency: string;
  isMandatory: boolean;
}

export type FeeListResponse = ApiResponse<Fee[]>;

export type FeeSingleResponse = ApiResponse<Fee>;
