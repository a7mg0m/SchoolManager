import { Component, OnInit, OnDestroy, computed, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ClinicVisitService } from '../../core/services/clinic-visit.service';
import { StudentService } from '../../core/services/student.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { ClinicVisit, CreateClinicVisitRequest, VISIT_OUTCOMES, VisitMedication, VisitOutcome } from '../../core/models/clinic-visit.model';
import { Student } from '../../core/models/student.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { clinicApiError } from '../clinic-errors';

@Component({
  selector: 'app-clinic-visit-save',
  standalone: false,
  styleUrl: './clinic-visit-save.component.scss',
  templateUrl: './clinic-visit-save.component.html',
})
export class ClinicVisitSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  students = signal<Student[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);

  // Stage -> grade -> section -> student (mirrors of the form values for the lists)
  stageId = signal('');
  gradeId = signal('');
  sectionId = signal('');

  /** Grades of the chosen stage */
  stageGrades = computed(() => this.grades().filter(g => g.stageId === this.stageId()));

  /** Sections of the chosen grade */
  gradeSections = computed(() => this.sections().filter(s => s.gradeId === this.gradeId()));

  /** Students of the chosen section, by name */
  sectionStudents = computed(() =>
    this.students()
      .filter(s => !!this.sectionId() && s.sectionId === this.sectionId())
      .sort((a, b) => this.studentLabel(a).localeCompare(this.studentLabel(b))));
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  visitId: string | null = null;

  readonly outcomes = VISIT_OUTCOMES;

  /** Page to go back to (the student's clinic file when opened from there) */
  private returnTo = '/clinic/visits';

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private visitService: ClinicVisitService,
    private studentService: StudentService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.visitId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.visitId);
    const query = this.activatedRoute.snapshot.queryParamMap;
    this.returnTo = query.get('returnTo') || this.returnTo;
    this.loadData(this.visitId ? null : query.get('studentId'));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.formBuilder.group({
      // Only to find the student (not sent)
      stageId: [''],
      gradeId: [''],
      sectionId: [''],
      studentId: ['', Validators.required],
      doctorName: ['', [Validators.required, Validators.maxLength(200)]],
      visitDate: [this.nowLocal(), Validators.required],
      complaint: ['', [Validators.required, Validators.maxLength(1000)]],
      temperature: [null, [Validators.min(30), Validators.max(45)]],
      weight: [null, [Validators.min(0), Validators.max(300)]],
      height: [null, [Validators.min(0), Validators.max(250)]],
      diagnosis: ['', Validators.maxLength(2000)],
      treatment: ['', Validators.maxLength(2000)],
      outcome: [VisitOutcome.ReturnedToClass, Validators.required],
      parentNotified: [false],
      notes: ['', Validators.maxLength(2000)],
      medications: this.formBuilder.array([])
    });

    // Choosing a stage / grade / section clears what comes after it
    this.form.get('stageId')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(id => {
      this.stageId.set(id || '');
      this.form.patchValue({ gradeId: '', sectionId: '', studentId: '' });
    });
    this.form.get('gradeId')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(id => {
      this.gradeId.set(id || '');
      this.form.patchValue({ sectionId: '', studentId: '' });
    });
    this.form.get('sectionId')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(id => {
      this.sectionId.set(id || '');
      this.form.patchValue({ studentId: '' });
    });
  }

  /** Stage, grade and section of [student], then the student (without clearing each other) */
  private selectStudent(student: Student): void {
    const section = this.sections().find(s => s.id === student.sectionId);
    const stageId = student.stageId || section?.stageId || '';
    this.stageId.set(stageId);
    this.gradeId.set(student.gradeId);
    this.sectionId.set(student.sectionId);
    this.form.patchValue(
      { stageId, gradeId: student.gradeId, sectionId: student.sectionId, studentId: student.id },
      { emitEvent: false });
  }

  get medications(): FormArray {
    return this.form.get('medications') as FormArray;
  }

  addMedication(medication?: VisitMedication): void {
    this.medications.push(this.formBuilder.group({
      medicineName: [medication?.medicineName ?? '', [Validators.required, Validators.maxLength(200)]],
      dose: [medication?.dose ?? '', [Validators.required, Validators.maxLength(200)]]
    }));
  }

  removeMedication(index: number): void {
    this.medications.removeAt(index);
  }

  /** Stages, grades, sections, students and (when editing) the visit; [studentId] = student chosen beforehand */
  private loadData(studentId: string | null): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      students: this.studentService.getAll(),
      visit: this.visitId ? this.visitService.getById(this.visitId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades, sections, students, visit }) => {
          const editedStudentId = visit?.data?.studentId;
          // Active ones (the edited visit's student is kept even when inactive)
          this.stages.set((stages.data || []).filter(s => s.isActive !== false || !!editedStudentId));
          this.grades.set((grades.data || []).filter(g => g.isActive !== false || !!editedStudentId));
          this.sections.set(sections.data || []);
          this.students.set((students.data || []).filter(s => s.isActive !== false || s.id === editedStudentId));
          if (visit) {
            if (visit.status === 200 && visit.data) {
              this.populateForm(visit.data);
            } else {
              this.errorMessage.set(visit.message || this.translate.instant('clinic.loadFailed'));
            }
          } else if (studentId) {
            // Opened from a student's clinic file
            const student = this.students().find(s => s.id === studentId);
            if (student) this.selectStudent(student);
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading clinic visit form:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(visit: ClinicVisit): void {
    // The student of a saved visit does not change
    const student = this.students().find(s => s.id === visit.studentId);
    if (student) this.selectStudent(student);
    for (const field of ['stageId', 'gradeId', 'sectionId', 'studentId']) {
      this.form.get(field)!.disable({ emitEvent: false });
    }

    this.form.patchValue({
      studentId: visit.studentId,
      doctorName: visit.doctorName,
      visitDate: (visit.visitDate || '').substring(0, 16),
      complaint: visit.complaint,
      temperature: visit.temperature,
      weight: visit.weight,
      height: visit.height,
      diagnosis: visit.diagnosis ?? '',
      treatment: visit.treatment ?? '',
      outcome: visit.outcome,
      parentNotified: visit.parentNotified,
      notes: visit.notes ?? ''
    });
    this.medications.clear();
    (visit.medications || []).forEach(m => this.addMedication(m));
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  studentLabel(student: Student): string {
    return this.currentLanguage() === 'ar' ? student.fullNameAr : student.fullNameEn;
  }

  localName(item: { nameAr: string; nameEn: string }): string {
    return this.currentLanguage() === 'ar' ? item.nameAr : item.nameEn;
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('clinic.fixErrors'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const number = (value: any): number | null => value === null || value === '' || value === undefined ? null : Number(value);
    const text = (value: any): string | null => String(value ?? '').trim() || null;
    const request: CreateClinicVisitRequest = {
      studentId: v.studentId,
      doctorName: String(v.doctorName).trim(),
      // Local date-time from the picker, sent without a time zone
      visitDate: `${v.visitDate}:00`,
      complaint: String(v.complaint).trim(),
      temperature: number(v.temperature),
      weight: number(v.weight),
      height: number(v.height),
      diagnosis: text(v.diagnosis),
      treatment: text(v.treatment),
      outcome: Number(v.outcome),
      parentNotified: !!v.parentNotified,
      notes: text(v.notes),
      medications: (v.medications || []).map((m: VisitMedication) => ({
        medicineName: String(m.medicineName).trim(),
        dose: String(m.dose).trim()
      }))
    };

    const { studentId, ...updateFields } = request;
    const request$ = this.isEditMode()
      ? this.visitService.update({ ...updateFields, id: this.visitId! })
      : this.visitService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(this.translate.instant('clinic.saved'));
          this.isSaving.set(false);
          // Navigate back after 1 second
          setTimeout(() => this.router.navigateByUrl(this.returnTo), 1000);
        } else {
          this.errorMessage.set(clinicApiError(this.translate, response.message, 'clinic.saveFailed'));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving clinic visit:', error);
        this.errorMessage.set(clinicApiError(this.translate, error?.error?.message, 'clinic.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigateByUrl(this.returnTo);
  }

  /** "yyyy-MM-ddTHH:mm" of now (local), for the date-time picker */
  private nowLocal(): string {
    const d = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
