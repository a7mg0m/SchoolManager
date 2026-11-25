using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public sealed class Timetable : BaseEntity
    {
        public Guid StageId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid AcademicYearId { get; private set; }

        /// <summary>Semester of the academic year; null for timetables saved before semesters (used in every semester)</summary>
        public Guid? SemesterId { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        /// <summary>When the first period of a work day starts (the same for every day); null when not set</summary>
        public TimeOnly? DayStartTime { get; private set; }

        /// <summary>The week: work days, breaks, slot duration and number of slots (stored as JSON with the timetable)</summary>
        public List<TimetableWorkDay> WorkDays { get; private set; } = new();

        // Navigation
        public Stage? Stage { get; private set; }
        public Grade? Grade { get; private set; }
        public AcademicYear? AcademicYear { get; private set; }
        public Semester? Semester { get; private set; }
        public List<TimetableSlot> Slots { get; private set; } = new();

        private Timetable() { }

        public Timetable(Guid stageId, Guid gradeId, Guid academicYearId, Guid? semesterId, AuditInfo auditInfo)
        {
            StageId = stageId;
            GradeId = gradeId;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;

            AuditInfo = auditInfo;
        }

        public void AddSlot(TimetableSlot slot)
        {
            Slots.Add(slot);
        }

        public void RemoveSlot(TimetableSlot slot)
        {
            Slots.Remove(slot);
        }

        /// <summary>Replaces the week settings (one row per day, ordered Sunday → Saturday)</summary>
        public void SetSchedule(TimeOnly? dayStartTime, IEnumerable<TimetableWorkDay> workDays)
        {
            DayStartTime = dayStartTime;
            WorkDays = workDays.OrderBy(d => d.Day).ToList();
        }

        public void Update(Guid stageId, Guid gradeId, Guid academicYearId, Guid? semesterId, AuditInfo auditInfo)
        {
            StageId = stageId;
            GradeId = gradeId;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;
            AuditInfo = auditInfo;
        }
               
    }

}
