import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SchoolEventService, extractApiErrors } from '../../core/services/school-event.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import {
  CreateSchoolEventRequest,
  EVENT_FOR_OPTIONS,
  EventFor,
  SchoolEvent
} from '../../core/models/school-event.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import {
  AudienceLookups,
  EMPTY_LOOKUPS,
  buildTargets,
  loadAudienceLookups,
  localizedFullName,
  localizedName,
  selectedIdsFromTargets,
  toHourMinute,
  todayIso
} from './event-audience';

/** One selectable row in the audience picker */
interface PickerItem {
  id: string;
  label: string;
  sub?: string;
  group?: string;
}

/** Field limits (SchoolEventConfiguration in the backend) */
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 2000;
const IMAGE_URL_MAX = 500;

@Component({
  selector: 'app-school-event-save',
  standalone: false,
  styleUrl: './school-event-save.component.scss',
  templateUrl: './school-event-save.component.html',
})
export class SchoolEventSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  eventId = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  apiErrors = signal<string[]>([]);
  successMessage = signal<string | null>(null);

  private lookups = signal<AudienceLookups>(EMPTY_LOOKUPS);

  // Audience
  eventFor = signal<EventFor>(EventFor.All);
  selectedIds = signal<Set<string>>(new Set());
  pickerSearch = signal('');
  pickerGradeFilter = signal('');
  /** Set after a submit attempt so the audience error only shows then */
  submitted = signal(false);

  // Mirrors of form values used by computed()s
  academicYearId = signal('');
  imageUrl = signal('');

  readonly eventForOptions = EVENT_FOR_OPTIONS;
  readonly EventFor = EventFor;
  readonly limits = { title: TITLE_MAX, description: DESCRIPTION_MAX, imageUrl: IMAGE_URL_MAX };

  academicYears = computed(() => this.lookups().academicYears);
  grades = computed(() => this.lookups().grades);

  semesters = computed(() => {
    const year = this.lookups().academicYears.find(y => y.id === this.academicYearId());
    return (year?.semesters ?? []).filter(s => !!s.id);
  });

  /** All rows for the current audience type */
  private pickerItems = computed<PickerItem[]>(() => {
    const l = this.lookups();
    const lang = this.currentLanguage();
    const stages = new Map(l.stages.map(s => [s.id, s]));
    const grades = new Map(l.grades.map(g => [g.id, g]));
    const sections = new Map(l.sections.map(s => [s.id, s]));

    switch (this.eventFor()) {
      case EventFor.Stage:
        return l.stages.map(s => ({
          id: s.id,
          label: localizedName(s, lang),
          sub: this.translate.instant('schoolEvents.gradesCount', { count: l.grades.filter(g => g.stageId === s.id).length })
        }));
      case EventFor.Grade:
        return l.grades.map(g => ({ id: g.id, label: localizedName(g, lang), group: localizedName(stages.get(g.stageId), lang) }));
      case EventFor.Section:
        return l.sections.map(s => ({ id: s.id, label: localizedName(s, lang), group: localizedName(grades.get(s.gradeId), lang) }));
      case EventFor.Teacher:
        return l.teachers
          .filter(t => t.isActive !== false || this.selectedIds().has(t.id))
          .map(t => ({ id: t.id, label: localizedFullName(t, lang), sub: t.phoneNumber }));
      case EventFor.Student:
        return l.students.map(s => ({
          id: s.id,
          label: localizedFullName(s, lang),
          sub: `${localizedName(grades.get(s.gradeId), lang)} / ${localizedName(sections.get(s.sectionId), lang)}`,
          group: s.gradeId
        }));
      default:
        return [];
    }
  });

  /** Rows after search (and grade filter for students) */
  visiblePickerItems = computed(() => {
    const term = this.pickerSearch().trim().toLowerCase();
    const gradeId = this.pickerGradeFilter();
    const isStudent = this.eventFor() === EventFor.Student;

    return this.pickerItems().filter(item => {
      if (isStudent && gradeId && item.group !== gradeId) return false;
      if (term && !`${item.label} ${item.sub ?? ''} ${item.group ?? ''}`.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  /** Visible rows grouped by their group label (students are not grouped) */
  pickerGroups = computed(() => {
    const groups = new Map<string, PickerItem[]>();
    const grouped = this.eventFor() === EventFor.Grade || this.eventFor() === EventFor.Section;
    for (const item of this.visiblePickerItems()) {
      const key = grouped ? item.group || '-' : '';
      groups.set(key, [...(groups.get(key) ?? []), item]);
    }
    return [...groups].map(([label, items]) => ({ label, items }));
  });

  allVisibleSelected = computed(() => {
    const visible = this.visiblePickerItems();
    const selected = this.selectedIds();
    return visible.length > 0 && visible.every(i => selected.has(i.id));
  });

  /** Number of target rows that will be saved (stages expand to their grades) */
  targetCount = computed(() => buildTargets(this.eventFor(), this.selectedIds(), this.lookups()).length);

  audienceInvalid = computed(() => this.eventFor() !== EventFor.All && this.targetCount() === 0);

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private schoolEventService: SchoolEventService,
    private academicYearService: AcademicYearService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private teacherService: TeacherService,
    private studentService: StudentService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.buildForm();

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.eventId.set(this.activatedRoute.snapshot.paramMap.get('id'));
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private buildForm(): void {
    this.form = this.formBuilder.group(
      {
        title: ['', [Validators.required, Validators.maxLength(TITLE_MAX), this.notBlank]],
        description: ['', [Validators.required, Validators.maxLength(DESCRIPTION_MAX), this.notBlank]],
        eventDate: [todayIso(), Validators.required],
        startTime: ['09:00', Validators.required],
        endTime: ['10:00', Validators.required],
        imageUrl: ['', [Validators.maxLength(IMAGE_URL_MAX), Validators.pattern(/^https?:\/\/\S+$/i)]],
        academicYearId: ['', Validators.required],
        semesterId: ['', Validators.required]
      },
      { validators: this.endAfterStart }
    );

    this.form.get('academicYearId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(id => {
        this.academicYearId.set(id || '');
        // Keep the semester only if it belongs to the new year
        const semesterId = this.form.get('semesterId')!.value;
        if (semesterId && !this.semesters().some(s => s.id === semesterId)) {
          this.form.get('semesterId')!.setValue('');
        }
        this.autoPickSemester();
      });

    this.form.get('eventDate')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.autoPickSemester());

    this.form.get('imageUrl')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(url => this.imageUrl.set(url || ''));
  }

  private notBlank(control: AbstractControl): ValidationErrors | null {
    return typeof control.value === 'string' && control.value.length > 0 && !control.value.trim() ? { required: true } : null;
  }

  private endAfterStart(group: AbstractControl): ValidationErrors | null {
    const start = group.get('startTime')?.value;
    const end = group.get('endTime')?.value;
    return start && end && end <= start ? { endBeforeStart: true } : null;
  }

  private loadData(): void {
    this.isLoading.set(true);
    const id = this.eventId();

    forkJoin({
      lookups: loadAudienceLookups({
        academicYearService: this.academicYearService,
        stageService: this.stageService,
        gradeService: this.gradeService,
        sectionService: this.sectionService,
        teacherService: this.teacherService,
        studentService: this.studentService
      }),
      event: id ? this.schoolEventService.getById(id) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ lookups, event }) => {
          this.lookups.set(lookups);
          if (id) {
            if (event?.status === 200 && event.data) {
              this.patchForm(event.data);
            } else {
              this.errorMessage.set(event?.message || this.translate.instant('schoolEvents.loadEventFailed'));
            }
          } else {
            this.setDefaultAcademicYear(lookups.academicYears);
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading event form:', error);
          this.errorMessage.set(
            this.translate.instant(id ? 'schoolEvents.loadEventFailed' : 'schoolEvents.loadLookupsFailed')
          );
          this.isLoading.set(false);
        }
      });
  }

  private setDefaultAcademicYear(years: AcademicYear[]): void {
    const today = todayIso();
    const year =
      years.find(y => y.isActive && (y.startDate || '') <= today && today <= (y.endDate || '')) ??
      years.find(y => y.isActive) ??
      years[0];
    if (year) this.form.patchValue({ academicYearId: year.id });
  }

  /** Pick the semester containing the event date when none is chosen yet */
  private autoPickSemester(): void {
    if (this.form.get('semesterId')!.value) return;
    const date = this.form.get('eventDate')!.value || todayIso();
    const semester = this.semesters().find(s => (s.startDate || '').substring(0, 10) <= date && date <= (s.endDate || '').substring(0, 10));
    if (semester?.id) this.form.get('semesterId')!.setValue(semester.id, { emitEvent: false });
  }

  private patchForm(event: SchoolEvent): void {
    this.form.patchValue({
      title: event.title,
      description: event.description,
      eventDate: (event.eventDate || '').substring(0, 10),
      startTime: toHourMinute(event.startTime),
      endTime: toHourMinute(event.endTime),
      imageUrl: event.imageUrl || '',
      academicYearId: event.academicYearId
    });
    // After the year so the semester list is already filtered
    this.form.patchValue({ semesterId: event.semesterId });

    this.eventFor.set(event.eventFor || EventFor.All);
    this.selectedIds.set(selectedIdsFromTargets(event.eventFor, event.targets ?? [], this.lookups()));
  }

  // ---------------------------------------------------------------------------
  // Audience picker
  // ---------------------------------------------------------------------------

  setEventFor(value: EventFor): void {
    if (value === this.eventFor()) return;
    this.eventFor.set(value);
    this.selectedIds.set(new Set());
    this.pickerSearch.set('');
    this.pickerGradeFilter.set('');
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  toggle(id: string): void {
    const next = new Set(this.selectedIds());
    next.has(id) ? next.delete(id) : next.add(id);
    this.selectedIds.set(next);
  }

  toggleAllVisible(): void {
    const next = new Set(this.selectedIds());
    const visible = this.visiblePickerItems();
    if (this.allVisibleSelected()) {
      visible.forEach(i => next.delete(i.id));
    } else {
      visible.forEach(i => next.add(i.id));
    }
    this.selectedIds.set(next);
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  onPickerSearch(event: Event): void {
    this.pickerSearch.set((event.target as HTMLInputElement).value);
  }

  onPickerGradeFilter(event: Event): void {
    this.pickerGradeFilter.set((event.target as HTMLSelectElement).value);
  }

  gradeName(id: string): string {
    return localizedName(this.lookups().grades.find(g => g.id === id), this.currentLanguage());
  }

  // ---------------------------------------------------------------------------
  // Submit
  // ---------------------------------------------------------------------------

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  get endBeforeStart(): boolean {
    return this.form.hasError('endBeforeStart') && !!(this.form.get('endTime')?.touched || this.form.get('startTime')?.touched);
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.apiErrors.set([]);

    if (this.form.invalid || this.audienceInvalid()) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('schoolEvents.invalidForm'));
      return;
    }

    const v = this.form.value;
    const request: CreateSchoolEventRequest = {
      title: String(v.title).trim(),
      description: String(v.description).trim(),
      eventDate: v.eventDate,
      startTime: `${v.startTime}:00`,
      endTime: `${v.endTime}:00`,
      imageUrl: (v.imageUrl || '').trim() || null,
      eventFor: this.eventFor(),
      academicYearId: v.academicYearId,
      semesterId: v.semesterId,
      targets: buildTargets(this.eventFor(), this.selectedIds(), this.lookups())
    };

    const id = this.eventId();
    const request$ = id
      ? this.schoolEventService.update({ ...request, id })
      : this.schoolEventService.create(request);
    const successKey = id ? 'schoolEvents.updated' : 'schoolEvents.created';
    const failKey = id ? 'schoolEvents.updateFailed' : 'schoolEvents.createFailed';

    this.isSaving.set(true);
    this.errorMessage.set(null);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(this.translate.instant(successKey));
          // Keep isSaving true to block double submits while navigating away
          setTimeout(() => this.router.navigate(['/notification/school-events']), 800);
        } else {
          this.errorMessage.set(response.message || this.translate.instant(failKey));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving school event:', error);
        // 400 ProblemDetails from the validation pipeline → translated messages
        const keys = extractApiErrors(error);
        this.apiErrors.set(keys.map(k => this.translateApiError(k)));
        this.errorMessage.set(this.translate.instant(failKey));
        this.isSaving.set(false);
      }
    });
  }

  private translateApiError(key: string): string {
    const translationKey = `schoolEvents.errors.${key}`;
    const text = this.translate.instant(translationKey);
    return text === translationKey ? key : text;
  }

  cancel(): void {
    this.router.navigate(['/notification/school-events']);
  }
}
