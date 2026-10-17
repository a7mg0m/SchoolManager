import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SchoolService } from '../../core/school.service';

@Component({
  selector: 'app-students',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <header class="page-head">
      <h1>الطلاب</h1>
      <span class="count">{{ filtered().length }} طالب</span>
    </header>

    <div class="toolbar">
      <input
        type="search"
        placeholder="ابحث باسم الطالب"
        aria-label="ابحث باسم الطالب"
        [value]="query()"
        (input)="query.set($any($event.target).value)"
      />
      <select
        aria-label="تصفية حسب الفصل"
        [value]="classFilter()"
        (change)="classFilter.set(+$any($event.target).value)"
      >
        <option value="0">كل الفصول</option>
        @for (c of school.classes(); track c.id) {
          <option [value]="c.id">{{ c.name }}</option>
        }
      </select>
    </div>

    <table>
      <thead>
        <tr>
          <th>الاسم</th>
          <th>الفصل</th>
          <th>ولي الأمر</th>
          <th>الهاتف</th>
          <th><span class="sr-only">إجراء</span></th>
        </tr>
      </thead>
      <tbody>
        @for (s of filtered(); track s.id) {
          <tr>
            <td>{{ s.name }}</td>
            <td>{{ school.classById(s.classId)?.name }}</td>
            <td>{{ s.guardian }}</td>
            <td dir="ltr">{{ s.phone }}</td>
            <td class="actions">
              <button class="ghost" (click)="school.setStatus(s.id, 'transferred')">نقل</button>
            </td>
          </tr>
        } @empty {
          <tr><td colspan="5" class="empty">لا توجد نتائج مطابقة.</td></tr>
        }
      </tbody>
    </table>

    <section class="block">
      <h2>إضافة طالب</h2>
      <form [formGroup]="form" (ngSubmit)="submit()" class="add-form">
        <label>
          الاسم
          <input formControlName="name" />
          @if (form.controls.name.touched && form.controls.name.invalid) {
            <span class="error">الاسم مطلوب</span>
          }
        </label>

        <label>
          الفصل
          <select formControlName="classId">
            @for (c of school.classes(); track c.id) {
              <option [ngValue]="c.id">{{ c.name }}</option>
            }
          </select>
        </label>

        <label>
          ولي الأمر
          <input formControlName="guardian" />
          @if (form.controls.guardian.touched && form.controls.guardian.invalid) {
            <span class="error">اسم ولي الأمر مطلوب</span>
          }
        </label>

        <label>
          رقم الهاتف
          <input formControlName="phone" dir="ltr" inputmode="tel" />
          @if (form.controls.phone.touched && form.controls.phone.invalid) {
            <span class="error">أدخل رقم هاتف صحيحًا</span>
          }
        </label>

        <div class="form-actions">
          <button type="submit">حفظ الطالب</button>
        </div>
      </form>
    </section>
  `,
  styles: [`
    .page-head { display: flex; align-items: baseline; gap: 1rem; margin-bottom: 1.5rem; }
    .count { color: var(--muted); }
    .toolbar { display: flex; gap: .75rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .actions { text-align: end; }
    .empty { text-align: center; color: var(--muted); padding: 2rem 0; }
    .sr-only {
      position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0);
    }
    .block { margin-top: 3rem; }
    .add-form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      align-items: end;
    }
    label { display: flex; flex-direction: column; gap: .35rem; font-size: .9rem; color: var(--ink-soft); }
    label input, label select { min-width: 0; width: 100%; }
    .form-actions { display: flex; justify-content: flex-start; }
  `],
})
export class StudentsComponent {
  readonly school = inject(SchoolService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly query = signal('');
  readonly classFilter = signal(0);

  readonly filtered = computed(() => {
    const q = this.query().trim();
    const cid = this.classFilter();
    return this.school
      .activeStudents()
      .filter((s) => (!cid || s.classId === cid) && (!q || s.name.includes(q)));
  });

  readonly form = this.fb.group({
    name: ['', Validators.required],
    classId: [1, Validators.required],
    guardian: ['', Validators.required],
    phone: ['', [Validators.required, Validators.pattern(/^[0-9+]{8,15}$/)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.school.addStudent(this.form.getRawValue());
    this.form.reset({ name: '', classId: this.form.getRawValue().classId, guardian: '', phone: '' });
  }
}
