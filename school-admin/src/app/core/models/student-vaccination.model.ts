import { ApiResponse } from './stage.model';

/** Mirrors SchoolManager.Management.Domain.VaccinationStatus (serialized as its number) */
export enum VaccinationStatus {
  Given = 1,
  NotGiven = 2,
  Exempted = 3,
  ParentRefused = 4
}

/** Mirrors SchoolManager.Management.Domain.VaccinationPlace */
export enum VaccinationPlace {
  School = 1,
  External = 2
}

export const VACCINATION_STATUSES: { value: VaccinationStatus; key: string; badge: string }[] = [
  { value: VaccinationStatus.Given, key: 'clinic.vaccinationStatus.given', badge: 'bg-success' },
  { value: VaccinationStatus.NotGiven, key: 'clinic.vaccinationStatus.notGiven', badge: 'bg-secondary' },
  { value: VaccinationStatus.Exempted, key: 'clinic.vaccinationStatus.exempted', badge: 'bg-info text-dark' },
  { value: VaccinationStatus.ParentRefused, key: 'clinic.vaccinationStatus.parentRefused', badge: 'bg-danger' }
];

export const VACCINATION_PLACES: { value: VaccinationPlace; key: string }[] = [
  { value: VaccinationPlace.School, key: 'clinic.vaccinationPlace.school' },
  { value: VaccinationPlace.External, key: 'clinic.vaccinationPlace.external' }
];

/**
 * Student Vaccination Model: one dose of a vaccine for a student
 */
export interface StudentVaccination {
  id: string;
  studentId: string;
  vaccineId: string;
  vaccineNameAr: string;
  vaccineNameEn: string;
  doseNumber: number;
  status: VaccinationStatus;
  /** yyyy-MM-dd, null when not given */
  vaccinationDate: string | null;
  place: VaccinationPlace;
  campaignId: string | null;
  /** Employee (Identity service) who gave the dose */
  givenByEmployeeId: string | null;
  notes: string | null;
}

/** Body for POST /api/student-vaccination/create */
export interface CreateStudentVaccinationRequest {
  studentId: string;
  vaccineId: string;
  doseNumber: number;
  status: VaccinationStatus;
  vaccinationDate: string | null;
  place: VaccinationPlace;
  campaignId: string | null;
  givenByEmployeeId: string | null;
  notes: string | null;
}

/** Body for PUT /api/student-vaccination/update (the student does not change) */
export interface UpdateStudentVaccinationRequest extends Omit<CreateStudentVaccinationRequest, 'studentId'> {
  id: string;
}

export type StudentVaccinationListResponse = ApiResponse<StudentVaccination[]>;

export type StudentVaccinationSingleResponse = ApiResponse<StudentVaccination>;
