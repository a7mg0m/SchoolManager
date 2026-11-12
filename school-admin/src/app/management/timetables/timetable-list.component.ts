import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TimetableService } from '../../core/services/timetable.service';
import { GradeService } from '../../core/services/grade.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { TeacherService } from '../../core/services/teacher.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Timetable, TimetableSlot } from '../../core/models/timetable.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Teacher } from '../../core/models/teacher.model';
import { WEEK_DAYS } from './timetable-save.component';

@Component({
  selector: 'app-timetable-list',
  standalone: false,
  styleUrl: './timetable-list.component.scss',
  templateUrl: './timetable-list.component.html',
})
export class TimetableListComponent implements OnInit, OnDestroy {
  timetables = signal<Timetable[]>([]);
  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  currentLanguage = signal<string>('en');
  expandedId = signal<string | null>(null);

  // Lookup maps for display
  private stages = new Map<string, Stage>();
  private grades = new Map<string, Grade>();
  private academicYears = new Map<string, AcademicYear>();
  private sections = new Map<string, Section>();
  private subjects = new Map<string, SchoolSubject>();
  private teachers = new Map<string, Teacher>();

  readonly weekDays = WEEK_DAYS;

  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private timetableToDelete: Timetable | null = null;

  constructor(
    private timetableService: TimetableService,
    private gradeService: GradeService,
    private academicYearService: AcademicYearService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private router: Router,
    private modalService: NgbModal
  ) {}

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

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      timetables: this.timetableService.getAll(),
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades(),
      academicYears: this.academicYearService.getAll(),
      sections: this.sectionService.getAll(),
      subjects: this.subjectService.getAll(),
      teachers: this.teacherService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages = this.toMap(res.stages.data);
          this.grades = this.toMap(res.grades.data);
          this.academicYears = this.toMap(res.academicYears.data);
          this.sections = this.toMap(res.sections.data);
          this.subjects = this.toMap(res.subjects.data);
          this.teachers = this.toMap(res.teachers.data);

          if (res.timetables.status === 200) {
            this.timetables.set(res.timetables.data || []);
          } else {
            this.errorMessage.set(res.timetables.message || 'Failed to load timetables');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading timetables:', error);
          this.errorMessage.set('Failed to load timetables. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private toMap<T extends { id: string }>(items: T[] | null | undefined): Map<string, T> {
    return new Map((items || []).map(i => [i.id, i]));
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  navigateToCreate(): void {
    this.router.navigate(['/management/timetables/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/management/timetables/save', id]);
  }

  toggleExpand(id: string): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  // ---------------------------------------------------------------------------
  // Delete
  // ---------------------------------------------------------------------------

  openDeleteConfirmation(timetable: Timetable, content: any): void {
    this.timetableToDelete = timetable;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.timetableToDelete) return;

    this.isDeleting.set(true);

    this.timetableService
      .delete(this.timetableToDelete.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Timetable deleted successfully');
            this.deleteModalRef?.close();
            this.timetableToDelete = null;
            this.loadData();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete timetable');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting timetable:', error);
          this.errorMessage.set('Failed to delete timetable. Please try again.');
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.timetableToDelete = null;
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  private localized(item: { nameAr?: string; nameEn?: string } | undefined): string {
    if (!item) return '—';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  stageName(id: string): string { return this.localized(this.stages.get(id)); }
  gradeName(id: string): string { return this.localized(this.grades.get(id)); }
  sectionName(id: string): string { return this.localized(this.sections.get(id)); }
  subjectName(id: string): string { return this.localized(this.subjects.get(id)); }
  slotName(slot: TimetableSlot): string { return this.localized(slot); }
  academicYearName(id: string): string { return this.academicYears.get(id)?.name ?? '—'; }

  teacherName(id: string): string {
    const t = this.teachers.get(id);
    if (!t) return '—';
    return this.currentLanguage() === 'ar' ? (t.fullNameAr || t.fullNameEn) : (t.fullNameEn || t.fullNameAr);
  }

  /** Days that have at least one slot, in week order */
  usedDays(timetable: Timetable): { value: number; key: string }[] {
    const days = new Set((timetable.slots || []).map(s => s.day));
    return this.weekDays.filter(d => days.has(d.value));
  }

  slotsForDay(timetable: Timetable, day: number): TimetableSlot[] {
    return (timetable.slots || [])
      .filter(s => s.day === day)
      .sort((a, b) => a.startTime.localeCompare(b.startTime));
  }

  sectionCount(timetable: Timetable): number {
    return new Set((timetable.slots || []).map(s => s.sectionId)).size;
  }

  formatTime(value: string): string {
    return value ? value.substring(0, 5) : '';
  }
}
