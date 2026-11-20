namespace SchoolManager.Attendance.Domain;

/// <summary>Attendance of a student for a whole school day</summary>
public enum DailyAttendanceStatus { Present, Absent, PartiallyPresent, Excused }

/// <summary>Attendance of a student in one lesson (timetable slot)</summary>
public enum PeriodAttendanceStatus { Present, Absent, Late, Excused }

/// <summary>Who recorded the attendance: the teacher in class, or the school office (admin dashboard)</summary>
public enum AttendanceSource { Teacher, Admin }

/// <summary>Rating of the teacher's evaluation (focus / interaction / behavior)</summary>
public enum EvaluationRating { Weak = 1, Acceptable = 2, Good = 3, VeryGood = 4, Excellent = 5 }

public enum HealthStatus { Good, Tired, Sick }

public enum EvaluationRequestStatus { Pending, Evaluated, Cancelled }

public enum TripType { ToSchool, ToHome }

public enum TripStatus { Planned, InProgress, Completed, Cancelled }

public enum TripStudentStatus { Pending, Boarded, DroppedOff, Absent }
