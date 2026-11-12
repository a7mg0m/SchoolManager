import { ApiResponse } from './stage.model';

/**
 * Mirrors SchoolManager.Financial.Domain.ExpenseType.
 * Serialized by the API as its numeric value.
 */
export enum ExpenseType {
  Salary = 0,
  Utilities = 1,
  Maintenance = 2,
  Supplies = 3,
  Transportation = 4,
  Equipment = 5,
  Event = 6,
  Other = 7
}

export const EXPENSE_TYPES: { value: ExpenseType; key: string }[] = [
  { value: ExpenseType.Salary, key: 'expenses.types.salary' },
  { value: ExpenseType.Utilities, key: 'expenses.types.utilities' },
  { value: ExpenseType.Maintenance, key: 'expenses.types.maintenance' },
  { value: ExpenseType.Supplies, key: 'expenses.types.supplies' },
  { value: ExpenseType.Transportation, key: 'expenses.types.transportation' },
  { value: ExpenseType.Equipment, key: 'expenses.types.equipment' },
  { value: ExpenseType.Event, key: 'expenses.types.event' },
  { value: ExpenseType.Other, key: 'expenses.types.other' }
];

/** Empty Guid used when an expense isn't linked to a school event */
export const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/**
 * Expense Model (Financial service)
 */
export interface Expense {
  id: string;
  title: string;
  description: string | null;
  eventId: string;
  categoryId: string;
  categoryName: string | null;
  type: ExpenseType;
  amount: number;
  currency: string;
  /** ISO date-time, e.g. "2026-11-12T00:00:00" */
  expenseDate: string;
  reference: string | null;
}

/**
 * Body for POST /api/expense/create and PUT /api/expense/update/{id}
 */
export interface SaveExpenseRequest {
  title: string;
  description: string | null;
  eventId: string;
  categoryId: string;
  type: ExpenseType;
  amount: number;
  currency: string;
  expenseDate: string;
  reference: string | null;
}

export type ExpenseListResponse = ApiResponse<Expense[]>;

export type ExpenseSingleResponse = ApiResponse<Expense>;
