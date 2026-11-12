import { Component, computed, input, model, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { EVENT_FOR_OPTIONS, EventFor } from '../../core/models/school-event.model';
import { AudienceLookups, EMPTY_LOOKUPS, localizedFullName, localizedName } from './audience';

/** One selectable row in the picker */
interface PickerItem {
  id: string;
  label: string;
  sub?: string;
  /** Group label (grades by stage, sections by grade) – or the grade id for students (used by the grade filter) */
  group?: string;
}

/**
 * Audience picker shared by the school event and notification forms.
 *
 * <app-audience-picker [(audience)]="eventFor" [(selectedIds)]="selectedIds" [lookups]="lookups()" />
 *
 * The parent turns the selection into targets with buildTargets() and validates it.
 */
@Component({
  selector: 'app-audience-picker',
  standalone: false,
  styleUrl: './audience-picker.component.scss',
  templateUrl: './audience-picker.component.html',
})
export class AudiencePickerComponent {
  audience = model<EventFor>(EventFor.All);
  selectedIds = model<Set<string>>(new Set());

  lookups = input<AudienceLookups>(EMPTY_LOOKUPS);
  lang = input<string>('en');
  /** Show the "select at least one" error (parent sets it after a submit attempt) */
  showError = input(false);
  /** Hint shown for "All" (differs between events and notifications) */
  allHintKey = input('schoolEvents.allHint');

  search = signal('');
  gradeFilter = signal('');

  readonly options = EVENT_FOR_OPTIONS;
  readonly EventFor = EventFor;

  grades = computed(() => this.lookups().grades);

  private items = computed<PickerItem[]>(() => {
    const l = this.lookups();
    const lang = this.lang();
    const stages = new Map(l.stages.map(s => [s.id, s]));
    const grades = new Map(l.grades.map(g => [g.id, g]));
    const sections = new Map(l.sections.map(s => [s.id, s]));

    switch (this.audience()) {
      case EventFor.Stage:
        return l.stages.map(s => ({
          id: s.id,
          label: localizedName(s, lang),
          sub: this.translate.instant('schoolEvents.gradesCount', { count: l.grades.filter(g => g.stageId === s.id).length })
        }));
      case EventFor.Grade:
        return l.grades.map(g => ({ id: g.id, label: localizedName(g, lang), group: localizedName(stages.get(g.stageId), lang) }));
      case EventFor.Section:
        return l.sections.map(s => ({ id: s.id, label: localizedName(s, lang), group: localizedName(grades.get(s.gradeId), lang) }));
      case EventFor.Teacher:
        return l.teachers
          .filter(t => t.isActive !== false || this.selectedIds().has(t.id))
          .map(t => ({ id: t.id, label: localizedFullName(t, lang), sub: t.phoneNumber }));
      case EventFor.Student:
        return l.students.map(s => ({
          id: s.id,
          label: localizedFullName(s, lang),
          sub: `${localizedName(grades.get(s.gradeId), lang)} / ${localizedName(sections.get(s.sectionId), lang)}`,
          group: s.gradeId
        }));
      default:
        return [];
    }
  });

  /** Rows after search (and the grade filter for students) */
  visibleItems = computed(() => {
    const term = this.search().trim().toLowerCase();
    const gradeId = this.gradeFilter();
    const isStudent = this.audience() === EventFor.Student;

    return this.items().filter(item => {
      if (isStudent && gradeId && item.group !== gradeId) return false;
      const groupText = isStudent ? '' : item.group ?? '';
      if (term && !`${item.label} ${item.sub ?? ''} ${groupText}`.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  /** Visible rows grouped (grades by stage, sections by grade); other types are one flat list */
  groups = computed(() => {
    const grouped = this.audience() === EventFor.Grade || this.audience() === EventFor.Section;
    const map = new Map<string, PickerItem[]>();
    for (const item of this.visibleItems()) {
      const key = grouped ? item.group || '-' : '';
      map.set(key, [...(map.get(key) ?? []), item]);
    }
    return [...map].map(([label, items]) => ({ label, items }));
  });

  allVisibleSelected = computed(() => {
    const visible = this.visibleItems();
    const selected = this.selectedIds();
    return visible.length > 0 && visible.every(i => selected.has(i.id));
  });

  constructor(private translate: TranslateService) {}

  setAudience(value: EventFor): void {
    if (value === this.audience()) return;
    this.audience.set(value);
    this.selectedIds.set(new Set());
    this.search.set('');
    this.gradeFilter.set('');
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  toggle(id: string): void {
    const next = new Set(this.selectedIds());
    next.has(id) ? next.delete(id) : next.add(id);
    this.selectedIds.set(next);
  }

  toggleAllVisible(): void {
    const next = new Set(this.selectedIds());
    const visible = this.visibleItems();
    if (this.allVisibleSelected()) {
      visible.forEach(i => next.delete(i.id));
    } else {
      visible.forEach(i => next.add(i.id));
    }
    this.selectedIds.set(next);
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  onGradeFilter(event: Event): void {
    this.gradeFilter.set((event.target as HTMLSelectElement).value);
  }

  gradeName(id: string): string {
    return localizedName(this.lookups().grades.find(g => g.id === id), this.lang());
  }
}
