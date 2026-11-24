import { ApiResponse } from './stage.model';

/*
 * School transport (Attendance service, gateway: /api/bus-route, /api/bus, /api/bus-assignment, /api/trip).
 * Enums mirror SchoolManager.Attendance.Domain and are serialized by the API as their numbers.
 * Dates are "yyyy-MM-dd" (DateOnly).
 */

/** Mirrors SchoolManager.Attendance.Domain.TripType */
export enum TripType {
  ToSchool = 0,
  ToHome = 1
}

export const TRIP_TYPES: { value: TripType; key: string }[] = [
  { value: TripType.ToSchool, key: 'transport.tripType.toSchool' },
  { value: TripType.ToHome, key: 'transport.tripType.toHome' }
];

/** Mirrors SchoolManager.Attendance.Domain.TripStatus */
export enum TripStatus {
  Planned = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}

export const TRIP_STATUSES: { value: TripStatus; key: string; badge: string }[] = [
  { value: TripStatus.Planned, key: 'transport.tripStatus.planned', badge: 'bg-secondary' },
  { value: TripStatus.InProgress, key: 'transport.tripStatus.inProgress', badge: 'bg-primary' },
  { value: TripStatus.Completed, key: 'transport.tripStatus.completed', badge: 'bg-success' },
  { value: TripStatus.Cancelled, key: 'transport.tripStatus.cancelled', badge: 'bg-danger' }
];

/** Mirrors SchoolManager.Attendance.Domain.TripStudentStatus */
export enum TripStudentStatus {
  Pending = 0,
  Boarded = 1,
  DroppedOff = 2,
  Absent = 3
}

export const TRIP_STUDENT_STATUSES: { value: TripStudentStatus; key: string; badge: string }[] = [
  { value: TripStudentStatus.Pending, key: 'transport.tripStudentStatus.pending', badge: 'bg-secondary' },
  { value: TripStudentStatus.Boarded, key: 'transport.tripStudentStatus.boarded', badge: 'bg-primary' },
  { value: TripStudentStatus.DroppedOff, key: 'transport.tripStudentStatus.droppedOff', badge: 'bg-success' },
  { value: TripStudentStatus.Absent, key: 'transport.tripStudentStatus.absent', badge: 'bg-danger' }
];

/** A stop on a bus route */
export interface BusRouteStop {
  /** Empty guid for a new stop */
  id: string;
  nameAr: string;
  nameEn: string;
  order: number;
  latitude: number | null;
  longitude: number | null;
}

/** A bus route with its stops */
export interface BusRoute {
  id: string;
  nameAr: string;
  nameEn: string;
  description: string | null;
  isActive: boolean;
  stops: BusRouteStop[];
}

/** Body for POST /api/bus-route/create */
export interface CreateBusRouteRequest {
  nameAr: string;
  nameEn: string;
  description: string | null;
  stops: BusRouteStop[];
}

/** Body for PUT /api/bus-route/update (the stops sent replace the saved ones) */
export interface UpdateBusRouteRequest extends CreateBusRouteRequest {
  id: string;
}

/** A school bus */
export interface Bus {
  id: string;
  number: string;
  plateNumber: string;
  capacity: number;
  driverName: string;
  driverPhone: string;
  /** Login name of the driver's account; null for buses saved before driver logins */
  driverLoginName?: string | null;
  busRouteId: string;
  isActive: boolean;
}

/** Body for POST /api/bus/create */
export interface CreateBusRequest {
  number: string;
  plateNumber: string;
  capacity: number;
  driverName: string;
  driverPhone: string;
  busRouteId: string;
  /** Driver's login name (required) */
  driverLoginName: string;
  /** Driver's password: required for a new bus or a bus without a login yet; null keeps the current one */
  driverPassword: string | null;
}

/** Body for PUT /api/bus/update */
export interface UpdateBusRequest extends CreateBusRequest {
  id: string;
}

/** A student riding a bus in an academic year, with their own bus fee */
export interface BusAssignment {
  id: string;
  studentId: string;
  busId: string;
  academicYearId: string;
  busRouteStopId: string | null;
  feeAmount: number;
  currency: string;
  /** "yyyy-MM-dd" */
  startDate: string;
  /** "yyyy-MM-dd" */
  endDate: string | null;
  isActive: boolean;
  /** The Transportation student fee in the Financial service (set once created there) */
  studentFeeId: string | null;
}

/** Body for POST /api/bus-assignment/assign */
export interface AssignStudentsToBusRequest {
  busId: string;
  academicYearId: string;
  busRouteStopId: string | null;
  studentIds: string[];
  feeAmount: number;
  currency: string;
  /** "yyyy-MM-dd" */
  startDate: string;
  /** ISO date-time of the fee's due date */
  dueDate: string;
}

/** Body for PUT /api/bus-assignment/change-fee/{id} */
export interface ChangeBusFeeRequest {
  feeAmount: number;
  currency: string;
}

/** Body for PUT /api/bus-assignment/move/{id} */
export interface MoveStudentToBusRequest {
  busId: string;
  busRouteStopId: string | null;
}

/** Body for PUT /api/bus-assignment/end/{id} */
export interface EndBusAssignmentRequest {
  /** "yyyy-MM-dd" */
  endDate: string;
}

/** A student on a trip */
export interface TripStudent {
  id: string;
  studentId: string;
  status: TripStudentStatus;
  /** ISO date-time */
  boardedAt: string | null;
  /** ISO date-time */
  droppedOffAt: string | null;
}

/** A trip of a bus (one per bus, date and direction) */
export interface Trip {
  id: string;
  busId: string;
  /** "yyyy-MM-dd" */
  date: string;
  type: TripType;
  status: TripStatus;
  /** ISO date-time */
  startedAt: string | null;
  /** ISO date-time */
  endedAt: string | null;
  currentLatitude: number | null;
  currentLongitude: number | null;
  /** ISO date-time */
  lastLocationAt: string | null;
  students: TripStudent[];
}

/** Body for POST /api/trip/create */
export interface CreateTripRequest {
  busId: string;
  /** "yyyy-MM-dd" */
  date: string;
  type: TripType;
}

/** Body for PUT /api/trip/location/{id} (driver) */
export interface UpdateTripLocationRequest {
  latitude: number;
  longitude: number;
}

/** Body for PUT /api/trip/mark-student/{tripId} */
export interface MarkTripStudentRequest {
  studentId: string;
  status: TripStudentStatus;
}

/** Parent app: the running trip of one child (GET /api/trip/my-child/{studentId}) */
export interface TripTracking {
  tripId: string;
  type: TripType;
  startedAt: string | null;
  busId: string;
  busNumber: string;
  plateNumber: string;
  driverName: string;
  driverPhone: string;
  currentLatitude: number | null;
  currentLongitude: number | null;
  lastLocationAt: string | null;
  studentStatus: TripStudentStatus;
  boardedAt: string | null;
  droppedOffAt: string | null;
}

export type BusRouteListResponse = ApiResponse<BusRoute[]>;

export type BusRouteSingleResponse = ApiResponse<BusRoute>;

export type BusListResponse = ApiResponse<Bus[]>;

export type BusSingleResponse = ApiResponse<Bus>;

export type BusAssignmentListResponse = ApiResponse<BusAssignment[]>;

export type BusAssignmentSingleResponse = ApiResponse<BusAssignment>;

/** data: ids of the new assignments */
export type AssignStudentsToBusResponse = ApiResponse<string[]>;

export type TripListResponse = ApiResponse<Trip[]>;

export type TripSingleResponse = ApiResponse<Trip>;

export type TripTrackingResponse = ApiResponse<TripTracking | null>;
