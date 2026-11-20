import { Injectable, computed, signal } from '@angular/core';
import { AcademicYearService } from './academic-year.service';
import { AcademicYear, AcademicYearSemester, AcademicYearSemesters } from '../models/academic-year.model';

const YEAR_KEY = 'academicContext.yearId';
const SEMESTER_KEY = 'academicContext.semesterId';

/**
 * The academic year and semester chosen in the header. Pages that work with an academic year / semester
 * (lists and new records) use these values. Defaults to the current year and semester; the choice is
 * remembered in this browser.
 */
@Injectable({ providedIn: 'root' })
export class AcademicContextService {
  /** All academic years (with their semesters), newest first */
  readonly years = signal<AcademicYear[]>([]);
  readonly yearId = signal('');
  readonly semesterId = signal('');
  readonly loaded = signal(false);

  readonly year = computed(() => this.years().find(y => y.id === this.yearId()) ?? null);
  readonly semesters = computed(() => AcademicYearSemesters.ordered(this.year()));
  readonly semester = computed<AcademicYearSemester | null>(
    () => this.semesters().find(s => s.id === this.semesterId()) ?? null);

  private loading = false;

  constructor(private academicYearService: AcademicYearService) {
    // Years / semesters edited on the academic years page: reload the header lists
    this.academicYearService.changed$.subscribe(() => this.load(true));
  }

  /** Loads the years once (again with force, e.g. after an academic year was saved) */
  load(force = false): void {
    if ((this.loaded() && !force) || this.loading) {
      return;
    }
    this.loading = true;
    this.academicYearService.getAll().subscribe({
      next: (response) => {
        const years = [...(response.data ?? [])].sort((a, b) => b.startDate.localeCompare(a.startDate));
        this.years.set(years);
        this.applyDefaults();
        this.loaded.set(true);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  /** Another year: its current semester is chosen with it */
  setYear(id: string): void {
    this.yearId.set(id);
    this.semesterId.set(AcademicYearSemesters.current(this.year())?.id ?? '');
    this.save();
  }

  setSemester(id: string): void {
    this.semesterId.set(id);
    this.save();
  }

  /** The current year: active with today inside it, else an active one, else the newest */
  currentYear(): AcademicYear | undefined {
    const today = new Date().toISOString().substring(0, 10);
    const years = this.years();
    return years.find(y => y.isActive && y.startDate.substring(0, 10) <= today && today <= y.endDate.substring(0, 10))
      ?? years.find(y => y.isActive)
      ?? years[0];
  }

  /** Year name for an id (lists of other years' records) */
  yearName(id: string | null | undefined): string {
    return this.years().find(y => y.id === id)?.name ?? '—';
  }

  /** Semester name for an id within a year */
  semesterName(yearId: string | null | undefined, semesterId: string | null | undefined): string {
    const year = this.years().find(y => y.id === yearId);
    return year?.semesters?.find(s => s.id === semesterId)?.name ?? '—';
  }

  /** Saved choice when it still exists, else the current year / semester */
  private applyDefaults(): void {
    const savedYear = this.read(YEAR_KEY);
    const savedSemester = this.read(SEMESTER_KEY);
    const year = this.years().find(y => y.id === (this.yearId() || savedYear)) ?? this.currentYear();
    this.yearId.set(year?.id ?? '');

    const semesters = AcademicYearSemesters.ordered(year);
    const wanted = this.semesterId() || savedSemester;
    const semester = semesters.find(s => s.id === wanted) ?? AcademicYearSemesters.current(year);
    this.semesterId.set(semester?.id ?? '');
  }

  private save(): void {
    try {
      localStorage.setItem(YEAR_KEY, this.yearId());
      localStorage.setItem(SEMESTER_KEY, this.semesterId());
    } catch {
      // Storage unavailable: the choice lasts for this session only
    }
  }

  private read(key: string): string {
    try {
      return localStorage.getItem(key) ?? '';
    } catch {
      return '';
    }
  }
}
