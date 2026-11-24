import { ApiResponse } from './stage.model';

/** Students' days recorded on the date, by status (Attendance service) */
export interface DashboardAttendance {
  recorded: number;
  present: number;
  partiallyPresent: number;
  absent: number;
  excused: number;
  /** Classes with at least one student recorded on the date */
  sectionsRecorded: number;
}

export interface DashboardBus {
  busId: string;
  number: string;
  driverName: string;
  capacity: number;
  riders: number;
  hasDriverLogin: boolean;
}

export interface DashboardTransport {
  buses: number;
  activeBuses: number;
  /** Seats of the active buses */
  seats: number;
  /** Students with an active bus assignment */
  riders: number;
  driversWithoutLogin: number;
  tripsPlanned: number;
  tripsInProgress: number;
  tripsCompleted: number;
  tripsCancelled: number;
  /** Active buses, fullest first */
  busOccupancy: DashboardBus[];
}

/** GET /api/attendance-dashboard/summary */
export interface DashboardSummary {
  /** "yyyy-MM-dd" */
  date: string;
  attendance: DashboardAttendance;
  transport: DashboardTransport;
}

export type DashboardSummaryResponse = ApiResponse<DashboardSummary>;
