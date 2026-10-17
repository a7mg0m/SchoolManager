export type StudentStatus = 'active' | 'transferred' | 'graduated';

export interface ClassRoom {
  id: number;
  name: string;
  grade: string;
  teacher: string;
  capacity: number;
}

export interface Student {
  id: number;
  name: string;
  classId: number;
  guardian: string;
  phone: string;
  status: StudentStatus;
}
