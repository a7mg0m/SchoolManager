import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { Student } from '../../core/models/student.model';
import { Grade } from '../../core/models/grade.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/** Students of the school; open one for their clinic file (medical profile, vaccinations, visits) */
@Component({
  selector: 'app-student-clinic-list',
  standalone: false,
  styleUrl: './student-clinic-list.component.scss',
  templateUrl: './student-clinic-list.component.html',
})
export class StudentClinicListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  students = signal<Student[]>([]);
  grades = signal<Grade[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal<string>('en');
  searchTerm = signal('');
  gradeFilter = signal('');
  page = signal(1);
  readonly pageSize = 20;

  filtered = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const grade = this.gradeFilter();
    return this.students()
      .filter(s => !grade || s.gradeId === grade)
      .filter(s => !term || `${s.fullNameAr} ${s.fullNameEn} ${s.nationalId}`.toLowerCase().includes(term));
  });

  paged = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private studentService: StudentService,
    private gradeService: GradeService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadStudents();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadStudents(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({ students: this.studentService.getAll(), grades: this.gradeService.getAllGrades() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ students, grades }) => {
          this.grades.set(grades.data || []);
          if (students.status === 200 && students.data) {
            this.students.set([...students.data].sort((a, b) => this.name(a).localeCompare(this.name(b))));
          } else {
            this.errorMessage.set(students.message || this.translate.instant('clinic.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading students:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  setSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.page.set(1);
  }

  setGrade(event: Event): void {
    this.gradeFilter.set((event.target as HTMLSelectElement).value);
    this.page.set(1);
  }

  openStudent(id: string): void {
    this.router.navigate(['/clinic/students', id]);
  }

  name(student: Student): string {
    return this.currentLanguage() === 'ar' ? student.fullNameAr : student.fullNameEn;
  }

  gradeName(id: string): string {
    const grade = this.grades().find(g => g.id === id);
    if (!grade) return '-';
    return this.currentLanguage() === 'ar' ? grade.nameAr : grade.nameEn;
  }
}
