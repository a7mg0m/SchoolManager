import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { SchoolEventService } from '../../core/services/school-event.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { EVENT_FOR_OPTIONS, EventAttendanceMark, EventAttendeeType, EventFor, SchoolEvent } from '../../core/models/school-event.model';
import { AudienceLookups, EMPTY_LOOKUPS, formatDate, formatTime, loadAudienceLookups, localizedFullName, localizedName } from '../shared/audience';

/** A mark in the page: present, absent, or not marked yet */
type Mark = boolean | null;

/** An invited person */
interface Invitee {
  key: string;
  type: EventAttendeeType;
  id: string;
  name: string;
  detail: string;
}

type StatusFilter = '' | 'present' | 'absent' | 'unmarked';

const keyOf = (type: EventAttendeeType, id: string) => `${type}:${id.toLowerCase()}`;

/**
 * Event check-in: /notification/school-events/:id/attendance
 * The invited students / teachers (from the event's audience), each marked present or absent on the day.
 * Uses /api/school-event/{id}/attendance (GET marks, PUT changed marks).
 */
@Component({
  selector: 'app-school-event-attendance',
  standalone: false,
  styleUrl: './school-event-attendance.component.scss',
  templateUrl: './school-event-attendance.component.html',
})
export class SchoolEventAttendanceComponent implements OnInit, OnDestroy {
  event = signal<SchoolEvent | null>(null);
  private lookups = signal<AudienceLookups>(EMPTY_LOOKUPS);
  currentLanguage = signal<string>('en');

  isLoading = signal(true);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  savedMessage = signal<string | null>(null);

  /** Marks as saved on the server */
  private saved = signal<Map<string, boolean>>(new Map());
  /** Marks as shown (saved + changes not saved yet) */
  marks = signal<Map<string, Mark>>(new Map());

  searchTerm = signal('');
  typeFilter = signal<EventAttendeeType | 0>(0);
  statusFilter = signal<StatusFilter>('');

  readonly EventAttendeeType = EventAttendeeType;
  readonly formatDate = formatDate;
  readonly formatTime = formatTime;

  private destroy$ = new Subject<void>();
  private eventId = '';

  /** Everyone the event is for, from its audience */
  invitees = computed<Invitee[]>(() => {
    const e = this.event();
    const l = this.lookups();
    const lang = this.currentLanguage();
    if (!e) return [];

    const grades = new Map(l.grades.map(g => [g.id, g]));
    const sections = new Map(l.sections.map(s => [s.id, s]));
    const activeStudents = l.students.filter(s => s.isActive);
    const activeTeachers = l.teachers.filter(t => t.isActive);
    const ids = (pick: (t: SchoolEvent['targets'][number]) => string | null) =>
      new Set(e.targets.map(pick).filter((id): id is string => !!id).map(id => id.toLowerCase()));

    let students: typeof activeStudents = [];
    let teachers: typeof activeTeachers = [];
    switch (e.eventFor) {
      case EventFor.All:
        students = activeStudents;
        teachers = activeTeachers;
        break;
      case EventFor.Stage:
      case EventFor.Grade: {
        const gradeIds = ids(t => t.gradeId);
        students = activeStudents.filter(s => gradeIds.has(s.gradeId.toLowerCase()));
        break;
      }
      case EventFor.Section: {
        const sectionIds = ids(t => t.sectionId);
        students = activeStudents.filter(s => sectionIds.has(s.sectionId.toLowerCase()));
        break;
      }
      case EventFor.Teacher: {
        // No teacher chosen: every teacher
        const teacherIds = ids(t => t.teacherId);
        teachers = teacherIds.size === 0 ? activeTeachers : activeTeachers.filter(t => teacherIds.has(t.id.toLowerCase()));
        break;
      }
      case EventFor.Student: {
        const studentIds = ids(t => t.studentId);
        students = l.students.filter(s => studentIds.has(s.id.toLowerCase()));
        break;
      }
    }

    const teacherLabel = this.translate.instant('eventAttendance.teacher');
    return [
      ...students.map(s => ({
        key: keyOf(EventAttendeeType.Student, s.id),
        type: EventAttendeeType.Student,
        id: s.id,
        name: localizedFullName(s, lang),
        detail: [localizedName(grades.get(s.gradeId), lang), localizedName(sections.get(s.sectionId), lang)].filter(Boolean).join(' / ')
      })),
      ...teachers.map(t => ({
        key: keyOf(EventAttendeeType.Teacher, t.id),
        type: EventAttendeeType.Teacher,
        id: t.id,
        name: localizedFullName(t, lang),
        detail: teacherLabel
      }))
    ].sort((a, b) => a.type - b.type || a.detail.localeCompare(b.detail) || a.name.localeCompare(b.name));
  });

