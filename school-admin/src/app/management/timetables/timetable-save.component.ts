import { Component, OnInit, OnDestroy, TemplateRef, ViewChild, effect, signal } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AcademicContextService } from '../../core/services/academic-context.service';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TimetableService } from '../../core/services/timetable.service';
import { GradeService } from '../../core/services/grade.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { TeacherService } from '../../core/services/teacher.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { TranslateService } from '@ngx-translate/core';
import { Timetable, TimetableSlot, TimetableWorkDay, CreateTimetableRequest, UpdateTimetableRequest } from '../../core/models/timetable.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { AcademicYear, AcademicYearSemester, AcademicYearSemesters } from '../../core/models/academic-year.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Teacher } from '../../core/models/teacher.model';

/** .NET DayOfWeek values (0 = Sunday) */
export const WEEK_DAYS: { value: number; key: string }[] = [
  { value: 0, key: 'timetables.days.sunday' },
  { value: 1, key: 'timetables.days.monday' },
  { value: 2, key: 'timetables.days.tuesday' },
  { value: 3, key: 'timetables.days.wednesday' },
  { value: 4, key: 'timetables.days.thursday' },
  { value: 5, key: 'timetables.days.friday' },
  { value: 6, key: 'timetables.days.saturday' }
];

/** Slot-level validator: end time must be after start time */
/** A work day column: break start / end together and in order */
function validBreak(group: AbstractControl): ValidationErrors | null {
  const start = group.get('breakStart')?.value;
  const end = group.get('breakEnd')?.value;
  if (!start && !end) return null;
  if (!start || !end) return { breakIncomplete: true };
  return end <= start ? { breakEndBeforeStart: true } : null;
}

/** "HH:mm" → minutes since midnight */
function toMinutes(value: string): number {
  const [h, m] = (value || '').split(':').map(Number);
  return (h || 0) * 60 + (m || 0);
}

