import { Component, OnInit, OnDestroy, computed, signal } from '@angular/core';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { StageService } from '../../core/services/stage.service';
import { SectionService } from '../../core/services/section.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Student, StudentStatus } from '../../core/models/student.model';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';
import { Section } from '../../core/models/section.model';

/** The list / target value for "no section" */
export const NO_SECTION = 'none';

interface SectionCard {
  id: string;
  name: string;
  total: number;
  boys: number;
  girls: number;
}

/**
 * Section assignment: choose a grade, see its current students (by default those without a section, e.g. just
 * promoted), select some and place them in one of the grade's sections, or take them out of their section.
 */
@Component({
  selector: 'app-section-assignment',
  templateUrl: './section-assignment.component.html',
  styleUrls: ['./section-assignment.component.scss'],
  standalone: false
})
export class SectionAssignmentComponent implements OnInit, OnDestroy {
  readonly NO_SECTION = NO_SECTION;

  isLoading = signal(true);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private stages = signal<Stage[]>([]);
  private grades = signal<Grade[]>([]);
  private sections = signal<Section[]>([]);
  private students = signal<Student[]>([]);

  gradeId = signal('');
  /** '' all, NO_SECTION, or a section id */
  listFilter = signal<string>(NO_SECTION);
  search = signal('');
  targetSectionId = signal('');
  selected = signal<Set<string>>(new Set());

  private destroy$ = new Subject<void>();

  constructor(
    private studentService: StudentService,
    private gradeService: GradeService,
    private stageService: StageService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService
  ) {}

  /** Grades in school order (by stage as listed, then as listed), with how many students have no section */
  gradeOptions = computed(() => {
    const stageOrder = new Map(this.stages().map((s, i) => [s.id, i]));
    const unplaced = new Map<string, number>();
    this.currentStudents().forEach(s => { if (!s.sectionId) unplaced.set(s.gradeId, (unplaced.get(s.gradeId) ?? 0) + 1); });
    return this.grades()
      .map((g, i) => ({ g, i }))
      .sort((a, b) => ((stageOrder.get(a.g.stageId) ?? 99) - (stageOrder.get(b.g.stageId) ?? 99)) || a.i - b.i)
      .map(({ g }) => ({
        grade: g,
        label: [this.getName(this.stages().find(s => s.id === g.stageId)), this.getName(g)].filter(Boolean).join(' · '),
        unplaced: unplaced.get(g.id) ?? 0
      }));
  });

  private currentStudents = computed(() => this.students().filter(s => s.status === StudentStatus.Current));

  private gradeStudents = computed(() =>
    this.currentStudents()
      .filter(s => s.gradeId === this.gradeId())
      .sort((a, b) => this.studentName(a).localeCompare(this.studentName(b))));

  gradeSections = computed(() => this.sections().filter(s => s.gradeId === this.gradeId()));

  /** "Without section" first, then each section of the grade, with its numbers */
  cards = computed<SectionCard[]>(() => {
    const students = this.gradeStudents();
    const card = (id: string, name: string, list: Student[]): SectionCard => ({
      id, name, total: list.length,
      boys: list.filter(s => (s.gender || '').toLowerCase() === 'male').length,
      girls: list.filter(s => (s.gender || '').toLowerCase() === 'female').length
    });
    return [
      card(NO_SECTION, this.translate.instant('sectionAssignment.withoutSection'), students.filter(s => !s.sectionId)),
      ...this.gradeSections().map(sec => card(sec.id, this.getName(sec), students.filter(s => s.sectionId === sec.id)))
    ];
  });

  visibleStudents = computed(() => {
    const filter = this.listFilter();
    const term = this.search().trim().toLowerCase();
    return this.gradeStudents()
      .filter(s => !filter || (filter === NO_SECTION ? !s.sectionId : s.sectionId === filter))
      .filter(s => !term || [s.fullNameAr, s.fullNameEn, s.nationalId].some(v => (v || '').toLowerCase().includes(term)));
  });

  selectedVisible = computed(() => this.visibleStudents().filter(s => this.selected().has(s.id)));
  allVisibleSelected = computed(() =>
    this.visibleStudents().length > 0 && this.selectedVisible().length === this.visibleStudents().length);

  canAssign = computed(() => !this.isSaving() && this.selectedVisible().length > 0 && !!this.targetSectionId());

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private load(): void {
    this.isLoading.set(true);
    forkJoin({
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      students: this.studentService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.stages.set(res.stages.data || []);
          this.grades.set((res.grades.data || []).filter(g => g.isActive));
          this.sections.set(((res.sections.data || []) as (Section & { isActive?: boolean })[]).filter(s => s.isActive !== false));
          this.students.set(res.students.data || []);
          // Start on the first grade that has students to place
          if (!this.gradeId()) {
            const first = this.gradeOptions().find(o => o.unplaced > 0) ?? this.gradeOptions()[0];
            if (first) this.gradeId.set(first.grade.id);
          }
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set(this.translate.instant('sectionAssignment.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  onGradeChange(id: string): void {
    this.gradeId.set(id);
    this.listFilter.set(NO_SECTION);
    this.targetSectionId.set('');
    this.selected.set(new Set());
  }

  setFilter(id: string): void {
    this.listFilter.set(this.listFilter() === id ? '' : id);
    this.selected.set(new Set());
  }

  toggle(id: string): void {
    const next = new Set(this.selected());
    if (next.has(id)) next.delete(id); else next.add(id);
    this.selected.set(next);
  }

  toggleAll(): void {
    this.selected.set(this.allVisibleSelected() ? new Set() : new Set(this.visibleStudents().map(s => s.id)));
  }

  assign(): void {
    if (!this.canAssign()) return;
    const studentIds = this.selectedVisible().map(s => s.id);
    const target = this.targetSectionId();
    const sectionId = target === NO_SECTION ? null : target;
    const targetName = sectionId ? this.getName(this.sections().find(s => s.id === sectionId)) : '';

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.studentService.assignSection({ studentIds, gradeId: this.gradeId(), sectionId })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          const count = res.data ?? studentIds.length;
          // Show the change at once (the list is reloaded behind it)
          this.students.set(this.students().map(s => studentIds.includes(s.id) ? { ...s, sectionId } : s));
          this.successMessage.set(this.translate.instant(
            sectionId ? 'sectionAssignment.assignedDone' : 'sectionAssignment.removedDone', { count, section: targetName }));
          this.selected.set(new Set());
          this.isSaving.set(false);
          this.studentService.getAll().pipe(takeUntil(this.destroy$)).subscribe({ next: r => this.students.set(r.data || []) });
        },
        error: err => {
          this.isSaving.set(false);
          const code = err?.error?.message;
          this.errorMessage.set(this.translate.instant('sectionAssignment.errors.' + (code || 'failed')));
        }
      });
  }

  getName(item: { nameAr?: string; nameEn?: string } | undefined | null): string {
    if (!item) return '';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn || '') : (item.nameEn || item.nameAr || '');
  }

  studentName(s: Student): string {
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  sectionName(id: string | null): string {
    return id ? this.getName(this.sections().find(s => s.id === id)) : '';
  }

  genderKey(g: string): string {
    return (g || '').toLowerCase() === 'female' ? 'sectionAssignment.girl' : 'sectionAssignment.boy';
  }
}
