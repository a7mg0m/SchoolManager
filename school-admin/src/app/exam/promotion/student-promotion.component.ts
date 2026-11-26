import { Component, OnInit, OnDestroy, computed, effect, signal } from '@angular/core';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { ExamService } from '../../core/services/exam.service';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { StageService } from '../../core/services/stage.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { AcademicContextService } from '../../core/services/academic-context.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { PromotionFinal } from '../../core/models/exam.model';
import { Student, StudentStatus } from '../../core/models/student.model';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { AcademicYear } from '../../core/models/academic-year.model';

/** One Final of a student: passed with the final, passed with its second final, failed, or no score yet */
type Outcome = 'passed' | 'passedSecond' | 'failed' | 'awaitingSecond' | 'missing';

interface SubjectOutcome {
  final: PromotionFinal;
  subjectName: string;
  score: number | null;
  secondScore: number | null;
  outcome: Outcome;
}

/** Overall: passed every Final / failed one with no second chance left / some score still missing */
type Verdict = 'passed' | 'failed' | 'pending' | 'noFinals';

interface PromotionRow {
  student: Student;
  outcomes: SubjectOutcome[];
  verdict: Verdict;
}

/** The target value "graduate" (instead of a grade id): the students finish school */
export const GRADUATE = 'graduate';

/**
 * End-of-year promotion. For a grade and academic year, each current student's Final exams (or their second
 * finals) decide: passed every Final → can be promoted. The admin approves the passed students, who then move to
 * the target grade of the target year with no section (they are placed in a class later), or graduate.
 */
@Component({
  selector: 'app-student-promotion',
  templateUrl: './student-promotion.component.html',
  styleUrls: ['./student-promotion.component.scss'],
  standalone: false
})
export class StudentPromotionComponent implements OnInit, OnDestroy {
  readonly GRADUATE = GRADUATE;

  isLoading = signal(true);
  isLoadingFinals = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  years = signal<AcademicYear[]>([]);
  private students = signal<Student[]>([]);
  private sections = new Map<string, Section>();
  private subjects = new Map<string, SchoolSubject>();

  yearId = signal('');
  gradeId = signal('');
  targetGradeId = signal('');
  targetYearId = signal('');
  verdictFilter = signal<'' | Verdict>('');

  finals = signal<PromotionFinal[]>([]);
  selected = signal<Set<string>>(new Set());

  private destroy$ = new Subject<void>();

  constructor(
    private examService: ExamService,
    private studentService: StudentService,
    private gradeService: GradeService,
    private stageService: StageService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private academicYearService: AcademicYearService,
    private academicContext: AcademicContextService,
    private languageService: LanguageService,
    private modalService: NgbModal,
    private translate: TranslateService
  ) {
    // The header's year is the default (until a year is chosen here)
    effect(() => {
      const headerYear = this.academicContext.yearId();
      if (headerYear && !this.yearId()) this.yearId.set(headerYear);
    });
  }

  /** Grades in school order: by stage (as listed), then as listed in the stage */
  orderedGrades = computed(() => {
    const stageOrder = new Map(this.stages().map((s, i) => [s.id, i]));
    return this.grades()
      .map((g, i) => ({ g, i }))
      .sort((a, b) => ((stageOrder.get(a.g.stageId) ?? 99) - (stageOrder.get(b.g.stageId) ?? 99)) || a.i - b.i)
      .map(x => x.g);
  });

  /** Years in date order */
  orderedYears = computed(() => [...this.years()].sort((a, b) => a.startDate.localeCompare(b.startDate)));

  /** The current students of the chosen grade (not those moved in from a later year) */
  private gradeStudents = computed(() => {
    const gradeId = this.gradeId();
    if (!gradeId) return [];
    const yearStart = this.years().find(y => y.id === this.yearId())?.startDate ?? '';
    const startOf = new Map(this.years().map(y => [y.id, y.startDate]));
    return this.students()
      .filter(s => s.status === StudentStatus.Current && s.gradeId === gradeId)
      .filter(s => !yearStart || (startOf.get(s.academicYearId) ?? '') <= yearStart)
      .sort((a, b) => this.studentName(a).localeCompare(this.studentName(b)));
  });

  rows = computed<PromotionRow[]>(() => {
    const finals = this.finals();
    return this.gradeStudents().map(student => {
      const outcomes = finals
        .filter(f => !f.sectionId || f.sectionId === student.sectionId)
        .map(f => this.outcomeOf(f, student.id));
      return { student, outcomes, verdict: this.verdictOf(outcomes) };
    });
  });

  visibleRows = computed(() => {
    const filter = this.verdictFilter();
    return filter ? this.rows().filter(r => r.verdict === filter) : this.rows();
  });

  counts = computed(() => {
    const c = { passed: 0, failed: 0, pending: 0, noFinals: 0 };
    this.rows().forEach(r => c[r.verdict]++);
    return c;
  });

  passedIds = computed(() => this.rows().filter(r => r.verdict === 'passed').map(r => r.student.id));
  selectedCount = computed(() => this.passedIds().filter(id => this.selected().has(id)).length);
  allPassedSelected = computed(() => this.passedIds().length > 0 && this.selectedCount() === this.passedIds().length);

  /** The target is "graduate", or a grade + year */
  isGraduate = computed(() => this.targetGradeId() === GRADUATE);
  canApprove = computed(() =>
    !this.isSaving() && this.selectedCount() > 0 &&
    (this.isGraduate() || (!!this.targetGradeId() && !!this.targetYearId())));