/** minutes since midnight → "HH:mm" */
function fromMinutes(total: number): string {
  const h = Math.floor(total / 60) % 24;
  const m = total % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

/** Defaults of a new timetable: Sunday → Thursday, 7 periods of 45 minutes */
const DEFAULT_WORK_DAYS = [0, 1, 2, 3, 4];

function endTimeAfterStartTime(group: AbstractControl): ValidationErrors | null {
  const start = group.get('startTime')?.value;
  const end = group.get('endTime')?.value;
  if (start && end && start >= end) {
    return { endTimeMustBeGreater: true };
  }
  return null;
}

/** Array-level validator: slots on the same day + section must not overlap */
function noOverlappingSlots(array: AbstractControl): ValidationErrors | null {
  const slots = (array as FormArray).controls.map(c => c.value);
  for (let i = 0; i < slots.length; i++) {
    for (let j = i + 1; j < slots.length; j++) {
      const a = slots[i];
      const b = slots[j];
      if (
        a.day !== null && a.day === b.day &&
        a.sectionId && a.sectionId === b.sectionId &&
        a.startTime && a.endTime && b.startTime && b.endTime &&
        a.startTime < b.endTime && b.startTime < a.endTime
      ) {
        return { overlappingSlots: { first: i + 1, second: j + 1 } };
      }
    }
  }
  return null;
}

/** Array-level validator: a teacher cannot give two lessons at the same time (in two sections) */
function noTeacherClash(array: AbstractControl): ValidationErrors | null {
  const slots = (array as FormArray).controls.map(c => c.value);
  for (let i = 0; i < slots.length; i++) {
    for (let j = i + 1; j < slots.length; j++) {
      const a = slots[i];
      const b = slots[j];
      if (
        a.teacherId && a.teacherId === b.teacherId &&
        a.day !== null && String(a.day) === String(b.day) &&
        a.startTime && a.endTime && b.startTime && b.endTime &&
        a.startTime < b.endTime && b.startTime < a.endTime
      ) {
        return { teacherClash: { first: i + 1, second: j + 1, teacherId: a.teacherId, day: Number(a.day), start: a.startTime } };
      }
    }
  }
  return null;
}

@Component({
  selector: 'app-timetable-save',
  standalone: false,
  styleUrl: './timetable-save.component.scss',
  templateUrl: './timetable-save.component.html',
})
export class TimetableSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  timetableId: string | null = null;

  // Lookups
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  /** Other timetables whose week settings can be copied (label: stage · grade · year · semester) */
  weekSources = signal<{ id: string; label: string }[]>([]);
  copyingWeek = signal(false);
  sections = signal<Section[]>([]);
  subjects = signal<SchoolSubject[]>([]);
  teachers = signal<Teacher[]>([]);

  // Grade-filtered lookups (recomputed when grade changes)
  gradeSections = signal<Section[]>([]);
  gradeSubjects = signal<SchoolSubject[]>([]);

  readonly weekDays = WEEK_DAYS;

  /** "Generate periods" for one class of the grade, or '' = every class */
  generateSectionId = signal<string>('');

  /** What "Generate periods" is about to do (shown in the confirmation modal) */
  generatePlan = signal<{ replaced: number; created: number; days: string[]; sections: string[] } | null>(null);

  @ViewChild('generateModal') private generateModal?: TemplateRef<unknown>;

  /** Sections whose periods are folded (all open by default) */
  collapsedSections = signal<Set<string>>(new Set());

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private timetableService: TimetableService,
    private gradeService: GradeService,
    private academicYearService: AcademicYearService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private modalService: NgbModal,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    public academic: AcademicContextService
  ) {
    this.followHeader();
  }

  ngOnInit(): void {
    this.initializeForm();
    this.timetableId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.timetableId);
    this.lockAcademicFields();
    this.academic.load();

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    // Changing the stage clears a grade that doesn't belong to it
    this.form.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(stageId => {
        const gradeId = this.form.get('gradeId')!.value;
        if (gradeId && !this.stageGrades.some(g => g.id === gradeId)) {
          this.form.get('gradeId')!.setValue('');
        }
      });

    this.form.get('gradeId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(gradeId => this.onGradeChanged(gradeId));

    // Another academic year: keep the semester only when it belongs to it, else its current semester
    this.form.get('academicYearId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        const semesterId = this.form.get('semesterId')!.value;
        if (!semesterId || !this.yearSemesters.some(s => s.id === semesterId)) {
          this.form.get('semesterId')!.setValue(this.currentSemesterId ?? '');
        }
      });

    this.loadLookups();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  private initializeForm(): void {
    this.form = this.formBuilder.group({
      stageId: ['', Validators.required],
      gradeId: ['', Validators.required],
      academicYearId: ['', Validators.required],
      semesterId: ['', Validators.required],
      // The week: one start time for every day, then one column per day
      dayStartTime: ['07:30'],
      workDays: this.formBuilder.array(WEEK_DAYS.map(d => this.createWorkDayGroup({
        day: d.value,
        isWorkDay: DEFAULT_WORK_DAYS.includes(d.value),
        breakStart: null,
        breakEnd: null,
        slotDuration: 45,
        slotCount: 7
      }))),
      slots: this.formBuilder.array([], { validators: [noOverlappingSlots, noTeacherClash] })
    });
  }

  get workDays(): FormArray {
    return this.form.get('workDays') as FormArray;
  }

  private createWorkDayGroup(day: TimetableWorkDay): FormGroup {
    const group = this.formBuilder.group(
      {
        day: [day.day],
        isWorkDay: [day.isWorkDay],
        breakStart: [this.toInputTime(day.breakStart)],
        breakEnd: [this.toInputTime(day.breakEnd)],
        slotDuration: [day.slotDuration || 45, [Validators.required, Validators.min(5), Validators.max(240)]],
        slotCount: [day.slotCount || 7, [Validators.required, Validators.min(1), Validators.max(20)]]
      },
      { validators: validBreak }
    );

    // A day off: its settings are kept but not checked
    const apply = (on: boolean) => {
      for (const name of ['breakStart', 'breakEnd', 'slotDuration', 'slotCount']) {
        const c = group.get(name)!;
        on ? c.enable({ emitEvent: false }) : c.disable({ emitEvent: false });
      }
    };
    apply(day.isWorkDay);
    group.get('isWorkDay')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(on => apply(!!on));
    return group;
  }

  workDayControl(index: number, name: string): AbstractControl {
    return this.workDays.at(index).get(name)!;
  }

  isWorkDay(index: number): boolean {
    return !!this.workDayControl(index, 'isWorkDay').value;
  }

  hasWorkDayError(index: number, error: string): boolean {
    const group = this.workDays.at(index);
    return group.hasError(error) && (group.dirty || group.touched);
  }

  /** The periods of one work day: [start, end] "HH:mm", one after another, jumping over the break */
  dayPeriods(index: number): [string, string][] {
    const v = this.workDays.at(index).getRawValue();
    const start = this.form.get('dayStartTime')!.value;
    const duration = Number(v.slotDuration);
    const count = Number(v.slotCount);
    if (!v.isWorkDay || !start || !(duration >= 5) || !(count >= 1) || count > 20) return [];

    const breakStart = v.breakStart && v.breakEnd ? toMinutes(v.breakStart) : null;
    const breakEnd = v.breakStart && v.breakEnd ? toMinutes(v.breakEnd) : null;
    const periods: [string, string][] = [];
    let t = toMinutes(start);
    for (let i = 0; i < count; i++) {
      // A period that would run into the break starts after it
      if (breakStart !== null && breakEnd !== null && t < breakEnd && t + duration > breakStart) t = Math.max(t, breakEnd);
      periods.push([fromMinutes(t), fromMinutes(t + duration)]);
      t += duration;
    }
    return periods;
  }

  /** When the last period of a work day ends ("" for a day off) */
  dayEnd(index: number): string {
    const periods = this.dayPeriods(index);
    return periods.length ? periods[periods.length - 1][1] : '';
  }

  /** Periods of every work day: the grade's classes (or the chosen one), with times; subject / teacher are chosen after */
  generatePeriods(): void {
    const sections = this.generateSectionId()
      ? this.gradeSections().filter(s => s.id === this.generateSectionId())
      : this.gradeSections();
    if (!sections.length || !this.form.get('dayStartTime')!.value) return;

    const workDays = this.workDays.controls.map((_, i) => i).filter(i => this.isWorkDay(i));
    const days = new Set(workDays.map(i => this.workDays.at(i).get('day')!.value));
    const sectionIds = new Set(sections.map(s => s.id));
    const replaced = this.slots.controls.filter(c => days.has(c.value.day) && sectionIds.has(c.value.sectionId));

    // Existing periods would be replaced: ask first (modal), otherwise generate right away
    if (replaced.length && this.generateModal) {
      this.generatePlan.set({
        replaced: replaced.length,
        created: sections.length * workDays.reduce((sum, i) => sum + this.dayPeriods(i).length, 0),
        days: workDays.map(i => this.translate.instant(WEEK_DAYS[i].key)),
        sections: sections.map(s => this.getName(s))
      });
      this.modalService
        .open(this.generateModal, { centered: true, backdrop: 'static', windowClass: 'generate-modal' })
        .result.then(
          ok => { if (ok) this.applyGenerate(sections, workDays, replaced); },
          () => undefined
        );
      return;
    }
    this.applyGenerate(sections, workDays, replaced);
  }

  /** Replaces the periods of the work days of these sections with freshly timed ones */
  private applyGenerate(sections: Section[], workDays: number[], replaced: AbstractControl[]): void {

    // Remove the periods being replaced (from the end, so indexes stay right)
    for (let i = this.slots.length - 1; i >= 0; i--) {
      if (replaced.includes(this.slots.at(i))) this.slots.removeAt(i);
    }

    for (const section of sections) {
      for (const i of workDays) {
        const day = this.workDays.at(i).get('day')!.value;
        this.dayPeriods(i).forEach(([start, end], n) => {
          this.slots.push(this.createSlotGroup({
            day,
            nameAr: `الحصة ${n + 1}`,
            nameEn: `Period ${n + 1}`,
            startTime: this.fromInputTime(start),
            endTime: this.fromInputTime(end),
            sectionId: section.id
          }));
        });
      }
    }
    this.sortSlots();
    this.successMessage.set(this.translate.instant('timetables.generated'));
  }

  get slots(): FormArray {
    return this.form.get('slots') as FormArray;
  }

  private createSlotGroup(slot?: Partial<TimetableSlot>): FormGroup {
    const group = this.formBuilder.group(
      {
        id: [slot?.id ?? null],
        day: [slot?.day ?? null, Validators.required],
        nameAr: [slot?.nameAr ?? '', [Validators.required, Validators.maxLength(100)]],
        nameEn: [slot?.nameEn ?? '', [Validators.required, Validators.maxLength(100)]],
        startTime: [this.toInputTime(slot?.startTime), Validators.required],
        endTime: [this.toInputTime(slot?.endTime), Validators.required],
        sectionId: [slot?.sectionId ?? '', Validators.required],
        subjectId: [slot?.subjectId ?? '', Validators.required],
        teacherId: [slot?.teacherId ?? '', Validators.required]
      },
      { validators: endTimeAfterStartTime }
    );

    // When the subject changes, clear the teacher if they don't teach it
    group.get('subjectId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(subjectId => {
        const teacherId = group.get('teacherId')!.value;
        if (teacherId && !this.getTeachersForSubject(subjectId).some(t => t.id === teacherId)) {
          group.get('teacherId')!.setValue('');
        }
      });

    return group;
  }

  /**
   * Periods grouped by section, in the grade's section order (periods without a section last).
   * indexes point into the slots FormArray.
   */
  slotGroups(): { sectionId: string; name: string; indexes: number[]; incomplete: number; invalid: boolean }[] {
    const bySection = new Map<string, number[]>();
    this.slots.controls.forEach((c, i) => {
      const id = c.get('sectionId')!.value || '';
      bySection.set(id, [...(bySection.get(id) ?? []), i]);
    });

    const order = this.gradeSections().map(s => s.id);
    const ids = [...bySection.keys()].sort((a, b) => {
      const ia = a ? order.indexOf(a) : Number.MAX_SAFE_INTEGER;
      const ib = b ? order.indexOf(b) : Number.MAX_SAFE_INTEGER;
      return (ia < 0 ? order.length : ia) - (ib < 0 ? order.length : ib);
    });

    return ids.map(id => {
      const indexes = bySection.get(id)!;
      const section = this.gradeSections().find(s => s.id === id) ?? this.sections().find(s => s.id === id);
      return {
        sectionId: id,
        name: id ? (section ? this.getName(section) : '-') : this.translate.instant('timetables.noSection'),
        indexes,
        // Periods still without a subject or a teacher (e.g. just generated)
        incomplete: indexes.filter(i => !this.slots.at(i).get('subjectId')!.value || !this.slots.at(i).get('teacherId')!.value).length,
        invalid: indexes.some(i => this.slots.at(i).invalid && this.slots.at(i).touched)
      };
    });
  }

  isSectionCollapsed(sectionId: string): boolean {
    return this.collapsedSections().has(sectionId);
  }

  toggleSection(sectionId: string): void {
    const next = new Set(this.collapsedSections());
    next.has(sectionId) ? next.delete(sectionId) : next.add(sectionId);
    this.collapsedSections.set(next);
  }

  expandAllSections(): void {
    this.collapsedSections.set(new Set());
  }

  collapseAllSections(): void {
    this.collapsedSections.set(new Set(this.slotGroups().map(g => g.sectionId)));
  }

  /** A new period in one section (after its last period; the section opens) */
  addSlotToSection(sectionId: string): void {
    const own = this.slots.controls.filter(c => c.get('sectionId')!.value === sectionId);
    const last = own.length ? own[own.length - 1].value : null;
    this.slots.push(this.createSlotGroup({
      day: last?.day ?? null,
      sectionId,
      startTime: last?.endTime ? this.fromInputTime(last.endTime) : undefined
    }));
    const next = new Set(this.collapsedSections());
    next.delete(sectionId);
    this.collapsedSections.set(next);
  }

  addSlot(): void {
    // Pre-fill day/section from the last slot to make bulk entry faster
    const last = this.slots.length ? this.slots.at(this.slots.length - 1).value : null;
    this.slots.push(this.createSlotGroup({
      day: last?.day ?? null,
      sectionId: last?.sectionId ?? '',
      startTime: last?.endTime ? this.fromInputTime(last.endTime) : undefined
    }));
  }

  duplicateSlot(index: number): void {
    const value = this.slots.at(index).value;
    this.slots.insert(index + 1, this.createSlotGroup({
      ...value,
      id: null,
      startTime: this.fromInputTime(value.startTime),
      endTime: this.fromInputTime(value.endTime)
    }));
  }

  removeSlot(index: number): void {
    this.slots.removeAt(index);
  }

  /** Sort slots by day then start time (purely visual) */
  sortSlots(): void {
    const sorted = [...this.slots.controls].sort((a, b) => {
      const av = a.value, bv = b.value;
      if (av.day !== bv.day) return (av.day ?? 99) - (bv.day ?? 99);
      return (av.startTime || '').localeCompare(bv.startTime || '');
    });
    this.slots.clear();
    sorted.forEach(c => this.slots.push(c));
  }

  // ---------------------------------------------------------------------------
  // Loading
  // ---------------------------------------------------------------------------

  private loadLookups(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getActiveGrades(),
      academicYears: this.academicYearService.getAll(),
      sections: this.sectionService.getAll(),
      subjects: this.subjectService.getAll(),
      teachers: this.teacherService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages.set(res.stages.data || []);
          this.grades.set(res.grades.data || []);
          this.academicYears.set(res.academicYears.data || []);
          this.loadWeekSources();
          this.sections.set(res.sections.data || []);
          this.subjects.set(res.subjects.data || []);
          this.teachers.set((res.teachers.data || []).filter(t => t.isActive));

          if (this.timetableId) {
            this.loadTimetable(this.timetableId);
          } else {
            // The header's academic year / semester
            this.form.patchValue({ academicYearId: this.academic.yearId(), semesterId: this.academic.semesterId() });
            this.isLoading.set(false);
          }
        },
        error: (error) => {
          console.error('Error loading lookups:', error);
          this.errorMessage.set('Failed to load required data. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private loadTimetable(id: string): void {
    this.timetableService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(response.message || 'Failed to load timetable');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading timetable:', error);
          this.errorMessage.set('Failed to load timetable. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(timetable: Timetable): void {
    const stageId = timetable.stageId
      || this.grades().find(g => g.id === timetable.gradeId)?.stageId
      || '';
    this.form.patchValue({
      stageId,
      gradeId: timetable.gradeId,
      academicYearId: timetable.academicYearId,
      // Timetables saved before semesters have none: suggest the year's current semester
      semesterId: timetable.semesterId
        || AcademicYearSemesters.current(this.academicYears().find(y => y.id === timetable.academicYearId))?.id
        || ''
    }, { emitEvent: false });
    this.applyGradeFilter(timetable.gradeId);

    // The saved week (timetables saved before it keep the defaults)
    this.applyWeek(timetable);

    this.slots.clear();
    [...(timetable.slots || [])]
      .sort((a, b) => a.day - b.day || a.startTime.localeCompare(b.startTime))
      .forEach(slot => this.slots.push(this.createSlotGroup(slot)));
  }

  /** Day start and work days (breaks, period length / count) of a timetable; false when it has none saved */
  private applyWeek(timetable: Timetable): boolean {
    if (!timetable.workDays?.length) return false;
    this.form.get('dayStartTime')!.setValue(this.toInputTime(timetable.dayStartTime) || '07:30', { emitEvent: false });
    // Update the day groups in place: the work days table on the page stays bound to these controls
    // (replacing them would leave the shown inputs on the old ones, e.g. when copying into a new timetable)
    WEEK_DAYS.forEach((d, i) => {
      const saved = timetable.workDays!.find(w => Number(w.day) === d.value)
        ?? { day: d.value, isWorkDay: false, breakStart: null, breakEnd: null, slotDuration: 45, slotCount: 7 };
      const group = this.workDays.at(i);
      if (!group) {
        this.workDays.push(this.createWorkDayGroup(saved));
        return;
      }
      // isWorkDay first: its change enables / disables the day's other fields
      group.get('isWorkDay')!.setValue(!!saved.isWorkDay);
      group.patchValue({
        day: d.value,
        breakStart: this.toInputTime(saved.breakStart),
        breakEnd: this.toInputTime(saved.breakEnd),
        slotDuration: saved.slotDuration || 45,
        slotCount: saved.slotCount || 7
      });
    });
    this.workDays.markAsDirty();
    return true;
  }

  /** The other timetables (labelled) for "Copy week from…" */
  private loadWeekSources(): void {
    this.timetableService.getAll().pipe(takeUntil(this.destroy$)).subscribe({
      next: res => {
        const name = (item: { nameAr?: string; nameEn?: string } | undefined) => this.getName(item);
        this.weekSources.set((res.data || [])
          .filter(t => t.id !== this.timetableId)
          .map(t => {
            const grade = this.grades().find(g => g.id === t.gradeId);
            const stage = this.stages().find(s => s.id === (t.stageId || grade?.stageId));
            const year = this.academicYears().find(y => y.id === t.academicYearId);
            const semester = AcademicYearSemesters.ordered(year).find(s => s.id === t.semesterId);
            return {
              id: t.id,
              label: [name(stage), name(grade), year?.name, semester ? name(semester as any) || (semester as any).name : '']
                .filter(Boolean).join(' · ')
            };
          })
          .sort((a, b) => a.label.localeCompare(b.label)));
      }
    });
  }

  /** Copies the chosen timetable's week settings into this one (periods / teachers are not touched) */
  copyWeekFrom(select: HTMLSelectElement): void {
    const id = select.value;
    if (!id) return;
    this.copyingWeek.set(true);
    this.timetableService.getById(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: res => {
        this.copyingWeek.set(false);
        select.value = '';
        const source = res.data;
        if (source && this.applyWeek(source)) {
          this.successMessage.set(this.translate.instant('timetables.weekCopied', {
            from: this.weekSources().find(s => s.id === id)?.label ?? ''
          }));
        } else {
          this.errorMessage.set(this.translate.instant('timetables.weekCopyEmpty'));
        }
      },
      error: () => {
        this.copyingWeek.set(false);
        select.value = '';
        this.errorMessage.set(this.translate.instant('timetables.weekCopyFailed'));
      }
    });
  }

  // ---------------------------------------------------------------------------
  // Grade-dependent filtering
  // ---------------------------------------------------------------------------

  private onGradeChanged(gradeId: string): void {
    this.applyGradeFilter(gradeId);

    // Clear slot sections/subjects that don't belong to the new grade
    const sectionIds = new Set(this.gradeSections().map(s => s.id));
    const subjectIds = new Set(this.gradeSubjects().map(s => s.id));
    this.slots.controls.forEach(slot => {
      if (!sectionIds.has(slot.get('sectionId')!.value)) slot.get('sectionId')!.setValue('');
      if (!subjectIds.has(slot.get('subjectId')!.value)) slot.get('subjectId')!.setValue('');
    });
  }

  private applyGradeFilter(gradeId: string): void {
    this.gradeSections.set(this.sections().filter(s => s.gradeId === gradeId));
    this.gradeSubjects.set(this.subjects().filter(s => s.gradeId === gradeId));
  }

  /** Teachers assigned to the subject; falls back to all teachers if the subject has none */
  getTeachersForSubject(subjectId: string): Teacher[] {
    const subject = this.subjects().find(s => s.id === subjectId);
    if (!subject || !subject.teacherIds?.length) {
      return this.teachers();
    }
    return this.teachers().filter(t => subject.teacherIds.includes(t.id));
  }

// ---------------------------------------------------------------------------
  // Academic year / semester from the header
  // ---------------------------------------------------------------------------

  /** New record: the header's year and semester (kept in step when the header changes) */
  private followHeader(): void {
    effect(() => {
      const academicYearId = this.academic.yearId();
      const semesterId = this.academic.semesterId();
      if (this.form && !this.isEditMode()) {
        this.form.patchValue({ academicYearId, semesterId });
      }
    });
  }

  /** Year / semester are not edited here: new records take the header's, saved ones keep theirs */
  private lockAcademicFields(): void {
    this.form.get('academicYearId')!.disable({ emitEvent: false });
    this.form.get('semesterId')!.disable({ emitEvent: false });
  }

  /** The header has no year / semester to save with */
  private missingAcademicContext(value: { academicYearId?: string; semesterId?: string }): boolean {
    return !value.academicYearId || !value.semesterId;
  }

  /** Semesters of the selected academic year (in date order) */
  get yearSemesters(): AcademicYearSemester[] {
    const yearId = this.form?.get('academicYearId')?.value;
    const year = this.academicYears().find(y => y.id === yearId);
    return AcademicYearSemesters.ordered(year);
  }

  /** The selected year's current semester (today inside it, else an active one, else the first) */
  get currentSemesterId(): string | undefined {
    const yearId = this.form?.get('academicYearId')?.value;
    return AcademicYearSemesters.current(this.academicYears().find(y => y.id === yearId))?.id;
  }

  /** Grades of the selected stage for the grade <select> */
  get stageGrades(): Grade[] {
    const stageId = this.form?.get('stageId')?.value;
    return stageId ? this.grades().filter(g => g.stageId === stageId) : [];
  }

  // ---------------------------------------------------------------------------
  // Submit
  // ---------------------------------------------------------------------------

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Please fix form errors before submitting');
      return;
    }

    if (this.missingAcademicContext(this.form.getRawValue())) {
      this.errorMessage.set('Choose the academic year and semester in the header first.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const slots: TimetableSlot[] = value.slots.map((s: any) => ({
      id: s.id || null,
      day: Number(s.day),
      nameAr: s.nameAr,
      nameEn: s.nameEn,
      startTime: this.fromInputTime(s.startTime),
      endTime: this.fromInputTime(s.endTime),
      sectionId: s.sectionId,
      subjectId: s.subjectId,
      teacherId: s.teacherId,
      gradeId: value.gradeId
    }));

    const dayStartTime = value.dayStartTime ? this.fromInputTime(value.dayStartTime) : null;
    const workDays: TimetableWorkDay[] = value.workDays.map((d: any) => ({
      day: Number(d.day),
      isWorkDay: !!d.isWorkDay,
      breakStart: d.breakStart ? this.fromInputTime(d.breakStart) : null,
      breakEnd: d.breakEnd ? this.fromInputTime(d.breakEnd) : null,
      slotDuration: Number(d.slotDuration) || 0,
      slotCount: Number(d.slotCount) || 0
    }));

    const request$ = this.isEditMode()
      ? this.timetableService.update({
          id: this.timetableId!,
          stageId: value.stageId,
          gradeId: value.gradeId,
          academicYearId: value.academicYearId,
          semesterId: value.semesterId,
          slots,
          dayStartTime,
          workDays
        } as UpdateTimetableRequest)
      : this.timetableService.create({
          stageId: value.stageId,
          gradeId: value.gradeId,
          academicYearId: value.academicYearId,
          semesterId: value.semesterId,
          slots: slots.map(({ id, ...rest }) => rest),
          dayStartTime,
          workDays
        } as CreateTimetableRequest);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(
            response.message || (this.isEditMode() ? 'Timetable updated successfully' : 'Timetable created successfully')
          );
          this.isSaving.set(false);
          setTimeout(() => this.router.navigate(['/management/timetables']), 1000);
        } else {
          this.errorMessage.set(response.message || 'Failed to save timetable');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving timetable:', error);
        const body = error?.error;
        this.errorMessage.set(body?.message === 'TeacherClash' && Array.isArray(body.data)
          ? this.clashMessage(body.data)
          : 'Failed to save timetable. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/management/timetables']);
  }

  // ---------------------------------------------------------------------------
  // Template helpers
  // ---------------------------------------------------------------------------

  getName(item: { nameAr?: string; nameEn?: string } | undefined | null): string {
    if (!item) return '';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  /** "Ahmed is already teaching Grade 2 / B on Sunday 09:00 - 09:45" for each clash the server found */
  private clashMessage(clashes: any[]): string {
    const ar = this.currentLanguage() === 'ar';
    const pick = (a: string, e: string) => (ar ? a || e : e || a);
    const time = (t: string) => (t || '').substring(0, 5);
    const lines = clashes.map(c => this.translate.instant('timetables.teacherClashLine', {
      teacher: pick(c.teacherNameAr, c.teacherNameEn),
      day: this.translate.instant(WEEK_DAYS.find(d => d.value === Number(c.day))?.key ?? ''),
      time: `${time(c.startTime)} - ${time(c.endTime)}`,
      section: pick(c.sectionNameAr, c.sectionNameEn),
      other: [pick(c.otherGradeNameAr, c.otherGradeNameEn), pick(c.otherSectionNameAr, c.otherSectionNameEn)].filter(Boolean).join(' / '),
      otherTime: `${time(c.otherStartTime)} - ${time(c.otherEndTime)}`
    }));
    return this.translate.instant('timetables.teacherClash') + ' ' + lines.join(' • ');
  }

  /** Name of the teacher of the in-form clash */
  clashTeacherName(id: string): string {
    const teacher = this.teachers().find(t => t.id === id);
    return teacher ? this.getTeacherName(teacher) : '';
  }

  clashDayName(day: number): string {
    return this.translate.instant(WEEK_DAYS.find(d => d.value === day)?.key ?? '');
  }

  getTeacherName(teacher: Teacher): string {
    return this.currentLanguage() === 'ar'
      ? (teacher.fullNameAr || teacher.fullNameEn)
      : (teacher.fullNameEn || teacher.fullNameAr);
  }

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  hasSlotError(index: number, fieldName: string, errorType: string): boolean {
    const control = this.slots.at(index)?.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  slotControl(index: number, name: string): any {
    return this.slots.at(index)?.get(name);
  }

  /** "08:00:00" -> "08:00" for <input type="time"> */
  private toInputTime(value?: string | null): string {
    return value ? value.substring(0, 5) : '';
  }

  /** "08:00" -> "08:00:00" for .NET TimeOnly */
  private fromInputTime(value?: string | null): string {
    if (!value) return '';
    return value.length === 5 ? `${value}:00` : value;
  }
}
