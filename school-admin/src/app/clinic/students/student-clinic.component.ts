import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { StudentService } from '../../core/services/student.service';
import { StudentMedicalProfileService } from '../../core/services/student-medical-profile.service';
import { StudentVaccinationService } from '../../core/services/student-vaccination.service';
import { ClinicVisitService } from '../../core/services/clinic-visit.service';
import { VaccineService } from '../../core/services/vaccine.service';
import { VaccinationCampaignService } from '../../core/services/vaccination-campaign.service';
import { EmployeeService } from '../../core/services/employee.service';
import { Student } from '../../core/models/student.model';
import { BLOOD_TYPES, SaveStudentMedicalProfileRequest, StudentMedicalProfile } from '../../core/models/student-medical-profile.model';
import {
  CreateStudentVaccinationRequest,
  StudentVaccination,
  VACCINATION_PLACES,
  VACCINATION_STATUSES,
  VaccinationPlace,
  VaccinationStatus
} from '../../core/models/student-vaccination.model';
import { ClinicVisit, VISIT_OUTCOMES } from '../../core/models/clinic-visit.model';
import { Vaccine } from '../../core/models/vaccine.model';
import { VaccinationCampaign } from '../../core/models/vaccination-campaign.model';
import { Employee } from '../../core/models/employee.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of, timer } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { clinicApiError } from '../clinic-errors';
import { formatDate, todayIso } from '../../notification/shared/audience';

/** The employee group the nurses were last picked from (staff of this group are listed first for "given by") */
const DOCTOR_GROUP_KEY = 'clinic.doctorGroupId';

/**
 * Clinic file of one student: medical profile, vaccine doses (add / edit / delete) and clinic visits.
 */
@Component({
  selector: 'app-student-clinic',
  standalone: false,
  styleUrl: './student-clinic.component.scss',
  templateUrl: './student-clinic.component.html',
})
export class StudentClinicComponent implements OnInit, OnDestroy {
  studentId = '';
  student = signal<Student | null>(null);
  vaccinations = signal<StudentVaccination[]>([]);
  visits = signal<ClinicVisit[]>([]);
  vaccines = signal<Vaccine[]>([]);
  campaigns = signal<VaccinationCampaign[]>([]);
  staff = signal<Employee[]>([]);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  isSavingProfile = signal(false);
  isSavingDose = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  doseError = signal<string | null>(null);

  profileForm!: FormGroup;
  doseForm!: FormGroup;
  /** Dose being edited (null: a new dose); the dose form shows while [showDoseForm] */
  editingDose = signal<StudentVaccination | null>(null);
  showDoseForm = signal(false);
  doseVaccineId = signal('');

  readonly bloodTypes = BLOOD_TYPES;
  readonly statuses = VACCINATION_STATUSES;
  readonly places = VACCINATION_PLACES;
  readonly outcomes = VISIT_OUTCOMES;

  /** Doses sorted by vaccine then dose number */
  sortedVaccinations = computed(() =>
    [...this.vaccinations()].sort((a, b) =>
      this.vaccineLabel(a).localeCompare(this.vaccineLabel(b)) || a.doseNumber - b.doseNumber));

  sortedVisits = computed(() => [...this.visits()].sort((a, b) => b.visitDate.localeCompare(a.visitDate)));

  /** Mandatory active vaccines the student has not completed (Given doses below the vaccine's doses) */
  missingMandatory = computed(() =>
    this.vaccines().filter(v => v.isActive && v.isMandatory &&
      this.vaccinations().filter(d => d.vaccineId === v.id && d.status === VaccinationStatus.Given).length < v.totalDoses));

