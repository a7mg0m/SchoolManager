import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExamService } from '../../core/services/exam.service';
import { ExamResultService } from '../../core/services/exam-result.service';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Exam, EXAM_TYPES } from '../../core/models/exam.model';
import { ExamResult, StudentScore } from '../../core/models/exam-result.model';
import { Student } from '../../core/models/student.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';

/** One row of the results sheet */
interface ResultRow {
  student: Student;
  /** Score saved on the server (null = no result yet) */
  savedScore: number | null;
  /** Student no longer in this grade/section but has a saved result */
  isFormer: boolean;
}

@Component({
  selector: 'app-exam-results',
  standalone: false,
  templateUrl: './exam-results.component.html',
})
export class ExamResultsComponent implements OnInit, OnDestroy {
  exam = signal<Exam | null>(null);
  rows = signal<ResultRow[]>([]);
  /** Current text in each score input, keyed by studentId */
  scores = signal<Record<string, string>>({});
  /** Values the inputs start with (not updated while typing, so the caret isn't disturbed) */
  initialScores = signal<Record<string, string>>({});
  /** Bumped on discard to re-create the inputs with the saved values */
  resetVersion = signal(0);
  search = signal('');

  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  private grades = new Map<string, Grade>();
  private sections = new Map<string, Section>();
  private subjects = new Map<string, SchoolSubject>();
  private examId = '';
  private destroy$ = new Subject<void>();

  /** Ids of rows matching the search box */
  visibleIds = computed(() => {
    const term = this.search().trim().toLowerCase();
    const rows = term
      ? this.rows().filter(r =>
          (r.student.fullNameEn || '').toLowerCase().includes(term) ||
          (r.student.fullNameAr || '').toLowerCase().includes(term) ||
          (r.student.nationalId || '').toLowerCase().includes(term))
      : this.rows();
    return new Set(rows.map(r => r.student.id));
  });

  /** studentIds whose input is not a valid score */
  invalidIds = computed(() => {
    const scores = this.scores();
    return new Set(Object.keys(scores).filter(id => this.parseScore(scores[id]) === undefined));
  });

  /** Rows whose input differs from what is saved */
  changes = computed<StudentScore[]>(() => {
    const scores = this.scores();
    const invalid = this.invalidIds();
    return this.rows()
      .filter(r => !invalid.has(r.student.id))
      .map(r => ({ studentId: r.student.id, score: this.parseScore(scores[r.student.id]) as number | null, saved: r.savedScore }))
      .filter(c => c.score !== c.saved)
      .map(({ studentId, score }) => ({ studentId, score }));
  });

  stats = computed(() => {
    const scores = this.scores();
    const values = this.rows()
      .map(r => this.parseScore(scores[r.student.id]))
      .filter((v): v is number => typeof v === 'number');
    const total = this.rows().length;
    if (values.length === 0) {
      return { total, entered: 0, average: null, highest: null, lowest: null, passed: 0, failed: 0, passRate: null };
    }
    const sum = values.reduce((a, b) => a + b, 0);
    const passed = values.filter(v => this.isPassingScore(v)).length;
    return {
      total,
      entered: values.length,
      average: Math.round((sum / values.length) * 100) / 100,
      highest: Math.max(...values),
      lowest: Math.min(...values),
      passed,
      failed: values.length - passed,
      passRate: Math.round((passed / values.length) * 100)
    };
  });

