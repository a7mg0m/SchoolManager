import { Component, inject } from '@angular/core';
import { SchoolService } from '../../core/school.service';

@Component({
  selector: 'app-classes',
  standalone: true,
  template: `
    <header class="page-head">
      <h1>الفصول</h1>
    </header>

    <ul class="class-list">
      @for (c of school.classes(); track c.id) {
        <li>
          <div class="class-main">
            <strong>{{ c.name }}</strong>
            <span class="muted">{{ c.grade }} · المعلم: {{ c.teacher }}</span>
          </div>
          <div class="class-count">
            <span>{{ school.countActiveIn(c.id) }} / {{ c.capacity }}</span>
            <div class="meter">
              <span [style.width.%]="(school.countActiveIn(c.id) / c.capacity) * 100"></span>
            </div>
          </div>
        </li>
      } @empty {
        <li class="muted">لا توجد فصول مضافة بعد.</li>
      }
    </ul>
  `,
  styles: [`
    .page-head { margin-bottom: 1.5rem; }
    .class-list { list-style: none; padding: 0; margin: 0; border-top: 1px solid var(--ink); }
    .class-list li {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      padding: 1.1rem 0;
      border-bottom: 1px solid var(--rule);
      flex-wrap: wrap;
    }
    .class-main { display: flex; flex-direction: column; }
    .muted { color: var(--muted); font-size: .9rem; }
    .class-count { display: flex; align-items: center; gap: .75rem; min-width: 180px; }
    .meter { flex: 1; height: 6px; background: var(--rule); overflow: hidden; }
    .meter span { display: block; height: 100%; background: var(--chalk); }
  `],
})
export class ClassesComponent {
  readonly school = inject(SchoolService);
}
