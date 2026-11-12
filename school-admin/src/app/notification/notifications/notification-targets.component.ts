import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NotificationService, NotificationTargetService } from '../../core/services/notification.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { AppNotification, NOTIFICATION_TYPES, NotificationTarget } from '../../core/models/notification.model';
import { EVENT_FOR_OPTIONS, EventFor } from '../../core/models/school-event.model';
import {
  AudienceLookups,
  EMPTY_LOOKUPS,
  formatDate,
  inferAudience,
  loadAudienceLookups,
  localizedFullName,
  localizedName
} from '../shared/audience';

type RecipientKind = 'grade' | 'section' | 'teacher' | 'student';

/** One target row resolved to display names */
interface RecipientRow {
  id: string;
  kind: RecipientKind;
  name: string;
  detail: string;
}

const KIND_ICONS: Record<RecipientKind, string> = {
  grade: 'pe-7s-study',
  section: 'pe-7s-albums',
  teacher: 'pe-7s-id',
  student: 'pe-7s-user'
};

/**
 * Recipients of one notification: /notification/notifications/:id/recipients
 * Uses /api/notification-target/get-by-notification/{id}.
 */
@Component({
  selector: 'app-notification-targets',
  standalone: false,
  styleUrl: './notification-targets.component.scss',
  templateUrl: './notification-targets.component.html',
})
export class NotificationTargetsComponent implements OnInit, OnDestroy {
  notification = signal<AppNotification | null>(null);
  targets = signal<NotificationTarget[]>([]);
  private lookups = signal<AudienceLookups>(EMPTY_LOOKUPS);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  searchTerm = signal('');
  kindFilter = signal<RecipientKind | ''>('');

  readonly kinds: RecipientKind[] = ['grade', 'section', 'teacher', 'student'];
  readonly kindIcons = KIND_ICONS;
  readonly EventFor = EventFor;

  audienceType = computed(() => inferAudience(this.targets(), this.lookups()));
  audienceOption = computed(() => EVENT_FOR_OPTIONS.find(o => o.value === this.audienceType()) ?? EVENT_FOR_OPTIONS[0]);

  rows = computed<RecipientRow[]>(() => {
    const l = this.lookups();
    const lang = this.currentLanguage();
    const stages = new Map(l.stages.map(s => [s.id, s]));
    const grades = new Map(l.grades.map(g => [g.id, g]));
    const sections = new Map(l.sections.map(s => [s.id, s]));
    const teachers = new Map(l.teachers.map(t => [t.id, t]));
    const students = new Map(l.students.map(s => [s.id, s]));
    const unknown = this.translate.instant('notifications.unknownRecipient');

    return this.targets().map<RecipientRow>(t => {
      if (t.studentId) {
        const s = students.get(t.studentId);
        return {
          id: t.id,
          kind: 'student',
          name: s ? localizedFullName(s, lang) : unknown,
          detail: s ? `${localizedName(grades.get(s.gradeId), lang)} / ${localizedName(sections.get(s.sectionId), lang)}` : t.studentId
        };
      }
      if (t.teacherId) {
        const teacher = teachers.get(t.teacherId);
        return { id: t.id, kind: 'teacher', name: teacher ? localizedFullName(teacher, lang) : unknown, detail: teacher?.phoneNumber || '' };
      }
      if (t.sectionId) {
        const section = sections.get(t.sectionId);
        const grade = grades.get(section?.gradeId ?? t.gradeId ?? '');
        return { id: t.id, kind: 'section', name: section ? localizedName(section, lang) : unknown, detail: grade ? localizedName(grade, lang) : '' };
      }
      const grade = grades.get(t.gradeId ?? '');
      return {
        id: t.id,
        kind: 'grade',
        name: grade ? localizedName(grade, lang) : unknown,
        detail: grade ? localizedName(stages.get(grade.stageId), lang) : ''
      };
    });
  });

  visibleRows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const kind = this.kindFilter();
    return this.rows().filter(r => {
      if (kind && r.kind !== kind) return false;
      if (term && !`${r.name} ${r.detail}`.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  kindCounts = computed(() => {
    const counts = new Map<RecipientKind, number>();
    for (const r of this.rows()) counts.set(r.kind, (counts.get(r.kind) ?? 0) + 1);
    return counts;
  });

  /**
   * Estimated reach: distinct students covered by grade/section/student targets, and targeted teachers.
   * No targets = the whole school.
   */
  reach = computed(() => {
    const l = this.lookups();
    const targets = this.targets();
    if (targets.length === 0) {
      return { students: l.students.length, teachers: l.teachers.filter(t => t.isActive !== false).length };
    }

    const gradeIds = new Set<string>();
    const sectionIds = new Set<string>();
    const studentIds = new Set<string>();
    const teacherIds = new Set<string>();
    for (const t of targets) {
      if (t.studentId) studentIds.add(t.studentId);
      else if (t.teacherId) teacherIds.add(t.teacherId);
      else if (t.sectionId) sectionIds.add(t.sectionId);
      else if (t.gradeId) gradeIds.add(t.gradeId);
    }

    const reached = l.students.filter(s => studentIds.has(s.id) || sectionIds.has(s.sectionId) || gradeIds.has(s.gradeId));
    return { students: reached.length, teachers: teacherIds.size };
  });

  private destroy$ = new Subject<void>();

  constructor(
    private notificationService: NotificationService,
    private notificationTargetService: NotificationTargetService,
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
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.loadData(this.activatedRoute.snapshot.paramMap.get('id') ?? '');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      notification: this.notificationService.getById(id),
      targets: this.notificationTargetService.getByNotification(id),
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
        next: ({ notification, targets, lookups }) => {
          this.lookups.set(lookups);
          this.notification.set(notification.status === 200 ? notification.data : null);
          if (targets.status === 200) {
            this.targets.set(targets.data || []);
          } else {
            this.errorMessage.set(targets.message || this.translate.instant('notifications.loadRecipientsFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading notification recipients:', error);
          this.errorMessage.set(this.translate.instant('notifications.loadRecipientsFailed'));
          this.isLoading.set(false);
        }
      });
  }

  onSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  toggleKind(kind: RecipientKind): void {
    this.kindFilter.set(this.kindFilter() === kind ? '' : kind);
  }

  kindCount(kind: RecipientKind): number {
    return this.kindCounts().get(kind) ?? 0;
  }

  typeInfo(n: AppNotification) {
    return NOTIFICATION_TYPES.find(t => t.value === n.notificationType) ?? NOTIFICATION_TYPES[NOTIFICATION_TYPES.length - 1];
  }

  formatDate(value: string): string {
    return formatDate(value);
  }

  backToList(): void {
    this.router.navigate(['/notification/notifications']);
  }

  editNotification(): void {
    const n = this.notification();
    if (n) this.router.navigate(['/notification/notifications/save', n.id]);
  }
}