  targetLabel = computed(() => {
    if (this.isGraduate()) return this.translate.instant('promotion.graduate');
    const grade = this.grades().find(g => g.id === this.targetGradeId());
    const year = this.years().find(y => y.id === this.targetYearId());
    return [this.getName(grade), year?.name].filter(Boolean).join(' · ');
  });

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.isLoading.set(true);
    forkJoin({
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      subjects: this.subjectService.getAll(),
      years: this.academicYearService.getAll(),
      students: this.studentService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.stages.set(res.stages.data || []);
          this.grades.set((res.grades.data || []).filter(g => g.isActive));
          this.sections = new Map((res.sections.data || []).map(s => [s.id, s]));
          this.subjects = new Map((res.subjects.data || []).map(s => [s.id, s]));
          this.years.set(res.years.data || []);
          this.students.set(res.students.data || []);
          if (!this.yearId()) this.yearId.set(this.academicContext.yearId() || this.orderedYears().at(-1)?.id || '');
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set(this.translate.instant('promotion.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private reloadStudents(): void {
    this.studentService.getAll().pipe(takeUntil(this.destroy$)).subscribe({
      next: res => this.students.set(res.data || [])
    });
  }

  onYearChange(id: string): void {
    this.yearId.set(id);
    this.suggestTarget();
    this.loadFinals();
  }

  onGradeChange(id: string): void {
    this.gradeId.set(id);
    this.verdictFilter.set('');
    this.suggestTarget();
    this.loadFinals();
  }

  /** The next grade (in school order; the last grade graduates) and the year after the chosen one */
  private suggestTarget(): void {
    const grades = this.orderedGrades();
    const index = grades.findIndex(g => g.id === this.gradeId());
    if (index < 0) {
      this.targetGradeId.set('');
    } else {
      this.targetGradeId.set(index + 1 < grades.length ? grades[index + 1].id : GRADUATE);
    }
    const years = this.orderedYears();
    const yearIndex = years.findIndex(y => y.id === this.yearId());
    this.targetYearId.set(yearIndex >= 0 && yearIndex + 1 < years.length ? years[yearIndex + 1].id : '');
  }

  private loadFinals(): void {
    this.finals.set([]);
    this.selected.set(new Set());
    if (!this.yearId() || !this.gradeId()) return;
    this.isLoadingFinals.set(true);
    this.examService.getPromotionFinals(this.yearId(), this.gradeId())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.finals.set(res.data || []);
          // The passed students are selected by default
          this.selected.set(new Set(this.passedIds()));
          this.isLoadingFinals.set(false);
        },
        error: () => {
          this.errorMessage.set(this.translate.instant('promotion.finalsFailed'));
          this.isLoadingFinals.set(false);
        }
      });
  }

  private outcomeOf(final: PromotionFinal, studentId: string): SubjectOutcome {
    const score = final.scores?.[studentId] ?? null;
    const secondScore = final.secondFinal?.scores?.[studentId] ?? null;
    let outcome: Outcome;
    if (score === null) outcome = 'missing';
    else if (score >= final.successScore) outcome = 'passed';
    else if (!final.secondFinal) outcome = 'failed';
    else if (secondScore === null) outcome = 'awaitingSecond';
    else outcome = secondScore >= final.secondFinal.successScore ? 'passedSecond' : 'failed';
    return { final, subjectName: this.getName(this.subjects.get(final.subjectId)), score, secondScore, outcome };
  }

  private verdictOf(outcomes: SubjectOutcome[]): Verdict {
    if (outcomes.length === 0) return 'noFinals';
    if (outcomes.some(o => o.outcome === 'failed')) return 'failed';
    if (outcomes.some(o => o.outcome === 'missing' || o.outcome === 'awaitingSecond')) return 'pending';
    return 'passed';
  }

  toggle(id: string): void {
    const next = new Set(this.selected());
    if (next.has(id)) next.delete(id); else next.add(id);
    this.selected.set(next);
  }

  toggleAll(): void {
    this.selected.set(this.allPassedSelected() ? new Set() : new Set(this.passedIds()));
  }

  approve(confirmContent: any): void {
    if (!this.canApprove()) return;
    this.modalService.open(confirmContent, { centered: true }).result.then(
      confirmed => { if (confirmed) this.save(); },
      () => { /* dismissed */ });
  }

  private save(): void {
    const studentIds = this.passedIds().filter(id => this.selected().has(id));
    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.studentService.promote({
      studentIds,
      fromGradeId: this.gradeId(),
      targetGradeId: this.isGraduate() ? null : this.targetGradeId(),
      targetAcademicYearId: this.isGraduate() ? null : this.targetYearId(),
      graduate: this.isGraduate()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.isSaving.set(false);
          this.successMessage.set(this.translate.instant(
            this.isGraduate() ? 'promotion.graduatedDone' : 'promotion.promotedDone',
            { count: res.data ?? studentIds.length, target: this.targetLabel() }));
          this.selected.set(new Set());
          this.reloadStudents();
        },
        error: err => {
          this.isSaving.set(false);
          const code = err?.error?.message;
          this.errorMessage.set(this.translate.instant('promotion.errors.' + (code || 'failed')) );
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
    return id ? this.getName(this.sections.get(id)) : this.translate.instant('students.noSection');
  }

  stageName(stageId: string): string {
    return this.getName(this.stages().find(s => s.id === stageId));
  }

  verdictBadge(v: Verdict): string {
    return { passed: 'bg-success', failed: 'bg-danger', pending: 'bg-warning text-dark', noFinals: 'bg-secondary' }[v];
  }

  outcomeClass(o: Outcome): string {
    return { passed: 'ok', passedSecond: 'ok second', failed: 'bad', awaitingSecond: 'wait', missing: 'missing' }[o];
  }
}