  /** Active campaigns of the vaccine chosen in the dose form */
  doseCampaigns = computed(() => this.campaigns().filter(c => c.vaccineId === this.doseVaccineId() && (c.isActive || c.id === this.editingDose()?.campaignId)));

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private doseToDelete: StudentVaccination | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private studentService: StudentService,
    private profileService: StudentMedicalProfileService,
    private vaccinationService: StudentVaccinationService,
    private visitService: ClinicVisitService,
    private vaccineService: VaccineService,
    private campaignService: VaccinationCampaignService,
    private employeeService: EmployeeService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.initializeForms();
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.studentId = this.activatedRoute.snapshot.paramMap.get('id') || '';
    this.loadData();
  }

  ngOnDestroy(): void {
    this.modalService.dismissAll();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    this.profileForm = this.formBuilder.group({
      bloodType: [''],
      allergies: ['', Validators.maxLength(1000)],
      chronicDiseases: ['', Validators.maxLength(1000)],
      regularMedications: ['', Validators.maxLength(1000)],
      emergencyPhone: ['', Validators.maxLength(50)],
      notes: ['', Validators.maxLength(2000)]
    });

    this.doseForm = this.formBuilder.group({
      vaccineId: ['', Validators.required],
      doseNumber: [1, [Validators.required, Validators.min(1)]],
      status: [VaccinationStatus.Given, Validators.required],
      vaccinationDate: [todayIso()],
      place: [VaccinationPlace.School, Validators.required],
      campaignId: [''],
      givenByEmployeeId: [''],
      notes: ['', Validators.maxLength(1000)]
    });

    this.doseForm.get('vaccineId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(vaccineId => {
        this.doseVaccineId.set(vaccineId || '');
        // A new dose: the next one of that vaccine, and no campaign of another vaccine
        if (!this.editingDose() && vaccineId) {
          this.doseForm.patchValue({ doseNumber: this.nextDose(vaccineId), campaignId: '' }, { emitEvent: false });
        }
      });
  }

  /** Student, profile, doses, visits, vaccines, campaigns and the clinic staff */
  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    let groupId: string | null = null;
    try {
      groupId = localStorage.getItem(DOCTOR_GROUP_KEY);
    } catch {
      groupId = null;
    }

    forkJoin({
      student: this.studentService.getById(this.studentId),
      profile: this.profileService.getByStudent(this.studentId),
      vaccinations: this.vaccinationService.getByStudent(this.studentId),
      visits: this.visitService.getByStudent(this.studentId),
      vaccines: this.vaccineService.getAll(),
      campaigns: this.campaignService.getAll(),
      // Names of doctors / nurses are optional (Identity service)
      staff: this.employeeService.getAll().pipe(catchError(() => of(null)))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (r) => {
          if (r.student.status !== 200 || !r.student.data) {
            this.errorMessage.set(this.translate.instant('clinic.studentNotFound'));
            this.isLoading.set(false);
            return;
          }
          this.student.set(r.student.data);
          this.populateProfile(r.profile.data);
          this.vaccinations.set(r.vaccinations.data || []);
          this.visits.set(r.visits.data || []);
          this.vaccines.set(r.vaccines.data || []);
          this.campaigns.set(r.campaigns.data || []);
          const staff = r.staff?.data || [];
          // Staff of the clinic group first (the group used for doctors)
          this.staff.set(groupId ? [...staff.filter(e => e.employeeGroupId === groupId), ...staff.filter(e => e.employeeGroupId !== groupId)] : staff);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading student clinic file:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Medical profile
  // ---------------------------------------------------------------------------

  private populateProfile(profile: StudentMedicalProfile | null): void {
    this.profileForm.reset({
      bloodType: profile?.bloodType ?? '',
      allergies: profile?.allergies ?? '',
      chronicDiseases: profile?.chronicDiseases ?? '',
      regularMedications: profile?.regularMedications ?? '',
      emergencyPhone: profile?.emergencyPhone ?? '',
      notes: profile?.notes ?? ''
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const v = this.profileForm.value;
    const text = (value: any): string | null => String(value ?? '').trim() || null;
    const request: SaveStudentMedicalProfileRequest = {
      studentId: this.studentId,
      bloodType: text(v.bloodType),
      allergies: text(v.allergies),
      chronicDiseases: text(v.chronicDiseases),
      regularMedications: text(v.regularMedications),
      emergencyPhone: text(v.emergencyPhone),
      notes: text(v.notes)
    };

    this.isSavingProfile.set(true);
    this.profileService.save(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.showSuccess(this.translate.instant('clinic.profileSaved'));
            this.profileForm.markAsPristine();
          } else {
            this.errorMessage.set(clinicApiError(this.translate, response.message, 'clinic.saveFailed'));
          }
          this.isSavingProfile.set(false);
        },
        error: (error) => {
          this.errorMessage.set(clinicApiError(this.translate, error?.error?.message, 'clinic.saveFailed'));
          this.isSavingProfile.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Vaccine doses
  // ---------------------------------------------------------------------------

  /** Vaccines offered in the dose form: active ones (plus the one of the dose being edited) */
  doseVaccines(): Vaccine[] {
    const editingId = this.editingDose()?.vaccineId;
    return this.vaccines().filter(v => v.isActive || v.id === editingId);
  }

  openNewDose(vaccineId = ''): void {
    this.editingDose.set(null);
    this.doseError.set(null);
    this.doseForm.reset({
      vaccineId: '',
      doseNumber: 1,
      status: VaccinationStatus.Given,
      vaccinationDate: todayIso(),
      place: VaccinationPlace.School,
      campaignId: '',
      givenByEmployeeId: '',
      notes: ''
    });
    if (vaccineId) this.doseForm.patchValue({ vaccineId });
    this.showDoseForm.set(true);
  }

  editDose(dose: StudentVaccination): void {
    this.editingDose.set(dose);
    this.doseError.set(null);
    this.doseForm.reset({
      vaccineId: dose.vaccineId,
      doseNumber: dose.doseNumber,
      status: dose.status,
      vaccinationDate: dose.vaccinationDate ? dose.vaccinationDate.substring(0, 10) : '',
      place: dose.place,
      campaignId: dose.campaignId ?? '',
      givenByEmployeeId: dose.givenByEmployeeId ?? '',
      notes: dose.notes ?? ''
    });
    this.doseVaccineId.set(dose.vaccineId);
    this.showDoseForm.set(true);
  }

  cancelDose(): void {
    this.showDoseForm.set(false);
    this.editingDose.set(null);
    this.doseError.set(null);
  }

  saveDose(): void {
    const v = this.doseForm.value;
    if (this.doseForm.invalid) {
      this.doseForm.markAllAsTouched();
      return;
    }
    // A given dose needs its date
    if (Number(v.status) === VaccinationStatus.Given && !v.vaccinationDate) {
      this.doseError.set(this.translate.instant('clinic.errors.VaccinationDateRequired'));
      return;
    }

    const request: CreateStudentVaccinationRequest = {
      studentId: this.studentId,
      vaccineId: v.vaccineId,
      doseNumber: Number(v.doseNumber),
      status: Number(v.status),
      vaccinationDate: v.vaccinationDate || null,
      place: Number(v.place),
      campaignId: v.campaignId || null,
      givenByEmployeeId: v.givenByEmployeeId || null,
      notes: String(v.notes ?? '').trim() || null
    };

    const editing = this.editingDose();
    const { studentId, ...updateFields } = request;
    const request$ = editing
      ? this.vaccinationService.update({ ...updateFields, id: editing.id })
      : this.vaccinationService.create(request);

    this.isSavingDose.set(true);
    this.doseError.set(null);
    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.showDoseForm.set(false);
          this.editingDose.set(null);
          this.showSuccess(this.translate.instant('clinic.doseSaved'));
          this.reloadVaccinations();
        } else {
          this.doseError.set(clinicApiError(this.translate, response.message, 'clinic.saveFailed'));
        }
        this.isSavingDose.set(false);
      },
      error: (error) => {
        this.doseError.set(clinicApiError(this.translate, error?.error?.message, 'clinic.saveFailed'));
        this.isSavingDose.set(false);
      }
    });
  }

  openDeleteDose(dose: StudentVaccination, content: any): void {
    this.doseToDelete = dose;
    this.deleteModalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  confirmDeleteDose(): void {
    const dose = this.doseToDelete;
    if (!dose) return;

    this.isDeleting.set(true);
    this.vaccinationService.delete(dose.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.vaccinations.update(list => list.filter(d => d.id !== dose.id));
            this.showSuccess(this.translate.instant('clinic.deleted'));
          } else {
            this.errorMessage.set(response.message || this.translate.instant('clinic.deleteFailed'));
          }
          this.deleteModalRef?.close();
          this.doseToDelete = null;
          this.isDeleting.set(false);
        },
        error: () => {
          this.errorMessage.set(this.translate.instant('clinic.deleteFailed'));
          this.deleteModalRef?.close();
          this.doseToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.doseToDelete = null;
  }

  private reloadVaccinations(): void {
    this.vaccinationService.getByStudent(this.studentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => this.vaccinations.set(response.data || []));
  }

  /** Next dose number of a vaccine for this student */
  private nextDose(vaccineId: string): number {
    const numbers = this.vaccinations().filter(d => d.vaccineId === vaccineId).map(d => d.doseNumber);
    return numbers.length ? Math.max(...numbers) + 1 : 1;
  }

  // ---------------------------------------------------------------------------
  // Visits
  // ---------------------------------------------------------------------------

  newVisit(): void {
    this.router.navigate(['/clinic/visits/save'], { queryParams: { studentId: this.studentId, returnTo: this.router.url } });
  }

  editVisit(visit: ClinicVisit): void {
    this.router.navigate(['/clinic/visits/save', visit.id], { queryParams: { returnTo: this.router.url } });
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  studentName(): string {
    const s = this.student();
    if (!s) return '';
    return this.currentLanguage() === 'ar' ? s.fullNameAr : s.fullNameEn;
  }

  vaccineName(vaccine: Vaccine): string {
    return this.currentLanguage() === 'ar' ? vaccine.nameAr : vaccine.nameEn;
  }

  vaccineLabel(dose: StudentVaccination): string {
    return this.currentLanguage() === 'ar' ? dose.vaccineNameAr : dose.vaccineNameEn;
  }

  totalDoses(vaccineId: string): number | null {
    return this.vaccines().find(v => v.id === vaccineId)?.totalDoses ?? null;
  }

  statusInfo(dose: StudentVaccination) {
    return this.statuses.find(s => s.value === dose.status) ?? this.statuses[0];
  }

  placeKey(dose: StudentVaccination): string {
    return this.places.find(p => p.value === dose.place)?.key ?? '';
  }

  campaignTitle(id: string | null): string {
    return id ? this.campaigns().find(c => c.id === id)?.title ?? '' : '';
  }

  staffName(id: string | null): string {
    if (!id) return '';
    const e = this.staff().find(x => x.id === id);
    return e ? (this.currentLanguage() === 'ar' ? e.fullNameAr : e.fullNameEn) : '';
  }

  personName(e: Employee): string {
    return this.currentLanguage() === 'ar' ? e.fullNameAr : e.fullNameEn;
  }

  outcomeInfo(visit: ClinicVisit) {
    return this.outcomes.find(o => o.value === visit.outcome) ?? this.outcomes[0];
  }

  formatDate(value: string | null): string {
    return value ? formatDate(value) : '-';
  }

  formatDateTime(value: string): string {
    const date = new Date(value);
    return isNaN(date.getTime())
      ? value
      : date.toLocaleString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { dateStyle: 'medium', timeStyle: 'short' });
  }

  back(): void {
    this.router.navigate(['/clinic/students']);
  }

  private showSuccess(message: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }
}
