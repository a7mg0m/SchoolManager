import { ApiResponse } from './stage.model';
import { EventTargetInput } from './school-event.model';

/**
 * Mirrors SchoolManager.Notification.Domain.NotificationTypeEnum (serialized as its number).
 */
export enum NotificationType {
  Homework = 1,
  Exam = 2,
  ExamResult = 3,
  Event = 4,
  Message = 5
}

export const NOTIFICATION_TYPES: { value: NotificationType; key: string; icon: string; badge: string }[] = [
  { value: NotificationType.Homework, key: 'notifications.types.homework', icon: 'pe-7s-notebook', badge: 'bg-info text-dark' },
  { value: NotificationType.Exam, key: 'notifications.types.exam', icon: 'pe-7s-note2', badge: 'bg-warning text-dark' },
  { value: NotificationType.ExamResult, key: 'notifications.types.examResult', icon: 'pe-7s-medal', badge: 'bg-success' },
  { value: NotificationType.Event, key: 'notifications.types.event', icon: 'pe-7s-date', badge: 'bg-primary' },
  { value: NotificationType.Message, key: 'notifications.types.message', icon: 'pe-7s-chat', badge: 'bg-secondary' }
];

/** Recipient row (backend NotificationTargetDto); one of the ids is set */
export interface NotificationTarget {
  id: string;
  notificationId: string;
  gradeId: string | null;
  sectionId: string | null;
  teacherId: string | null;
  studentId: string | null;
}

/** Notification (backend NotificationDto). No targets = the whole school. */
export interface AppNotification {
  id: string;
  title: string;
  description: string;
  /** yyyy-MM-dd – the day the notification is (or was) sent */
  sentOn: string;
  notificationType: NotificationType;
  imageUrl: string | null;
  /** ISO date-time */
  createdOn: string;
  targets: NotificationTarget[];
}

/** Body for POST /api/notification/create (backend CreateNotificationDto) */
export interface CreateNotificationRequest {
  title: string;
  description: string;
  /** yyyy-MM-dd */
  sentOn: string;
  notificationType: NotificationType;
  imageUrl: string | null;
  /** Empty = the whole school */
  targets: EventTargetInput[];
}

/** Body for PUT /api/notification/update (backend UpdateNotificationCommand); targets replace the existing ones */
export interface UpdateNotificationRequest extends CreateNotificationRequest {
  id: string;
}

export type NotificationListResponse = ApiResponse<AppNotification[]>;
export type NotificationSingleResponse = ApiResponse<AppNotification>;
export type NotificationTargetListResponse = ApiResponse<NotificationTarget[]>;
export type NotificationTargetSingleResponse = ApiResponse<NotificationTarget>;
