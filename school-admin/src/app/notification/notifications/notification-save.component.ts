import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../core/services/notification.service';
import { extractApiErrors } from '../../core/services/school-event.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import {
  AppNotification,
  CreateNotificationRequest,
  NOTIFICATION_TYPES,
  NotificationType
} from '../../core/models/notification.model';
import { EventFor } from '../../core/models/school-event.model';
import {
  AudienceLookups,
  EMPTY_LOOKUPS,
  buildTargets,
  inferAudience,
  loadAudienceLookups,
  selectedIdsFromTargets,
  todayIso
} from '../shared/audience';

/** Field limits (NotificationConfiguration in the backend) */
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 2000;
const IMAGE_URL_MAX = 500;

@Component({
  selector: 'app-notification-save',
  standalone: false,
  styleUrl: './notification-save.component.scss',
  templateUrl: './notification-save.component.html',
})
export class NotificationSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  notificationId = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  apiErrors = signal<string[]>([]);
  successMessage = signal<string | null>(null);

  lookups = signal<AudienceLookups>(EMPTY_LOOKUPS);

  // Audience (bound two-way to <app-audience-picker>); notifications store targets only
  audience = signal<EventFor>(EventFor.All);
  selectedIds = signal<Set<string>>(new Set());
  submitted = signal(false);

  // Mirrors of form values for the template
  notificationType = signal<NotificationType>(NotificationType.Message);
  imageUrl = signal('');
  sentOn = signal(todayIso());

  readonly types = NOTIFICATION_TYPES;
  readonly limits = { title: TITLE_MAX, description: DESCRIPTION_MAX, imageUrl: IMAGE_URL_MAX };

  targetCount = computed(() => buildTargets(this.audience(), this.selectedIds(), this.lookups()).length);
  audienceInvalid = computed(() => this.audience() !== EventFor.All && this.targetCount() === 0);
  isScheduled = computed(() => this.sentOn() > todayIso());

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private notificationService: NotificationService,
    private academicYearService: AcademicYearService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private teacherService: TeacherService,
    private studentService: StudentService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.buildForm();

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.notificationId.set(this.activatedRoute.snapshot.paramMap.get('id'));
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private buildForm(): void {
    this.form = this.formBuilder.group({
      title: ['', [Validators.required, Validators.maxLength(TITLE_MAX), this.notBlank]],
      description: ['', [Validators.required, Validators.maxLength(DESCRIPTION_MAX), this.notBlank]],
      notificationType: [NotificationType.Message, Validators.required],
      sentOn: [todayIso(), Validators.required],
      imageUrl: ['', [Validators.maxLength(IMAGE_URL_MAX), Validators.pattern(/^https?:\/\/\S+$/i)]]
    });

    this.form.get('notificationType')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(value => this.notificationType.set(Number(value)));
    this.form.get('imageUrl')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(url => this.imageUrl.set(url || ''));
    this.form.get('sentOn')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(date => this.sentOn.set(date || ''));
  }

  private notBlank(control: AbstractControl): ValidationErrors | null {
    return typeof control.value === 'string' && control.value.length > 0 && !control.value.trim() ? { required: true } : null;
  }

  private loadData(): void {
    this.isLoading.set(true);
    const id = this.notificationId();

    forkJoin({
      lookups: loadAudienceLookups({
        academicYearService: this.academicYearService,
        stageService: this.stageService,
        gradeService: this.gradeService,
        sectionService: this.sectionService,
        teacherService: this.teacherService,
        studentService: this.studentService
      }),
      notification: id ? this.notificationService.getById(id) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ lookups, notification }) => {
          this.lookups.set(lookups);
          if (id) {
            if (notification?.status === 200 && notification.data) {
              this.patchForm(notification.data);
            } else {
              this.errorMessage.set(notification?.message || this.translate.instant('notifications.loadNotificationFailed'));
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading notification form:', error);
          this.errorMessage.set(
            this.translate.instant(id ? 'notifications.loadNotificationFailed' : 'schoolEvents.loadLookupsFailed')
          );
          this.isLoading.set(false);
        }
      });
  }

  private patchForm(n: AppNotification): void {
    this.form.patchValue({
      title: n.title,
      description: n.description,
      notificationType: n.notificationType,
      sentOn: (n.sentOn || '').substring(0, 10),
      imageUrl: n.imageUrl || ''
    });

    const audience = inferAudience(n.targets ?? [], this.lookups());
    this.audience.set(audience);
    this.selectedIds.set(selectedIdsFromTargets(audience, n.targets ?? [], this.lookups()));
  }

  setType(value: NotificationType): void {
    this.form.get('notificationType')!.setValue(value);
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.apiErrors.set([]);

    if (this.form.invalid || this.audienceInvalid()) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('schoolEvents.invalidForm'));
      return;
    }

    const v = this.form.value;
    const request: CreateNotificationRequest = {
      title: String(v.title).trim(),
      description: String(v.description).trim(),
      sentOn: v.sentOn,
      notificationType: Number(v.notificationType),
      imageUrl: (v.imageUrl || '').trim() || null,
      targets: buildTargets(this.audience(), this.selectedIds(), this.lookups())
    };

    const id = this.notificationId();
    const request$ = id
      ? this.notificationService.update({ ...request, id })
      : this.notificationService.create(request);
    const successKey = id ? 'notifications.updated' : 'notifications.created';
    const failKey = id ? 'notifications.updateFailed' : 'notifications.createFailed';

    this.isSaving.set(true);
    this.errorMessage.set(null);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(this.translate.instant(successKey));
          // Keep isSaving true to block double submits while navigating away
          setTimeout(() => this.router.navigate(['/notification/notifications']), 800);
        } else {
          this.errorMessage.set(response.message || this.translate.instant(failKey));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving notification:', error);
        this.apiErrors.set(extractApiErrors(error).map(k => this.translateApiError(k)));
        this.errorMessage.set(this.translate.instant(failKey));
        this.isSaving.set(false);
      }
    });
  }

  /** Validation keys are shared with school events (TitleRequired, ...) plus NotificationTypeInvalid */
  private translateApiError(key: string): string {
    for (const prefix of ['notifications.errors', 'schoolEvents.errors']) {
      const translationKey = `${prefix}.${key}`;
      const text = this.translate.instant(translationKey);
      if (text !== translationKey) return text;
    }
    return key;
  }

  cancel(): void {
    this.router.navigate(['/notification/notifications']);
  }
}
