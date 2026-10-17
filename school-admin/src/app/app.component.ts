import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">إدارة المدرسة</div>
        <nav>
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">الرئيسية</a>
          <a routerLink="/students" routerLinkActive="active">الطلاب</a>
          <a routerLink="/classes" routerLinkActive="active">الفصول</a>
        </nav>
      </aside>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .shell { display: grid; grid-template-columns: 240px 1fr; min-height: 100%; }
    .sidebar {
      background: var(--ink);
      color: var(--paper);
      padding: 2rem 1.25rem;
    }
    .brand { font-weight: 800; font-size: 1.25rem; margin-bottom: 2.5rem; }
    nav { display: flex; flex-direction: column; gap: .25rem; }
    nav a {
      color: #c9d1da;
      text-decoration: none;
      padding: .5rem .75rem;
      border-inline-start: 2px solid transparent;
    }
    nav a:hover { color: #fff; }
    nav a.active { color: #fff; border-inline-start-color: var(--ochre); }
    .content { padding: 2.5rem 3rem; max-width: 1100px; width: 100%; }
    @media (max-width: 760px) {
      .shell { grid-template-columns: 1fr; }
      .sidebar { padding: 1rem; }
      .brand { margin-bottom: .5rem; }
      nav { flex-direction: row; flex-wrap: wrap; }
      .content { padding: 1.5rem 1rem; }
    }
  `],
})
export class AppComponent {}
