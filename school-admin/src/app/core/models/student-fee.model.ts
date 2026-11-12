import { ApiResponse } from './stage.model';

/**
 * Mirrors SchoolManager.Financial.Domain.FeeStatus.
 * Serialized by the API as its numeric value.
 */
export enum FeeStatus {
  Pending = 0,
  PartiallyPaid = 1,
  Paid = 2,
  Waived = 3,
  Overdue = 4
}

export const FEE_STATUSES: { value: FeeStatus; key: string; badge: string }[] = [
  { value: FeeStatus.Pending, key: 'studentFees.statuses.pending', badge: 'bg-warning text-dark' },
  { value: FeeStatus.PartiallyPaid, key: 'studentFees.statuses.partiallyPaid', badge: 'bg-info text-dark' },
  { value: FeeStatus.Paid, key: 'studentFees.statuses.paid', badge: 'bg-success' },
  { value: FeeStatus.Waived, key: 'studentFees.statuses.waived', badge: 'bg-secondary' },
  { value: FeeStatus.Overdue, key: 'studentFees.statuses.overdue', badge: 'bg-danger' }
];

/** Today's date as yyyy-MM-dd in local time */
function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** True when the fee's due date (date part) is before today */
export function isFeePastDue(fee: Pick<StudentFee, 'dueDate'>): boolean {
  const due = (fee.dueDate || '').substring(0, 10);
  return !!due && due < todayIso();
}

/**
 * Nothing in the backend calls StudentFee.MarkOverdueIfDue yet, so the stored
 * status never becomes Overdue. Derive it: past due date + balance left.
 */
export function effectiveFeeStatus(fee: StudentFee): FeeStatus {
  if (
    (fee.status === FeeStatus.Pending || fee.status === FeeStatus.PartiallyPaid) &&
    fee.balance > 0 &&
    isFeePastDue(fee)
  ) {
    return FeeStatus.Overdue;
  }
  return fee.status;
}

/** Translation key + badge class for a fee, using its effective status */
export function feeStatusInfo(fee: StudentFee) {
  const status = effectiveFeeStatus(fee);
  return FEE_STATUSES.find(s => s.value === status) ?? FEE_STATUSES[0];
}

/**
 * Student Fee (Financial service StudentFeeDto).
 * studentId refers to a Student in the Management service.
 */
export interface StudentFee {
  id: string;
  studentId: string;
  totalAmount: number;
  discountAmount: number;
  paidAmount: number;
  netAmount: number;
  balance: number;
  currency: string;
  status: FeeStatus;
  /** ISO date-time */
  dueDate: string;
  /** Unpaid balance forgiven by a waiver (0 when not waived, or waived by a full discount) */
  waivedAmount: number;
  waiveReason: string | null;
  /** ISO date-time (UTC) */
  waivedAt: string | null;
}

/** Body for PUT /api/student-fee/waive/{id} */
export interface WaiveStudentFeeRequest {
  reason: string;
}

/** Max length of a waive reason (StudentFee.MaxWaiveReasonLength in the backend) */
export const MAX_WAIVE_REASON_LENGTH = 500;

/** A fee can be waived while something is still owed and it isn't already waived */
export function canWaiveFee(fee: StudentFee): boolean {
  return fee.status !== FeeStatus.Waived && fee.status !== FeeStatus.Paid && fee.balance > 0;
}

/** Only waivers made via "waive" can be undone; a 100% discount at assignment cannot */
export function canRestoreWaiver(fee: StudentFee): boolean {
  return fee.status === FeeStatus.Waived && (fee.waivedAmount ?? 0) > 0;
}

/**
 * Body for POST /api/student-fee/assign
 * One StudentFee is created per student, totalling the selected fees.
 */
export interface AssignStudentFeesRequest {
  studentIds: string[];
  feeIds: string[];
  /** Discount applied to each student's total (same currency as the fees) */
  discountAmount: number;
  /** ISO date-time */
  dueDate: string;
}

/** Response of POST /api/student-fee/assign: ids of the created student fees */
export type AssignStudentFeesResponse = ApiResponse<string[]>;

/** Body for PUT /api/student-fee/apply-payment/{id} */
export interface ApplyStudentFeePaymentRequest {
  amount: number;
  currency: string;
}

export type StudentFeeListResponse = ApiResponse<StudentFee[]>;

export type StudentFeeSingleResponse = ApiResponse<StudentFee>;
