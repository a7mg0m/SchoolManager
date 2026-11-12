import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { TeacherService } from '../../core/services/teacher.service';
import { StudentService } from '../../core/services/student.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { Teacher } from '../../core/models/teacher.model';
import { Student } from '../../core/models/student.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { EventFor, EventTargetInput } from '../../core/models/school-event.model';

/**
 * Shared audience helpers for the Notification service screens (school events, notifications).
 * Both store recipients as rows with one of grade/section/teacher/student ids set.
 */
export type TargetIds = Pick<EventTargetInput, 'gradeId' | 'sectionId' | 'teacherId' | 'studentId'>;

/** Reference data needed to pick and describe an audience */
export interface AudienceLookups {
  academicYears: AcademicYear[];
  stages: Stage[];
  grades: Grade[];
  sections: Section[];
  teachers: Teacher[];
  students: Student[];
}

export const EMPTY_LOOKUPS: AudienceLookups = {
  academicYears: [],
  stages: [],
  grades: [],
  sections: [],
  teachers: [],
  students: []
};

/** Loads all lookups; any failing call yields an empty list instead of failing the page */
export function loadAudienceLookups(services: {
  academicYearService: AcademicYearService;
  stageService: StageService;
  gradeService: GradeService;
  sectionService: SectionService;
  teacherService: TeacherService;
  studentService: StudentService;
}): Observable<AudienceLookups> {
  const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
    obs.pipe(
      map(r => (r.status === 200 && r.data ? r.data : [])),
      catchError(() => of([] as T[]))
    );

  return forkJoin({
    academicYears: list(services.academicYearService.getAll()),
    stages: list(services.stageService.getAllStages()),
    grades: list(services.gradeService.getAllGrades()),
    sections: list(services.sectionService.getAll()),
    teachers: list(services.teacherService.getAll()),
    students: list(services.studentService.getAll())
  });
}

/**
 * The backend stores targets as grade/section/teacher/student ids only (no stage id).
 * A "Stage" event is therefore saved as one grade target per grade of each chosen stage.
 */
export function buildTargets(eventFor: EventFor, selectedIds: Set<string>, lookups: AudienceLookups): EventTargetInput[] {
  const empty: EventTargetInput = { gradeId: null, sectionId: null, teacherId: null, studentId: null };
  const ids = [...selectedIds];

  switch (eventFor) {
    case EventFor.Stage:
      return lookups.grades
        .filter(g => selectedIds.has(g.stageId))
        .map(g => ({ ...empty, gradeId: g.id }));
    case EventFor.Grade:
      return ids.map(id => ({ ...empty, gradeId: id }));
    case EventFor.Section: {
      const sections = new Map(lookups.sections.map(s => [s.id, s]));
      return ids.map(id => ({ ...empty, gradeId: sections.get(id)?.gradeId ?? null, sectionId: id }));
    }
    case EventFor.Teacher:
      return ids.map(id => ({ ...empty, teacherId: id }));
    case EventFor.Student:
      return ids.map(id => ({ ...empty, studentId: id }));
    default:
      return [];
  }
}

/** Reverse of buildTargets: which ids the form should show as selected */
export function selectedIdsFromTargets(eventFor: EventFor, targets: TargetIds[], lookups: AudienceLookups): Set<string> {
  switch (eventFor) {
    case EventFor.Stage: {
      // A stage counts as selected when any of its grades is targeted
      const gradeIds = new Set(targets.map(t => t.gradeId).filter((id): id is string => !!id));
      const stageIds = lookups.grades.filter(g => gradeIds.has(g.id)).map(g => g.stageId);
      return new Set(stageIds);
    }
    case EventFor.Grade:
      return new Set(targets.map(t => t.gradeId).filter((id): id is string => !!id));
    case EventFor.Section:
      return new Set(targets.map(t => t.sectionId).filter((id): id is string => !!id));
    case EventFor.Teacher:
      return new Set(targets.map(t => t.teacherId).filter((id): id is string => !!id));
    case EventFor.Student:
      return new Set(targets.map(t => t.studentId).filter((id): id is string => !!id));
    default:
      return new Set();
  }
}

export function localizedName(item: { nameAr: string; nameEn: string } | undefined, lang: string): string {
  if (!item) return '-';
  return lang === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
}

export function localizedFullName(item: { fullNameAr: string; fullNameEn: string } | undefined, lang: string): string {
  if (!item) return '-';
  return lang === 'ar' ? item.fullNameAr || item.fullNameEn : item.fullNameEn || item.fullNameAr;
}

/**
 * Notifications don't store an audience type, only targets. Infer it:
 * none → All; otherwise the most specific id present. Grade targets that
 * exactly cover whole stages are shown as Stage (that's how stages are saved).
 */
export function inferAudience(targets: TargetIds[], lookups: AudienceLookups): EventFor {
  if (!targets?.length) return EventFor.All;
  if (targets.some(t => t.studentId)) return EventFor.Student;
  if (targets.some(t => t.teacherId)) return EventFor.Teacher;
  if (targets.some(t => t.sectionId)) return EventFor.Section;

  const gradeIds = new Set(targets.map(t => t.gradeId).filter((id): id is string => !!id));
  const stageIds = new Set(lookups.grades.filter(g => gradeIds.has(g.id)).map(g => g.stageId));
  const coveredGrades = lookups.grades.filter(g => stageIds.has(g.stageId));
  const coversWholeStages =
    stageIds.size > 0 && coveredGrades.length === gradeIds.size && coveredGrades.every(g => gradeIds.has(g.id));
  return coversWholeStages ? EventFor.Stage : EventFor.Grade;
}

/** Display names of an audience (empty for "All") */
export function audienceNames(eventFor: EventFor, targets: TargetIds[], lookups: AudienceLookups, lang: string): string[] {
  const ids = selectedIdsFromTargets(eventFor, targets ?? [], lookups);
  const pick = <T extends { id: string }>(list: T[], name: (x: T) => string) =>
    list.filter(x => ids.has(x.id)).map(name);

  switch (eventFor) {
    case EventFor.Stage:
      return pick(lookups.stages, s => localizedName(s, lang));
    case EventFor.Grade:
      return pick(lookups.grades, g => localizedName(g, lang));
    case EventFor.Section: {
      const grades = new Map(lookups.grades.map(g => [g.id, g]));
      return pick(lookups.sections, s => `${localizedName(grades.get(s.gradeId), lang)} / ${localizedName(s, lang)}`);
    }
    case EventFor.Teacher:
      return pick(lookups.teachers, t => localizedFullName(t, lang));
    case EventFor.Student:
      return pick(lookups.students, s => localizedFullName(s, lang));
    default:
      return [];
  }
}

/** yyyy-MM-dd for today in local time */
export function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** "HH:mm:ss" / "HH:mm" → "HH:mm" */
export function toHourMinute(time: string | null | undefined): string {
  return (time || '').substring(0, 5);
}

/** Localized short time (e.g. 9:30 AM) from "HH:mm[:ss]" */
export function formatTime(time: string | null | undefined): string {
  const hm = toHourMinute(time);
  if (!hm) return '';
  const [h, m] = hm.split(':').map(Number);
  const d = new Date();
  d.setHours(h, m, 0, 0);
  return d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
}

/** Localized date from "yyyy-MM-dd" */
export function formatDate(date: string | null | undefined, options?: Intl.DateTimeFormatOptions): string {
  const value = (date || '').substring(0, 10);
  if (!value) return '';
  const d = new Date(value + 'T00:00:00');
  return isNaN(d.getTime())
    ? value
    : d.toLocaleDateString(undefined, options ?? { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' });
}
