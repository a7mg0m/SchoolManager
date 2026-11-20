import { ApiResponse } from './stage.model';

/**
 * Student Medical Profile Model (one per student)
 */
export interface StudentMedicalProfile {
  id: string;
  studentId: string;
  bloodType: string | null;
  allergies: string | null;
  chronicDiseases: string | null;
  regularMedications: string | null;
  emergencyPhone: string | null;
  notes: string | null;
}

/** Body for POST /api/student-medical-profile/create (creates or updates the student's profile) */
export interface SaveStudentMedicalProfileRequest {
  studentId: string;
  bloodType: string | null;
  allergies: string | null;
  chronicDiseases: string | null;
  regularMedications: string | null;
  emergencyPhone: string | null;
  notes: string | null;
}

export const BLOOD_TYPES = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

/** data is null when the student has no profile yet */
export type StudentMedicalProfileSingleResponse = ApiResponse<StudentMedicalProfile | null>;

export type StudentMedicalProfileListResponse = ApiResponse<StudentMedicalProfile[]>;
