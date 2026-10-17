import { Injectable, computed, signal } from '@angular/core';
import { ClassRoom, Student } from './models';

/**
 * Holds the school's data in memory with signals.
 * Replace the seed data and the mutation methods with HTTP calls
 * to your backend when you connect the template to an API.
 */
@Injectable({ providedIn: 'root' })
export class SchoolService {
  private readonly _classes = signal<ClassRoom[]>([
    { id: 1, name: 'الأول أ', grade: 'الصف الأول الابتدائي', teacher: 'منى السيد', capacity: 30 },
    { id: 2, name: 'الثاني ب', grade: 'الصف الثاني الابتدائي', teacher: 'أحمد فؤاد', capacity: 28 },
    { id: 3, name: 'الثالث أ', grade: 'الصف الثالث الابتدائي', teacher: 'هدى محمود', capacity: 25 },
  ]);

  private readonly _students = signal<Student[]>([
    { id: 1, name: 'يوسف عمر', classId: 1, guardian: 'عمر حسن', phone: '01000000001', status: 'active' },
    { id: 2, name: 'ليان خالد', classId: 1, guardian: 'خالد نبيل', phone: '01000000002', status: 'active' },
    { id: 3, name: 'مريم طارق', classId: 2, guardian: 'طارق سامي', phone: '01000000003', status: 'active' },
    { id: 4, name: 'آدم شريف', classId: 3, guardian: 'شريف عادل', phone: '01000000004', status: 'active' },
    { id: 5, name: 'نور الدين', classId: 2, guardian: 'سعيد ياسر', phone: '01000000005', status: 'transferred' },
  ]);

  readonly classes = this._classes.asReadonly();
  readonly students = this._students.asReadonly();

  readonly activeStudents = computed(() =>
    this._students().filter((s) => s.status === 'active')
  );

  readonly totalCapacity = computed(() =>
    this._classes().reduce((sum, c) => sum + c.capacity, 0)
  );

  readonly utilization = computed(() => {
    const cap = this.totalCapacity();
    return cap ? Math.round((this.activeStudents().length / cap) * 100) : 0;
  });

  classById(id: number): ClassRoom | undefined {
    return this._classes().find((c) => c.id === id);
  }

  countActiveIn(classId: number): number {
    return this.activeStudents().filter((s) => s.classId === classId).length;
  }

  addStudent(data: Omit<Student, 'id' | 'status'>): void {
    const id = Math.max(0, ...this._students().map((s) => s.id)) + 1;
    this._students.update((list) => [...list, { ...data, id, status: 'active' }]);
  }

  setStatus(id: number, status: Student['status']): void {
    this._students.update((list) =>
      list.map((s) => (s.id === id ? { ...s, status } : s))
    );
  }
}
