import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { StudentService } from '../../core/services/student.service';
import { ParentService } from '../../core/services/parent.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Student, StudentStatus, STUDENT_STATUSES, studentStatusInfo } from '../../core/models/student.model';
import { Parent } from '../../core/models/parent.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { AcademicYear } from '../../core/models/academic-year.model';

/** One student row of the report */
interface ReportRow {
  student: Student;
  status: StudentStatus;
  name: string;
  stage: string;
  grade: string;
  section: string;
  year: string;
  parent: string;
  parentPhone: string;
}

/** Counts of one grade */
interface GradeBreakdown {
  gradeId: string;
  name: string;
  stage: string;
  current: number;
  graduated: number;
  transferred: number;
  total: number;
}

/**
 * Report: students by status (current / graduated / transferred) — /reports/students-status
 * Filters by year, stage, grade, class, status, status date and text; counts, a breakdown per grade, the student
 * list; export to Excel (CSV) and print.
 */
@Component({
  selector: 'app-students-status-report',
  standalone: false,
  styleUrl: './students-status-report.component.scss',
  templateUrl: './students-status-report.component.html',
})
export class StudentsStatusReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private students = signal<Student[]>([]);
  private parents = signal<Map<string, Parent>>(new Map());
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  // Filters
  yearFilter = signal('');
  stageFilter = signal('');
  gradeFilter = signal('');
  sectionFilter = signal('');
  /** 0 = every status */
  statusFilter = signal<number>(0);
  /** Status date range (graduated / transferred), yyyy-MM-dd */
  dateFrom = signal('');
  dateTo = signal('');
  searchTerm = signal('');

  readonly statuses = STUDENT_STATUSES;
  readonly statusInfo = studentStatusInfo;
  readonly StudentStatus = StudentStatus;
  readonly generatedOn = new Date();

  private destroy$ = new Subject<void>();

  filterGrades = computed(() => {
    const stage = this.stageFilter();
    return this.grades().filter(g => !stage || g.stageId === stage);
  });

  filterSections = computed(() => {
    const grade = this.gradeFilter();
    return grade ? this.sections().filter(s => s.gradeId === grade) : [];
  });

  /** Every student, with names resolved */
  private rows = computed<ReportRow[]>(() => {
    const lang = this.currentLanguage();
    const stages = new Map(this.stages().map(s => [s.id, s]));
    const grades = new Map(this.grades().map(g => [g.id, g]));
    const sections = new Map(this.sections().map(s => [s.id, s]));
    const years = new Map(this.academicYears().map(y => [y.id, y]));
    const parents = this.parents();
    return this.students().map(s => {
      const grade = grades.get(s.gradeId);
      const parent = parents.get(s.parentId);
      return {
        student: s,
        status: s.status ?? StudentStatus.Current,
        name: this.pick(s.fullNameAr, s.fullNameEn, lang),
        stage: this.nameOf(stages.get(s.stageId || grade?.stageId || ''), lang),
        grade: this.nameOf(grade, lang),
        section: this.nameOf(sections.get(s.sectionId), lang),
        year: years.get(s.academicYearId)?.name ?? '',
        parent: parent ? this.pick(parent.fullNameAr, parent.fullNameEn, lang) : '',
        parentPhone: parent?.phoneNumber ?? ''
      };
    });
  });

  /** Rows after every filter but the status one (so the counts show each status) */
  private scoped = computed(() => {
    const year = this.yearFilter();
    const stage = this.stageFilter();
    const grade = this.gradeFilter();
    const section = this.sectionFilter();
    const from = this.dateFrom();
    const to = this.dateTo();
    const term = this.searchTerm().trim().toLowerCase();
    const grades = new Map(this.grades().map(g => [g.id, g]));
    return this.rows().filter(r => {
      const s = r.student;
      if (year && s.academicYearId !== year) return false;
      if (stage && (s.stageId || grades.get(s.gradeId)?.stageId) !== stage) return false;
      if (grade && s.gradeId !== grade) return false;
      if (section && s.sectionId !== section) return false;
      // A date range keeps the students who left in it
      if (from || to) {
        const d = (s.statusDate ?? '').slice(0, 10);
        if (!d || (from && d < from) || (to && d > to)) return false;
      }
      if (term) {
        const text = [s.fullNameAr, s.fullNameEn, s.nationalId, r.parent, r.parentPhone, s.statusNote].join(' ').toLowerCase();
        if (!text.includes(term)) return false;
      }
      return true;
    });
  });

  /** The report's rows (status filter too), by status, grade, class then name */
  filtered = computed(() => {
    const status = this.statusFilter();
    return this.scoped()
      .filter(r => !status || r.status === status)
      .sort((a, b) => a.status - b.status || a.grade.localeCompare(b.grade) || a.section.localeCompare(b.section) || a.name.localeCompare(b.name));
  });

  summary = computed(() => {
    const rows = this.scoped();
    const count = (st: StudentStatus) => rows.filter(r => r.status === st).length;
    const total = rows.length;
    const pct = (n: number) => (total ? Math.round((n * 100) / total) : 0);
    const current = count(StudentStatus.Current);
    const graduated = count(StudentStatus.Graduated);
    const transferred = count(StudentStatus.Transferred);
    return { total, current, graduated, transferred, currentPct: pct(current), graduatedPct: pct(graduated), transferredPct: pct(transferred) };
  });

  /** Counts per grade (with the status filter), biggest first */
  byGrade = computed<GradeBreakdown[]>(() => {
    const status = this.statusFilter();
    const groups = new Map<string, GradeBreakdown>();
    for (const r of this.scoped()) {
      if (status && r.status !== status) continue;
      const key = r.student.gradeId;
      const g = groups.get(key) ?? { gradeId: key, name: r.grade || '-', stage: r.stage, current: 0, graduated: 0, transferred: 0, total: 0 };
      if (r.status === StudentStatus.Graduated) g.graduated++;
      else if (r.status === StudentStatus.Transferred) g.transferred++;
      else g.current++;
      g.total++;
      groups.set(key, g);
    }
    return [...groups.values()].sort((a, b) => a.stage.localeCompare(b.stage) || a.name.localeCompare(b.name));
  });

  hasFilters = computed(() =>
    !!(this.yearFilter() || this.stageFilter() || this.gradeFilter() || this.sectionFilter() || this.statusFilter()
      || this.dateFrom() || this.dateTo() || this.searchTerm()));

  constructor(
    private studentService: StudentService,
    private parentService: ParentService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private academicYearService: AcademicYearService,
    private languageService: LanguageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$.pipe(takeUntil(this.destroy$)).subscribe(l => this.currentLanguage.set(l));
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let failed = false;
    const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(
        map(r => (r.status === 200 && r.data ? r.data : [])),
        catchError(() => {
          failed = true;
          return of([] as T[]);
        })
      );
    forkJoin({
      students: list(this.studentService.getAll()),
      parents: list(this.parentService.getAll()),
      stages: list(this.stageService.getAllStages()),
      grades: list(this.gradeService.getAllGrades()),
      sections: list(this.sectionService.getAll()),
      years: list(this.academicYearService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.students.set(r.students);
        this.parents.set(new Map(r.parents.map(p => [p.id, p])));
        this.stages.set(r.stages);
        this.grades.set(r.grades);
        this.sections.set(r.sections);
        this.academicYears.set(r.years);
        if (failed) this.errorMessage.set(this.translate.instant('reports.loadFailed'));
        this.isLoading.set(false);
      });
  }

  setStage(id: string): void {
    this.stageFilter.set(id);
    if (this.gradeFilter() && !this.filterGrades().some(g => g.id === this.gradeFilter())) {
      this.gradeFilter.set('');
      this.sectionFilter.set('');
    }
  }

  setGrade(id: string): void {
    this.gradeFilter.set(id);
    this.sectionFilter.set('');
  }

  clearFilters(): void {
    this.yearFilter.set('');
    this.stageFilter.set('');
    this.gradeFilter.set('');
    this.sectionFilter.set('');
    this.statusFilter.set(0);
    this.dateFrom.set('');
    this.dateTo.set('');
    this.searchTerm.set('');
  }

  print(): void {
    window.print();
  }

  /** The rows shown, as a CSV file that Excel opens with Arabic names (UTF-8 with BOM) */
  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.studentsStatus.student'), t('parents.nationalId'), t('parents.stage'), t('parents.grade'), t('parents.section'),
      t('parents.academicYear'), t('students.status'), t('reports.studentsStatus.statusDate'), t('reports.studentsStatus.note'),
      t('students.parent'), t('parents.phoneNumber')
    ];
    const lines = this.filtered().map(r => [
      r.name, r.student.nationalId, r.stage, r.grade, r.section, r.year, t(this.statusInfo(r.status).key),
      r.student.statusDate ?? '', r.student.statusNote ?? '', r.parent, r.parentPhone
    ]);
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `students-status-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  nameOf(item: { nameAr: string; nameEn: string } | undefined, lang = this.currentLanguage()): string {
    return item ? this.pick(item.nameAr, item.nameEn, lang) : '';
  }

  private pick(ar: string | null | undefined, en: string | null | undefined, lang: string): string {
    return (lang === 'ar' ? ar || en : en || ar) || '';
  }
}