  constructor(
    private examService: ExamService,
    private examResultService: ExamResultService,
    private studentService: StudentService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private languageService: LanguageService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.examId = this.activatedRoute.snapshot.paramMap.get('id') || '';

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Loading
  // ---------------------------------------------------------------------------

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      exam: this.examService.getById(this.examId),
      results: this.examResultService.getByExam(this.examId),
      students: this.studentService.getAll(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      subjects: this.subjectService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.exam.status !== 200 || !res.exam.data) {
            this.errorMessage.set(res.exam.message || 'Failed to load exam');
            this.isLoading.set(false);
            return;
          }

          this.grades = new Map((res.grades.data || []).map(g => [g.id, g]));
          this.sections = new Map((res.sections.data || []).map(s => [s.id, s]));
          this.subjects = new Map((res.subjects.data || []).map(s => [s.id, s]));

          const exam = res.exam.data;
          this.exam.set(exam);
          this.buildRows(exam, res.students.data || [], res.results.data || []);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading exam results:', error);
          this.errorMessage.set('Failed to load exam results. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Students of the exam's grade (and section, if the exam has one), plus any
   * student who already has a saved result even if they moved since.
   */
  private buildRows(exam: Exam, students: Student[], results: ExamResult[]): void {
    const savedByStudent = new Map(results.map(r => [r.studentId, r.score]));

    const inClass = students.filter(s =>
      s.isActive &&
      s.gradeId === exam.gradeId &&
      (!exam.sectionId || s.sectionId === exam.sectionId));
    const inClassIds = new Set(inClass.map(s => s.id));

    const former = students.filter(s => !inClassIds.has(s.id) && savedByStudent.has(s.id));

    const rows: ResultRow[] = [
      ...inClass.map(student => ({ student, savedScore: savedByStudent.get(student.id) ?? null, isFormer: false })),
      ...former.map(student => ({ student, savedScore: savedByStudent.get(student.id) ?? null, isFormer: true }))
    ].sort((a, b) =>
      Number(a.isFormer) - Number(b.isFormer) ||
      this.sectionName(a.student.sectionId).localeCompare(this.sectionName(b.student.sectionId)) ||
      this.studentName(a.student).localeCompare(this.studentName(b.student)));

    this.rows.set(rows);
    this.resetChanges();
  }

  // ---------------------------------------------------------------------------
  // Editing
  // ---------------------------------------------------------------------------

  setScore(studentId: string, value: string): void {
    this.scores.update(s => ({ ...s, [studentId]: value }));
  }

  /** Discard unsaved edits */
  resetChanges(): void {
    const saved = Object.fromEntries(this.rows().map(r => [r.student.id, r.savedScore === null ? '' : String(r.savedScore)]));
    this.initialScores.set(saved);
    this.scores.set({ ...saved });
    this.resetVersion.update(v => v + 1);
  }

  isChanged(row: ResultRow): boolean {
    const parsed = this.parseScore(this.scores()[row.student.id]);
    return parsed !== undefined && parsed !== row.savedScore;
  }

  /**
   * '' -> null (no result), number within 0..weight -> number, anything else -> undefined (invalid)
   */
  private parseScore(value: string | undefined): number | null | undefined {
    const text = (value ?? '').toString().trim();
    if (text === '') return null;
    const n = Number(text);
    const weight = this.exam()?.weight ?? Infinity;
    return Number.isFinite(n) && n >= 0 && n <= weight ? n : undefined;
  }

  private isPassingScore(score: number): boolean {
    return score >= (this.exam()?.successScore ?? 0);
  }

  /** Pass/fail of the score currently in the row's input: true, false, or null (no valid score) */
  rowPassed(row: ResultRow): boolean | null {
    const parsed = this.parseScore(this.scores()[row.student.id]);
    return typeof parsed === 'number' ? this.isPassingScore(parsed) : null;
  }

  // ---------------------------------------------------------------------------
  // Save
  // ---------------------------------------------------------------------------

  save(): void {
    if (this.invalidIds().size > 0) {
      this.errorMessage.set(`Some scores are invalid. Scores must be between 0 and ${this.exam()?.weight}.`);
      return;
    }

    const results = this.changes();
    if (results.length === 0) return;

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.examResultService
      .saveBulk({ examId: this.examId, results })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            const s = response.data;
            this.successMessage.set(s
              ? `Results saved (${s.created} added, ${s.updated} updated, ${s.removed} removed)`
              : 'Results saved successfully');

            // Mark the sent values as saved
            const sent = new Map(results.map(r => [r.studentId, r.score]));
            this.rows.update(rows => rows.map(r => sent.has(r.student.id) ? { ...r, savedScore: sent.get(r.student.id)! } : r));

            timer(4000).pipe(takeUntil(this.destroy$)).subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to save results');
          }
          this.isSaving.set(false);
        },
        error: (error) => {
          console.error('Error saving exam results:', error);
          this.errorMessage.set(error?.error?.message === 'ScoreInvalid'
            ? `Some scores are invalid. Scores must be between 0 and ${this.exam()?.weight}.`
            : error?.error?.message || 'Failed to save results. Please try again.');
          this.isSaving.set(false);
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/exam/exams']);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  private localized(item: { nameAr?: string; nameEn?: string } | undefined): string {
    if (!item) return '—';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  studentName(s: Student): string {
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  gradeName(id: string): string { return this.localized(this.grades.get(id)); }
  subjectName(id: string): string { return this.localized(this.subjects.get(id)); }
  sectionName(id: string | null): string { return id ? this.localized(this.sections.get(id)) : ''; }

  examTypeKey(type: number): string {
    return EXAM_TYPES.find(t => t.value === type)?.key ?? '';
  }

  formatTime(value: string): string {
    return value ? value.substring(0, 5) : '';
  }
}
