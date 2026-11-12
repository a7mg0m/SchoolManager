import { ApiResponse } from './stage.model';

/**
 * Mirrors SchoolManager.Financial.Domain.RevenueType.
 * Serialized by the API as its numeric value.
 */
export enum RevenueType {
  Tuition = 0,
  Donation = 1,
  Grant = 2,
  Event = 3,
  Other = 4
}

export const REVENUE_TYPES: { value: RevenueType; key: string }[] = [
  { value: RevenueType.Tuition, key: 'revenues.types.tuition' },
  { value: RevenueType.Donation, key: 'revenues.types.donation' },
  { value: RevenueType.Grant, key: 'revenues.types.grant' },
  { value: RevenueType.Event, key: 'revenues.types.event' },
  { value: RevenueType.Other, key: 'revenues.types.other' }
];

/**
 * Revenue Model (Financial service)
 */
export interface Revenue {
  id: string;
  title: string;
  description: string | null;
  categoryId: string;
  categoryName: string | null;
  type: RevenueType;
  amount: number;
  currency: string;
  /** ISO date-time, e.g. "2026-11-12T00:00:00" */
  revenueDate: string;
  reference: string | null;
}

/**
 * Body for POST /api/revenue/create and PUT /api/revenue/update/{id}
 */
export interface SaveRevenueRequest {
  title: string;
  description: string | null;
  categoryId: string;
  type: RevenueType;
  amount: number;
  currency: string;
  revenueDate: string;
  reference: string | null;
}

export type RevenueListResponse = ApiResponse<Revenue[]>;

export type RevenueSingleResponse = ApiResponse<Revenue>;
