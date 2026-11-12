import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { SchoolEventService } from '../../core/services/school-event.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { SchoolEvent, EventFor, EVENT_FOR_OPTIONS } from '../../core/models/school-event.model';
import {
  AudienceLookups,
  EMPTY_LOOKUPS,
  audienceNames,
  formatDate,
  formatTime,
  loadAudienceLookups,
  todayIso
} from './event-audience';

type EventTab = 'upcoming' | 'past';

@Component({
  selector: 'app-school-event-list',
  standalone: false,
  styleUrl: './school-event-list.component.scss',
  templateUrl: './school-event-list.component.html',
})
export class SchoolEventListComponent implements OnInit, OnDestroy {
  events = signal<SchoolEvent[]>([]);
  private lookups = signal<AudienceLookups>(EMPTY_LOOKUPS);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  viewedEvent = signal<SchoolEvent | null>(null);
  eventToDelete = signal<SchoolEvent | null>(null);

  // Filters
  tab = signal<EventTab>('upcoming');
  searchTerm = signal('');
  eventForFilter = signal<string>('');
  academicYearFilter = signal<string>('');

  readonly eventForOptions = EVENT_FOR_OPTIONS;
  readonly EventFor = EventFor;

  academicYears = computed(() => this.lookups().academicYears);

  /** Events matching search + filters, before the upcoming/past split */
  private matchingEvents = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const eventFor = this.eventForFilter();
    const yearId = this.academicYearFilter();

    return this.events().filter(e => {
      if (eventFor && e.eventFor !== Number(eventFor)) return false;
      if (yearId && e.academicYearId !== yearId) return false;
      if (term && !`${e.title} ${e.description}`.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  upcomingEvents = computed(() => {
    const today = todayIso();
    return this.matchingEvents()
      .filter(e => e.eventDate >= today)
      .sort((a, b) => (a.eventDate + a.startTime).localeCompare(b.eventDate + b.startTime));
  });

  pastEvents = computed(() => {
    const today = todayIso();
    return this.matchingEvents()
      .filter(e => e.eventDate < today)
      .sort((a, b) => (b.eventDate + b.startTime).localeCompare(a.eventDate + a.startTime));
  });

  visibleEvents = computed(() => (this.tab() === 'upcoming' ? this.upcomingEvents() : this.pastEvents()));

  todayCount = computed(() => {
    const today = todayIso();
    return this.events().filter(e => e.eventDate === today).length;
  });

  hasActiveFilters = computed(() => !!(this.searchTerm() || this.eventForFilter() || this.academicYearFilter()));

  private destroy$ = new Subject<void>();
  private modalRef: NgbModalRef | null = null;

  constructor(
    private schoolEventService: SchoolEventService,
    private academicYearService: AcademicYearService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private teacherService: TeacherService,
    private studentService: StudentService,
    private languageService: LanguageService,
    private translate: TranslateService,
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
    this.modalRef?.close();
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      events: this.schoolEventService.getAll(),
      lookups: loadAudienceLookups({
        academicYearService: this.academicYearService,
        stageService: this.stageService,
        gradeService: this.gradeService,
        sectionService: this.sectionService,
        teacherService: this.teacherService,
        studentService: this.studentService
      })
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ events, lookups }) => {
          this.lookups.set(lookups);
          if (events.status === 200) {
            this.events.set(events.data || []);
          } else {
            this.errorMessage.set(events.message || this.translate.instant('schoolEvents.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading school events:', error);
          this.errorMessage.set(this.translate.instant('schoolEvents.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters & navigation
  // ---------------------------------------------------------------------------

  setFilter(filter: 'search' | 'eventFor' | 'academicYear', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    if (filter === 'search') this.searchTerm.set(value);
    if (filter === 'eventFor') this.eventForFilter.set(value);
    if (filter === 'academicYear') this.academicYearFilter.set(value);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.eventForFilter.set('');
    this.academicYearFilter.set('');
  }

  navigateToCreate(): void {
    this.router.navigate(['/notification/school-events/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/notification/school-events/save', id]);
  }

  // ---------------------------------------------------------------------------
  // View / delete
  // ---------------------------------------------------------------------------

  openView(event: SchoolEvent, content: any): void {
    this.viewedEvent.set(event);
    this.modalRef = this.modalService.open(content, { centered: true, size: 'lg', scrollable: true });
    this.modalRef.hidden.pipe(takeUntil(this.destroy$)).subscribe(() => this.viewedEvent.set(null));
  }

  openDeleteConfirmation(event: SchoolEvent, content: any): void {
    this.eventToDelete.set(event);
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  confirmDelete(): void {
    const event = this.eventToDelete();
    if (!event || this.isDeleting()) return;

    this.isDeleting.set(true);

    this.schoolEventService
      .delete(event.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.eventToDelete.set(null);
            this.events.update(list => list.filter(e => e.id !== event.id));
            this.showSuccess(this.translate.instant('schoolEvents.deleted'));
          } else {
            this.errorMessage.set(response.message || this.translate.instant('schoolEvents.deleteFailed'));
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting school event:', error);
          this.errorMessage.set(this.translate.instant('schoolEvents.deleteFailed'));
          this.modalRef?.close();
          this.eventToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.modalRef?.close();
    this.eventToDelete.set(null);
  }

  private showSuccess(message: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  eventForOption(event: SchoolEvent) {
    return this.eventForOptions.find(o => o.value === event.eventFor) ?? this.eventForOptions[0];
  }

  audience(event: SchoolEvent): string[] {
    return audienceNames(event, this.lookups(), this.currentLanguage());
  }

  /** Short audience label for the card: "All school" or e.g. "Grade 1, Grade 2 +3" */
  audienceSummary(event: SchoolEvent): string {
    if (event.eventFor === EventFor.All) return this.translate.instant('schoolEvents.wholeSchool');
    const names = this.audience(event);
    if (names.length === 0) return this.translate.instant('schoolEvents.noTargets');
    const separator = this.currentLanguage() === 'ar' ? '، ' : ', ';
    const shown = names.slice(0, 2).join(separator);
    return names.length > 2 ? `${shown} +${names.length - 2}` : shown;
  }

  academicYearName(id: string): string {
    return this.lookups().academicYears.find(y => y.id === id)?.name || '-';
  }

  semesterName(event: SchoolEvent): string {
    const year = this.lookups().academicYears.find(y => y.id === event.academicYearId);
    return year?.semesters?.find(s => s.id === event.semesterId)?.name || '-';
  }

  isToday(event: SchoolEvent): boolean {
    return event.eventDate === todayIso();
  }

  dayOfMonth(event: SchoolEvent): string {
    return String(Number((event.eventDate || '').substring(8, 10)) || '');
  }

  monthShort(event: SchoolEvent): string {
    return formatDate(event.eventDate, { month: 'short' });
  }

  fullDate(event: SchoolEvent): string {
    return formatDate(event.eventDate);
  }

  timeRange(event: SchoolEvent): string {
    return `${formatTime(event.startTime)} – ${formatTime(event.endTime)}`;
  }
}
