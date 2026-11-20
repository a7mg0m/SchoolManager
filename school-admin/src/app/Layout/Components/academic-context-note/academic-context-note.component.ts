import { Component, Input } from '@angular/core';
import { AcademicContextService } from '../../../core/services/academic-context.service';

/**
 * "Academic year: 2026-2027 · Semester: First" — the header's choice a page is showing / saving with.
 * [semester]=false hides the semester (pages that only use the year).
 */
@Component({
  selector: 'app-academic-context-note',
  standalone: false,
  template: `
    @if (academic.year(); as year) {
      <div class="academic-context-note d-flex flex-wrap align-items-center gap-2 mb-3 small">
        <i class="pe-7s-date text-primary fs-5"></i>
        <span class="badge bg-primary-subtle text-primary-emphasis border border-primary-subtle">
          {{ 'header.academicYear' | translate }}: {{ year.name }}
        </span>
        @if (semester) {
          <span class="badge bg-info-subtle text-info-emphasis border border-info-subtle">
            {{ 'header.semester' | translate }}: {{ academic.semester()?.name ?? ('header.noSemesters' | translate) }}
          </span>
        }
        <span class="text-muted">{{ 'common.fromHeader' | translate }}</span>
      </div>
    }
  `
})
export class AcademicContextNoteComponent {
  /** Show the semester too */
  @Input() semester = true;

  constructor(public academic: AcademicContextService) {
    this.academic.load();
  }
}
