import { Component, inject } from '@angular/core';
import { SchoolService } from '../../core/school.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: `
    <header class="page-head">
      <p class="date">{{ today }}</p>
      <h1>نظرة عامة</h1>
    </header>

    <section class="hero">
      <div class="big">{{ school.activeStudents().length }}</div>
      <div class="hero-text">
        <strong>طالب مسجّل حاليًا</strong>
        <span>من أصل {{ school.totalCapacity() }} مقعد متاح، بنسبة إشغال {{ school.utilization() }}%</span>
      </div>
    </section>

    <section class="block">
      <h2>الفصول</h2>
      <table>
        <thead>
          <tr>
            <th>الفصل</th>
            <th>المعلم</th>
            <th>الطلاب</th>
            <th>الإشغال</th>
          </tr>
        </thead>
        <tbody>
          @for (c of school.classes(); track c.id) {
            <tr>
              <td>{{ c.name }}</td>
              <td>{{ c.teacher }}</td>
              <td>{{ school.countActiveIn(c.id) }} / {{ c.capacity }}</td>
              <td>
                <div class="meter" [attr.aria-label]="'إشغال ' + c.name">
                  <span [style.width.%]="(school.countActiveIn(c.id) / c.capacity) * 100"></span>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
  styles: [`
    .page-head { margin-bottom: 2rem; }
    .date { color: var(--muted); margin: 0 0 .25rem; font-size: .9rem; }
    .hero {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      padding: 1.5rem 0 2rem;
      border-bottom: 3px solid var(--ochre);
      margin-bottom: 2.5rem;
    }
    .big { font-size: 5.5rem; font-weight: 800; line-height: 1; color: var(--ink); }
    .hero-text { display: flex; flex-direction: column; gap: .25rem; }
    .hero-text strong { font-size: 1.25rem; }
    .hero-text span { color: var(--ink-soft); }
    .block { margin-top: 2rem; }
    .meter {
      width: 140px;
      height: 6px;
      background: var(--rule);
      border-radius: 1px;
      overflow: hidden;
    }
    .meter span { display: block; height: 100%; background: var(--chalk); }
  `],
})
export class DashboardComponent {
  readonly school = inject(SchoolService);
  readonly today = new Date().toLocaleDateString('ar-EG', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}
