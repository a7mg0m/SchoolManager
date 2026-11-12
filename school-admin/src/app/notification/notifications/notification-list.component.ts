import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../core/services/notification.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { AppNotification, NOTIFICATION_TYPES } from '../../core/models/notification.model';
import { EVENT_FOR_OPTIONS, EventFor } from '../../core/models/school-event.model';
import {
  AudienceLookups,
  EMPTY_LOOKUPS,
  audienceNames,
  formatDate,
  inferAudience,
  loadAudienceLookups,
  todayIso
} from '../shared/audience';

type StatusFilter = '' | 'scheduled' | 'sent';

@Component({
  selector: 'app-notification-list',
  standalone: false,
  styleUrl: './notification-list.component.scss',
  templateUrl: './notification-list.component.html',
})
export class NotificationListComponent implements OnInit, OnDestroy {
  notifications = signal<AppNotification[]>([]);
  private lookups = signal<AudienceLookups>(EMPTY_LOOKUPS);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  viewed = signal<AppNotification | null>(null);
  toDelete = signal<AppNotification | null>(null);

  // Filters
  searchTerm = signal('');
  typeFilter = signal('');
  statusFilter = signal<StatusFilter>('');
  page = signal(1);
  readonly pageSize = 10;

  readonly types = NOTIFICATION_TYPES;
  readonly EventFor = EventFor;

  filtered = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const type = this.typeFilter();
    const status = this.statusFilter();
    const today = todayIso();

    return this.notifications().filter(n => {
      if (type && n.notificationType !== Number(type)) return false;
      if (status === 'scheduled' && !(n.sentOn > today)) return false;
      if (status === 'sent' && n.sentOn > today) return false;
      if (term && !`${n.title} ${n.description}`.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  paged = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  });

  /** Counts per type for the summary chips (whole list, not filtered) */
  typeCounts = computed(() => {
    const counts = new Map<number, number>();
    for (const n of this.notifications()) counts.set(n.notificationType, (counts.get(n.notificationType) ?? 0) + 1);
    return counts;
  });

  scheduledCount = computed(() => {
    const today = todayIso();
    return this.notifications().filter(n => n.sentOn > today).length;
  });

  hasActiveFilters = computed(() => !!(this.searchTerm() || this.typeFilter() || this.statusFilter()));

  private destroy$ = new Subject<void>();
  private modalRef: NgbModalRef | null = null;

  constructor(
    private notificationService: NotificationService,
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
      notifications: this.notificationService.getAll(),
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
        next: ({ notifications, lookups }) => {
          this.lookups.set(lookups);
          if (notifications.status === 200) {
            this.notifications.set(notifications.data || []);
            this.clampPage();
          } else {
            this.errorMessage.set(notifications.message || this.translate.instant('notifications.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading notifications:', error);
          this.errorMessage.set(this.translate.instant('notifications.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters & navigation
  // ---------------------------------------------------------------------------

  setFilter(filter: 'search' | 'type' | 'status', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    if (filter === 'search') this.searchTerm.set(value);
    if (filter === 'type') this.typeFilter.set(value);
    if (filter === 'status') this.statusFilter.set(value as StatusFilter);
    this.page.set(1);
  }

  /** Clicking a type chip toggles that type filter */
  toggleType(value: number): void {
    this.typeFilter.set(this.typeFilter() === String(value) ? '' : String(value));
    this.page.set(1);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.typeFilter.set('');
    this.statusFilter.set('');
    this.page.set(1);
  }

  private clampPage(): void {
    const last = Math.max(1, Math.ceil(this.filtered().length / this.pageSize));
    if (this.page() > last) this.page.set(last);
  }

  navigateToCreate(): void {
    this.router.navigate(['/notification/notifications/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/notification/notifications/save', id]);
  }

  navigateToRecipients(id: string): void {
    this.router.navigate(['/notification/notifications', id, 'recipients']);
  }

  // ---------------------------------------------------------------------------
  // View / delete
  // ---------------------------------------------------------------------------

  openView(n: AppNotification, content: any): void {
    this.viewed.set(n);
    this.modalRef = this.modalService.open(content, { centered: true, size: 'lg', scrollable: true });
    this.modalRef.hidden.pipe(takeUntil(this.destroy$)).subscribe(() => this.viewed.set(null));
  }

  openDeleteConfirmation(n: AppNotification, content: any): void {
    this.toDelete.set(n);
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  confirmDelete(): void {
    const n = this.toDelete();
    if (!n || this.isDeleting()) return;

    this.isDeleting.set(true);

    this.notificationService
      .delete(n.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.toDelete.set(null);
            this.notifications.update(list => list.filter(x => x.id !== n.id));
            this.clampPage();
            this.showSuccess(this.translate.instant('notifications.deleted'));
          } else {
            this.errorMessage.set(response.message || this.translate.instant('notifications.deleteFailed'));
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting notification:', error);
          this.errorMessage.set(this.translate.instant('notifications.deleteFailed'));
          this.modalRef?.close();
          this.toDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.modalRef?.close();
    this.toDelete.set(null);
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

  typeInfo(n: AppNotification) {
    return this.types.find(t => t.value === n.notificationType) ?? this.types[this.types.length - 1];
  }

  typeCount(value: number): number {
    return this.typeCounts().get(value) ?? 0;
  }

  isScheduled(n: AppNotification): boolean {
    return n.sentOn > todayIso();
  }

  audienceType(n: AppNotification): EventFor {
    return inferAudience(n.targets ?? [], this.lookups());
  }

  audienceOption(n: AppNotification) {
    const type = this.audienceType(n);
    return EVENT_FOR_OPTIONS.find(o => o.value === type) ?? EVENT_FOR_OPTIONS[0];
  }

  audience(n: AppNotification): string[] {
    return audienceNames(this.audienceType(n), n.targets ?? [], this.lookups(), this.currentLanguage());
  }

  audienceSummary(n: AppNotification): string {
    if (!n.targets?.length) return this.translate.instant('schoolEvents.wholeSchool');
    const names = this.audience(n);
    if (names.length === 0) return this.translate.instant('notifications.recipientsCount', { count: n.targets.length });
    const separator = this.currentLanguage() === 'ar' ? '، ' : ', ';
    const shown = names.slice(0, 2).join(separator);
    return names.length > 2 ? `${shown} +${names.length - 2}` : shown;
  }

  formatDate(value: string): string {
    return formatDate(value);
  }
}
