import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { ParentService } from '../../core/services/parent.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { GradeService } from '../../core/services/grade.service';
import { StageService } from '../../core/services/stage.service';
import { SectionService } from '../../core/services/section.service';
import {
  Parent,
  ParentStudent,
  ParentStudentDto,
  CreateParentRequest,
  UpdateParentRequest,
  ParentSingleResponse
} from '../../core/models/parent.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';
import { Section } from '../../core/models/section.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

type Gender = 'Male' | 'Female';

/**
 * FormArray validator: national IDs must be unique among the parent's students
 * (the backend has a unique index on Student.NationalId)
 */
function uniqueNationalIds(control: AbstractControl): ValidationErrors | null {
  const groups = (control as FormArray).controls as FormGroup[];
  const seen = new Map<string, number>();
  const duplicates: number[] = [];

  groups.forEach((group, index) => {
    const value = String(group.getRawValue().nationalId ?? '').trim().toLowerCase();
    if (!value) {
      return;
    }
    if (seen.has(value)) {
      duplicates.push(seen.get(value)!, index);
    } else {
      seen.set(value, index);
    }
  });

  return duplicates.length ? { duplicateNationalId: [...new Set(duplicates)] } : null;
}

@Component({
  selector: 'app-parent-save',
  standalone: false,
  styleUrl: './parent-save.component.scss',
  templateUrl: './parent-save.component.html',
})
export class ParentSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  parentId: string | null = null;

  readonly genders: Gender[] = ['Male', 'Female'];
  /** Latest allowed date of birth (today), ISO yyyy-MM-dd */
  readonly maxDateOfBirth = new Date().toISOString().substring(0, 10);

  // Reference data
  academicYears = signal<AcademicYear[]>([]);
  private stages = signal<Stage[]>([]);
  private grades = signal<Grade[]>([]);
  private sections = signal<Section[]>([]);
  /** Stages/grades already used by loaded students (kept in the dropdowns even if now inactive) */
  private keptStageIds = signal<Set<string>>(new Set());
  private keptGradeIds = signal<Set<string>>(new Set());

  /** Grade lookup by ID */
  private gradeMap = computed(() => new Map(this.grades().map(g => [g.id, g])));

  /**
   * Active stages, plus stages already used by existing students
   */
  stageOptions = computed(() => {
    const keep = this.keptStageIds();
    return this.stages().filter(s => s.isActive || keep.has(s.id));
  });

  /** Usable grades (active, or already used by existing students) indexed by stage ID */
  private gradesByStage = computed(() => {
    const keep = this.keptGradeIds();
    const map = new Map<string, Grade[]>();
    for (const grade of this.grades()) {
      if (!grade.isActive && !keep.has(grade.id)) {
        continue;
      }
      if (!map.has(grade.stageId)) {
        map.set(grade.stageId, []);
      }
      map.get(grade.stageId)!.push(grade);
    }
    return map;
  });

  /** Sections indexed by grade ID */
  private sectionsByGrade = computed(() => {
    const map = new Map<string, Section[]>();
    for (const section of this.sections()) {
      if (!map.has(section.gradeId)) {
        map.set(section.gradeId, []);
      }
      map.get(section.gradeId)!.push(section);
    }
    return map;
  });

  /** Default academic year for new students: the first active one */
  private defaultAcademicYearId = computed(() => this.academicYears().find(a => a.isActive)?.id ?? '');

  private readonly emptyGuid = '00000000-0000-0000-0000-000000000000';

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private parentService: ParentService,
    private academicYearService: AcademicYearService,
    private gradeService: GradeService,
    private stageService: StageService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private modalService: NgbModal,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.parentId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.parentId);

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get studentsArray(): FormArray {
    return this.form.get('students') as FormArray;
  }

  studentGroup(index: number): FormGroup {
    return this.studentsArray.at(index) as FormGroup;
  }

  /**
   * Initialize the parent form (lengths mirror the backend EF configuration)
   */
  private initializeForm(): void {
    this.form = this.formBuilder.group({
      fullNameAr: ['', [Validators.required, Validators.maxLength(200)]],
      fullNameEn: ['', [Validators.required, Validators.maxLength(200)]],
      phoneNumber: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.email, Validators.maxLength(256)]],
      address: ['', [Validators.required, Validators.maxLength(500)]],
      students: this.formBuilder.array([], uniqueNationalIds)
    });
  }

  /**
   * Build a student form group. Existing students (with an ID) keep their ID, and
   * date of birth / national ID / gender are locked because the backend does not update them.
   * Stage is UI-only (the backend stores only the grade): it is derived from the student's grade.
   */
  private createStudentGroup(student?: Partial<ParentStudent>): FormGroup {
    const gradeId = student?.gradeId ?? '';
    const stageId = gradeId ? this.gradeMap().get(gradeId)?.stageId ?? '' : '';
    const group = this.formBuilder.group({
      id: [student?.id ?? null],
      fullNameAr: [student?.fullNameAr ?? '', [Validators.required, Validators.maxLength(200)]],
      fullNameEn: [student?.fullNameEn ?? '', [Validators.required, Validators.maxLength(200)]],
      dateOfBirth: [student?.dateOfBirth ? student.dateOfBirth.substring(0, 10) : '', Validators.required],
      nationalId: [student?.nationalId ?? '', [Validators.required, Validators.maxLength(50)]],
      gender: [student?.gender ?? '', Validators.required],
      phoneNumber: [student?.phoneNumber ?? '', [Validators.required, Validators.maxLength(50)]],
      email: [student?.email ?? '', [Validators.email, Validators.maxLength(256)]],
      address: [student?.address ?? '', [Validators.required, Validators.maxLength(500)]],
      academicYearId: [student?.academicYearId ?? '', Validators.required],
      stageId: [stageId, Validators.required],
      gradeId: [{ value: gradeId, disabled: !stageId }, Validators.required],
      sectionId: [{ value: student?.sectionId ?? '', disabled: !gradeId }, Validators.required]
    });

    if (student?.id) {
      ['dateOfBirth', 'nationalId', 'gender'].forEach(f => group.get(f)!.disable({ emitEvent: false }));
    }

    // Stage drives the grade list: enable the grade and drop a grade from another stage
    group.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((newStageId: string) => {
        const gradeControl = group.get('gradeId')!;
        if (newStageId) {
          gradeControl.enable({ emitEvent: false });
        } else {
          gradeControl.disable({ emitEvent: false });
        }
        const currentGrade = gradeControl.value;
        if (currentGrade && this.gradeMap().get(currentGrade)?.stageId !== newStageId) {
          // Emits, so the section below is reset too
          gradeControl.setValue('');
        }
      });

    // Grade drives the section list: reset the section whenever the grade changes
    group.get('gradeId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((newGradeId: string) => {
        const sectionControl = group.get('sectionId')!;
        sectionControl.setValue('', { emitEvent: false });
        if (newGradeId) {
          sectionControl.enable({ emitEvent: false });
        } else {
          sectionControl.disable({ emitEvent: false });
        }
      });

    return group;
  }

  /**
   * Load reference data and (in edit mode) the parent in parallel,
   * then build the form once every dropdown has its options
   */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      academicYears: this.academicYearService.getAll(),
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      parent: this.parentId
        ? this.parentService.getById(this.parentId)
        : of<ParentSingleResponse | null>(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ academicYears, stages, grades, sections, parent }) => {
          const ok = <T>(r: { status: number; data: T[] }) => (r.status === 200 && r.data ? r.data : []);
          this.academicYears.set(ok(academicYears));
          this.stages.set(ok(stages));
          this.grades.set(ok(grades));
          this.sections.set(ok(sections));

          if (parent) {
            if (parent.status === 200 && parent.data) {
              this.populateForm(parent.data);
            } else {
              this.errorMessage.set(
                parent.message || this.translate.instant('parents.loadParentFailed')
              );
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading parent form data:', error);
          this.errorMessage.set(
            this.translate.instant(this.parentId ? 'parents.loadParentFailed' : 'parents.loadLookupsFailed')
          );
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate the form with the parent and all of its students
   */
  private populateForm(parent: Parent): void {
    const students = parent.students ?? [];
    const gradeIds = students.map(s => s.gradeId).filter(Boolean);
    this.keptGradeIds.set(new Set(gradeIds));
    this.keptStageIds.set(new Set(
      gradeIds.map(id => this.gradeMap().get(id)?.stageId).filter((id): id is string => !!id)
    ));

    this.form.patchValue({
      fullNameAr: parent.fullNameAr ?? '',
      fullNameEn: parent.fullNameEn ?? '',
      phoneNumber: parent.phoneNumber ?? '',
      email: parent.email ?? '',
      address: parent.address ?? ''
    });

    this.studentsArray.clear();
    students.forEach(s => this.studentsArray.push(this.createStudentGroup(s)));
  }

  /**
   * Add a new, empty student (address prefilled from the parent, default academic year selected)
   */
  addStudent(): void {
    this.studentsArray.push(this.createStudentGroup({
      address: (this.form.get('address')!.value ?? '').trim(),
      academicYearId: this.defaultAcademicYearId()
    }));
  }

  /**
   * Remove a student. Existing students are confirmed first because saving deletes them.
   */
  removeStudent(index: number, confirmContent: any): void {
    if (this.isSaving()) {
      return;
    }
    if (!this.studentGroup(index).get('id')!.value) {
      this.studentsArray.removeAt(index);
      return;
    }

    this.modalService.open(confirmContent, { centered: true }).result.then(
      (confirmed) => {
        if (confirmed) {
          this.studentsArray.removeAt(index);
        }
      },
      () => { /* dismissed */ }
    );
  }

  isExistingStudent(index: number): boolean {
    return !!this.studentGroup(index).get('id')!.value;
  }

  /**
   * Usable grades of the given stage
   */
  gradesFor(stageId: string | null | undefined): Grade[] {
    return stageId ? this.gradesByStage().get(stageId) ?? [] : [];
  }

  /**
   * Sections of the given grade
   */
  sectionsFor(gradeId: string | null | undefined): Section[] {
    return gradeId ? this.sectionsByGrade().get(gradeId) ?? [] : [];
  }

  /**
   * Title for a student card: its name in the current language, or a placeholder
   */
  studentTitle(index: number): string {
    const v = this.studentGroup(index).getRawValue();
    const name = this.currentLanguage() === 'ar' ? v.fullNameAr || v.fullNameEn : v.fullNameEn || v.fullNameAr;
    return name?.trim() || this.translate.instant('parents.newStudent');
  }

  /**
   * Submit the form
   */
  onSubmit(): void {
    if (this.isSaving()) {
      return;
    }

    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('parents.invalidForm'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const body = {
      fullNameAr: v.fullNameAr.trim(),
      fullNameEn: v.fullNameEn.trim(),
      phoneNumber: v.phoneNumber.trim(),
      email: this.normalizeEmail(v.email),
      address: v.address.trim(),
      students: (this.studentsArray.controls as FormGroup[]).map(g => this.toStudentDto(g))
    };

    if (this.isEditMode()) {
      const request: UpdateParentRequest = { id: this.parentId!, ...body };
      this.save(this.parentService.update(request), 'parents.updated', 'parents.updateFailed');
    } else {
      const request: CreateParentRequest = { id: this.emptyGuid, ...body };
      this.save(this.parentService.create(request), 'parents.created', 'parents.createFailed');
    }
  }

  /**
   * Map a student form group to exactly the backend ParentStudentDto shape
   */
  private toStudentDto(group: FormGroup): ParentStudentDto {
    const s = group.getRawValue();
    return {
      id: s.id || null,
      fullNameAr: s.fullNameAr.trim(),
      fullNameEn: s.fullNameEn.trim(),
      dateOfBirth: s.dateOfBirth,
      nationalId: s.nationalId.trim(),
      gender: s.gender,
      phoneNumber: s.phoneNumber.trim(),
      email: this.normalizeEmail(s.email),
      address: s.address.trim(),
      gradeId: s.gradeId,
      sectionId: s.sectionId,
      academicYearId: s.academicYearId
    };
  }

  /**
   * Trimmed email, or null when left empty (email is optional)
   */
  private normalizeEmail(value: string | null): string | null {
    const email = (value ?? '').trim();
    return email ? email : null;
  }

  /**
   * Shared create/update submission handling
   */
  private save(request$: ReturnType<ParentService['create']>, successKey: string, failKey: string): void {
    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(this.translate.instant(successKey));
            // Navigate back after 1 second (keep isSaving to block duplicate submits)
            setTimeout(() => this.router.navigate(['/management/parents']), 1000);
          } else {
            if (response.message === 'NationalIdAlreadyExists') {
              const ids = Array.isArray(response.errors) ? response.errors.join(', ') : '';
              this.errorMessage.set(this.translate.instant('parents.nationalIdExists', { ids }));
            } else {
              this.errorMessage.set(response.message || this.translate.instant(failKey));
            }
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error saving parent:', error);
          this.errorMessage.set(this.translate.instant(failKey));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/management/parents']);
  }

  getOptionName(item: { nameAr: string; nameEn: string }): string {
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  // ----- Validation helpers -----

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  isInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && control.touched);
  }

  studentHasError(index: number, fieldName: string, errorType: string): boolean {
    const control = this.studentGroup(index).get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  studentInvalid(index: number, fieldName: string): boolean {
    const control = this.studentGroup(index).get(fieldName);
    if (fieldName === 'nationalId' && this.isDuplicateNationalId(index)) {
      return true;
    }
    return !!(control && control.invalid && control.touched);
  }

  isDuplicateNationalId(index: number): boolean {
    const duplicates: number[] | undefined = this.studentsArray.errors?.['duplicateNationalId'];
    return !!duplicates?.includes(index);
  }

  /**
   * Whether a student card contains any visible error (used to highlight the card)
   */
  studentHasVisibleErrors(index: number): boolean {
    const group = this.studentGroup(index);
    return (group.invalid && group.touched) || this.isDuplicateNationalId(index);
  }
}
