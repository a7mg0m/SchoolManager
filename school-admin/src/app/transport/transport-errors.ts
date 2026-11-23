import { TranslateService } from '@ngx-translate/core';

/**
 * The transport endpoints of the Attendance API return either a key (validator keys) or an English
 * DomainException sentence. Sentences are mapped to a key here so they can be translated
 * (transport.errors.<key>).
 */
const MESSAGE_KEYS: Record<string, string> = {
  'A completed trip cannot be cancelled.': 'CompletedTripCannotBeCancelled',
  'A student can be marked boarded, dropped off or absent.': 'StudentMarkInvalid',
  'A trip cannot end before it started.': 'TripEndBeforeStart',
  'Academic year is required.': 'AcademicYearIdRequired',
  'Another bus has this plate number.': 'PlateNumberTaken',
  'Bus fee must be greater than zero.': 'FeeAmountMustBeGreaterThanZero',
  'Bus is required.': 'BusIdRequired',
  'Bus not found.': 'BusNotFound',
  'Bus number is required.': 'NumberRequired',
  'Bus route is required.': 'BusRouteIdRequired',
  'Bus route not found.': 'BusRouteNotFound',
  'Capacity must be greater than zero.': 'CapacityMustBeGreaterThanZero',
  'Driver name is required.': 'DriverNameRequired',
  'Driver phone is required.': 'DriverPhoneRequired',
  'Drop-off cannot be before boarding.': 'DropOffBeforeBoarding',
  'Due date is required.': 'DueDateRequired',
  'End date cannot be before the start date.': 'EndDateBeforeStartDate',
  'Latitude and longitude must be given together.': 'CoordinatesIncomplete',
  'Latitude must be between -90 and 90.': 'LatitudeInvalid',
  'Longitude must be between -180 and 180.': 'LongitudeInvalid',
  'Only a planned trip can start.': 'OnlyPlannedTripCanStart',
  'Only a running trip can be completed.': 'OnlyRunningTripCanComplete',
  'Only a student on the bus can be dropped off.': 'OnlyBoardedStudentCanDropOff',
  'Only a waiting student can be marked absent.': 'OnlyWaitingStudentCanBeAbsent',
  'Only a waiting student can board.': 'OnlyWaitingStudentCanBoard',
  'Plate number is required.': 'PlateNumberRequired',
  'Route name is required in Arabic and English.': 'RouteNameRequired',
  'Select at least one student.': 'StudentIdsRequired',
  'Stop name is required in Arabic and English.': 'StopNameRequired',
  'Stop order must be greater than zero.': 'StopOrderInvalid',
  'Stop order must be unique.': 'StopOrderDuplicated',
  'Student fee is required.': 'StudentFeeRequired',
  'Student is required.': 'StudentIdRequired',
  'Students can be added only to a planned or running trip.': 'TripNotOpenForStudents',
  'The assignment has already ended.': 'AssignmentAlreadyEnded',
  'The assignment has ended.': 'AssignmentEnded',
  'The bus already has this trip on this date.': 'TripAlreadyExists',
  'The bus has students assigned. End or move their assignments first.': 'BusHasStudents',
  'The bus is not active.': 'BusNotActive',
  'The route is used by buses. Move the buses to another route or deactivate it.': 'RouteUsedByBuses',
  "The stop is not on the bus's route.": 'StopNotOnRoute',
  'The student is already on this trip.': 'StudentAlreadyOnTrip',
  'The student is not on this trip.': 'StudentNotOnTrip',
  'The student is not one of your children.': 'StudentNotFound',
  'The trip is not running.': 'TripNotRunning',
  'Trip type is not valid.': 'TripTypeInvalid',
  'Your children could not be checked right now. Please try again later.': 'ManagementUnavailable'
};

/** Sentences with a number in them: the number is passed to the translation as {{count}} */
const MESSAGE_PATTERNS: { pattern: RegExp; key: string }[] = [
  { pattern: /^Description cannot exceed (\d+) characters\.$/, key: 'DescriptionTooLong' },
  { pattern: /^The bus has (\d+) free seats\.$/, key: 'NotEnoughSeats' },
  { pattern: /^The bus has (\d+) students assigned; the capacity cannot be lower\.$/, key: 'CapacityBelowAssigned' },
  { pattern: /^(\d+) of the selected students already ride a bus in this academic year\.$/, key: 'StudentsAlreadyAssigned' }
];

/**
 * Text of an API error of the transport pages (e.g. "Bus not found." -> transport.errors.BusNotFound),
 * or the fallback message when the error is unknown / missing.
 */
export function transportApiError(translate: TranslateService, key: string | null | undefined, fallbackKey: string): string {
  if (key) {
    let errorKey = MESSAGE_KEYS[key] ?? key;
    let params: Record<string, string> = {};
    for (const p of MESSAGE_PATTERNS) {
      const match = p.pattern.exec(key);
      if (match) {
        errorKey = p.key;
        params = { count: match[1] };
        break;
      }
    }
    const translationKey = `transport.errors.${errorKey}`;
    const text = translate.instant(translationKey, params);
    if (text !== translationKey) return text;
  }
  return translate.instant(fallbackKey);
}
