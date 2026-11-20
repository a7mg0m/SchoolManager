import { ApiResponse } from './stage.model';

/** Mirrors SchoolManager.Management.Domain.VisitOutcome (serialized as its number) */
export enum VisitOutcome {
  ReturnedToClass = 1,
  SentHome = 2,
  Referred = 3
}

export const VISIT_OUTCOMES: { value: VisitOutcome; key: string; badge: string }[] = [
  { value: VisitOutcome.ReturnedToClass, key: 'clinic.visitOutcome.returnedToClass', badge: 'bg-success' },
  { value: VisitOutcome.SentHome, key: 'clinic.visitOutcome.sentHome', badge: 'bg-warning text-dark' },
  { value: VisitOutcome.Referred, key: 'clinic.visitOutcome.referred', badge: 'bg-danger' }
];

/** A medication given in a visit */
export interface VisitMedication {
  medicineName: string;
  dose: string;
}

/**
 * Clinic Visit Model (examination and diagnosis)
 */
export interface ClinicVisit {
  id: string;
  studentId: string;
  /** Name of the doctor (typed in) */
  doctorName: string;
  /** ISO date-time */
  visitDate: string;
  complaint: string;
  temperature: number | null;
  weight: number | null;
  height: number | null;
  diagnosis: string | null;
  treatment: string | null;
  outcome: VisitOutcome;
  parentNotified: boolean;
  notes: string | null;
  medications: VisitMedication[];
}

/** Body for POST /api/clinic-visit/create */
export interface CreateClinicVisitRequest {
  studentId: string;
  doctorName: string;
  visitDate: string;
  complaint: string;
  temperature: number | null;
  weight: number | null;
  height: number | null;
  diagnosis: string | null;
  treatment: string | null;
  outcome: VisitOutcome;
  parentNotified: boolean;
  notes: string | null;
  /** Replaces the saved list */
  medications: VisitMedication[];
}

/** Body for PUT /api/clinic-visit/update (the student does not change) */
export interface UpdateClinicVisitRequest extends Omit<CreateClinicVisitRequest, 'studentId'> {
  id: string;
}

export type ClinicVisitListResponse = ApiResponse<ClinicVisit[]>;

export type ClinicVisitSingleResponse = ApiResponse<ClinicVisit>;