  markOf(key: string): Mark {
    return this.marks().get(key) ?? null;
  }

  visible = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const type = this.typeFilter();
    const status = this.statusFilter();
    const marks = this.marks();
    return this.invitees().filter(p => {
      if (type && p.type !== type) return false;
      const m = marks.get(p.key) ?? null;
      if (status === 'present' && m !== true) return false;
      if (status === 'absent' && m !== false) return false;
      if (status === 'unmarked' && m !== null) return false;
      return !term || `${p.name} ${p.detail}`.toLowerCase().includes(term);
    });
  });

  summary = computed(() => {
    const marks = this.marks();
    const all = this.invitees();
    let present = 0;
    let absent = 0;
    for (const p of all) {
      const m = marks.get(p.key);
      if (m === true) present++;
      else if (m === false) absent++;
    }
    const students = all.filter(p => p.type === EventAttendeeType.Student).length;
    return {
      invited: all.length,
      students,
      teachers: all.length - students,
      present,
      absent,
      unmarked: all.length - present - absent,
      rate: present + absent > 0 ? Math.round((present * 100) / (present + absent)) : null
    };
  });

  /** People whose mark differs from the saved one */
  changes = computed(() => {
    const saved = this.saved();
    const marks = this.marks();
    return this.invitees().filter(p => (marks.get(p.key) ?? null) !== (saved.get(p.key) ?? null));
  });

  eventForKey = computed(() => EVENT_FOR_OPTIONS.find(o => o.value === this.event()?.eventFor)?.key ?? '');

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private translate: TranslateService,
    private languageService: LanguageService,
    private schoolEventService: SchoolEventService,
    private academicYearService: AcademicYearService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private teacherService: TeacherService,
    private studentService: StudentService
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$.pipe(takeUntil(this.destroy$)).subscribe(lang => this.currentLanguage.set(lang));
    this.eventId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    forkJoin({
      event: this.schoolEventService.getById(this.eventId).pipe(catchError(() => of(null))),
      marks: this.schoolEventService.getAttendance(this.eventId).pipe(catchError(() => of(null))),
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
      .subscribe(r => {
        this.isLoading.set(false);
        if (!r.event || r.event.status !== 200 || !r.event.data) {
          this.errorMessage.set(this.translate.instant('eventAttendance.loadFailed'));
          return;
        }
        this.event.set(r.event.data);
        this.lookups.set(r.lookups);
        if (!r.marks || r.marks.status !== 200) {
          this.errorMessage.set(this.translate.instant('eventAttendance.loadMarksFailed'));
        }
        this.applySaved(r.marks?.data ?? []);
      });
  }

  private applySaved(marks: EventAttendanceMark[]): void {
    const saved = new Map(marks.map(m => [keyOf(m.attendeeType, m.attendeeId), m.isPresent]));
    this.saved.set(saved);
    this.marks.set(new Map(saved));
  }

  /** Present / absent; clicking the chosen one again clears it */
  setMark(p: Invitee, value: boolean): void {
    this.savedMessage.set(null);
    const next = new Map(this.marks());
    next.set(p.key, next.get(p.key) === value ? null : value);
    this.marks.set(next);
  }

  /** Marks every person shown (after the filters) */
  markVisible(value: Mark): void {
    this.savedMessage.set(null);
    const next = new Map(this.marks());
    for (const p of this.visible()) next.set(p.key, value);
    this.marks.set(next);
  }

  discardChanges(): void {
    this.marks.set(new Map(this.saved()));
    this.savedMessage.set(null);
  }

  save(): void {
    const changed = this.changes();
    if (changed.length === 0 || this.isSaving()) return;
    this.isSaving.set(true);
    this.errorMessage.set(null);
    const records = changed.map(p => ({ attendeeType: p.type, attendeeId: p.id, isPresent: this.markOf(p.key) }));
    this.schoolEventService
      .saveAttendance(this.eventId, records)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // What the server has now
          const saved = new Map(this.saved());
          for (const r of records) {
            const key = keyOf(r.attendeeType, r.attendeeId);
            if (r.isPresent === null) saved.delete(key);
            else saved.set(key, r.isPresent);
          }
          this.saved.set(saved);
          this.isSaving.set(false);
          this.savedMessage.set(this.translate.instant('eventAttendance.saved', { count: records.length }));
        },
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set(this.translate.instant('eventAttendance.saveFailed'));
        }
      });
  }

  backToList(): void {
    this.router.navigate(['/notification/school-events']);
  }
}
