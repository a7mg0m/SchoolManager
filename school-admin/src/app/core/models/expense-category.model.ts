import { ApiResponse } from './stage.model';

/**
 * Expense Category Model (Financial service)
 */
export interface ExpenseCategory {
  id: string;
  name: string;
  description: string | null;
}

/**
 * Request body for POST /api/expense-category/create
 */
export interface CreateExpenseCategoryRequest {
  name: string;
  description: string | null;
}

/**
 * Request body for PUT /api/expense-category/update/{id}
 * (the id travels in the URL, not the body)
 */
export interface UpdateExpenseCategoryRequest {
  name: string;
  description: string | null;
}

export type ExpenseCategoryListResponse = ApiResponse<ExpenseCategory[]>;

export type ExpenseCategorySingleResponse = ApiResponse<ExpenseCategory>;
