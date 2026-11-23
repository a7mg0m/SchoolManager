import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { StudentEvaluationService } from '../../core/services/student-evaluation.service';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { TeacherService } from '../../core/services/teacher.service';
import {
  EVALUATION_RATINGS,
  EvaluationRating,
  HEALTH_STATUSES,
  HealthStatus,
  StudentEvaluation
} from '../../core/models/attendance.model';
import { Student } from '../../core/models/student.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Teacher } from '../../core/models/teacher.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { attendanceApiError } from '../attendance-errors';
import { formatDate, todayIso } from '../../notification/shared/audience';

/**
 * Read-only list of a student's evaluations in a date range.
 * Teachers evaluate from the teacher app; the dashboard only views.
 */
@Component({
  selector: 'app-evaluation-list',
  standalone: false,
  styleUrl: './evaluation-list.component.scss',
  templateUrl: './evaluation-list.component.html',
})
export class EvaluationListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);
  students = signal<Student[]>([]);
  subjects = signal<SchoolSubject[]>([]);
  teachers = signal<Teacher[]>([]);
  evaluations = signal<StudentEvaluation[]>([]);
  isLoading = signal(false);
  isSearching = signal(false);
  searched = signal(false);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  // Filters
  gradeId = signal('');
  sectionId = signal('');
  studentId = signal('');
  studentSearch = signal('');
  subjectFilter = signal('');
  from = signal(this.monthStart());
  to = signal(todayIso());

  readonly ratings = EVALUATION_RATINGS;
  readonly healthStatuses = HEALTH_STATUSES;
  readonly stars = [1, 2, 3, 4, 5];

  activeGrades = computed(() => this.grades().filter(g => g.isActive !== false));

  gradeSections = computed(() => this.sections().filter(s => s.gradeId === this.gradeId()));

  /** Students of the chosen grade / section, or found by name / national id */
  pickableStudents = computed(() => {
    const term = this.studentSearch().trim().toLowerCase();
    return this.students()
      .filter(s => !this.gradeId() || s.gradeId === this.gradeId())
      .filter(s => !this.sectionId() || s.sectionId === this.sectionId())
      .filter(s => !term || `${s.fullNameAr} ${s.fullNameEn} ${s.nationalId}`.toLowerCase().includes(term))
      .sort((a, b) => this.studentName(a).localeCompare(this.studentName(b)));
  });

  /** Subjects that appear in the results (for the subject filter) */
  resultSubjects = computed(() => {
    const ids = new Set(this.evaluations().map(e => e.subjectId));
    return this.subjects().filter(s => ids.has(s.id));
  });

  /** Newest first, of one subject when chosen */
  filtered = computed(() => {
    const subjectId = this.subjectFilter();
    return this.evaluations()
      .filter(e => !subjectId || e.subjectId === subjectId)
      .sort((a, b) => b.date.localeCompare(a.date));
  });

  /** Average of each rating in the results */
  averages = computed(() => {
    const list = this.filtered();
    if (list.length === 0) return null;
    const avg = (pick: (e: StudentEvaluation) => number) => list.reduce((sum, e) => sum + pick(e), 0) / list.length;
    return { focus: avg(e => e.focus), interaction: avg(e => e.interaction), behavior: avg(e => e.behavior) };
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private evaluationService: StudentEvaluationService,
    private studentService: StudentService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadLookups();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadLookups(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // Subject / teacher names are optional: a failing lookup only shows "-"
    const list = <T>(obs: Observable<{ data: T[] }>): Observable<T[]> =>
      obs.pipe(map(r => r?.data || []), catchError(() => of([] as T[])));

    forkJoin({
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      students: this.studentService.getAll(),
      subjects: list(this.subjectService.getAll()),
      teachers: list(this.teacherService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ grades, sections, students, subjects, teachers }) => {
          this.grades.set(grades.data || []);
          this.sections.set(sections.data || []);
          this.students.set(students.data || []);
          this.subjects.set(subjects);
          this.teachers.set(teachers);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading evaluation lookups:', error);
          this.errorMessage.set(this.translate.instant('attendance.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters
  // ---------------------------------------------------------------------------

  onGradeChange(event: Event): void {
    this.gradeId.set((event.target as HTMLSelectElement).value);
    this.sectionId.set('');
    this.clearStudentIfHidden();
  }

  onSectionChange(event: Event): void {
    this.sectionId.set((event.target as HTMLSelectElement).value);
    this.clearStudentIfHidden();
  }

  onStudentSearch(event: Event): void {
    this.studentSearch.set((event.target as HTMLInputElement).value);
  }

  onStudentChange(event: Event): void {
    this.studentId.set((event.target as HTMLSelectElement).value);
    this.search();
  }

  onFromChange(event: Event): void {
    this.from.set((event.target as HTMLInputElement).value);
    this.search();
  }

  onToChange(event: Event): void {
    this.to.set((event.target as HTMLInputElement).value);
    this.search();
  }

  onSubjectFilter(event: Event): void {
    this.subjectFilter.set((event.target as HTMLSelectElement).value);
  }

  /** The chosen student's evaluations between from and to */
  search(): void {
    if (!this.studentId() || !this.from() || !this.to()) return;
    if (this.to() < this.from()) {
      this.errorMessage.set(this.translate.instant('attendance.errors.ToBeforeFrom'));
      return;
    }

    this.isSearching.set(true);
    this.errorMessage.set(null);
    this.subjectFilter.set('');

    this.evaluationService
      .getByStudent(this.studentId(), this.from(), this.to())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.evaluations.set(response.data || []);
          } else {
            this.evaluations.set([]);
            this.errorMessage.set(attendanceApiError(this.translate, response.message, 'attendance.loadFailed'));
          }
          this.searched.set(true);
          this.isSearching.set(false);
        },
        error: (error) => {
          console.error('Error loading evaluations:', error);
          this.evaluations.set([]);
          this.errorMessage.set(this.translate.instant('attendance.loadFailed'));
          this.isSearching.set(false);
        }
      });
  }

  navigateToStudent(): void {
    if (this.studentId()) {
      this.router.navigate(['/attendance/daily/student', this.studentId()]);
    }
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  studentName(student: Student): string {
    return this.currentLanguage() === 'ar' ? (student.fullNameAr || student.fullNameEn) : (student.fullNameEn || student.fullNameAr);
  }

  localName(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) return '-';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  subjectName(id: string): string {
    return this.localName(this.subjects().find(s => s.id === id));
  }

  teacherName(id: string): string {
    const teacher = this.teachers().find(t => t.id === id);
    if (!teacher) return '-';
    return this.currentLanguage() === 'ar' ? (teacher.fullNameAr || teacher.fullNameEn) : (teacher.fullNameEn || teacher.fullNameAr);
  }

  ratingInfo(rating: EvaluationRating) {
    return this.ratings.find(r => r.value === rating) ?? this.ratings[0];
  }

  healthInfo(status: HealthStatus) {
    return this.healthStatuses.find(h => h.value === status) ?? this.healthStatuses[0];
  }

  day(value: string): string {
    return formatDate(value) || value;
  }

  private clearStudentIfHidden(): void {
    if (this.studentId() && !this.pickableStudents().some(s => s.id === this.studentId())) {
      this.studentId.set('');
      this.evaluations.set([]);
      this.searched.set(false);
    }
  }

  private monthStart(): string {
    return `${todayIso().substring(0, 8)}01`;
  }
}
