using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public interface ITimetableRepository : IRepository<Timetable>
    {
        /// <summary>
        /// Slots of one section in the timetable of its grade and academic year,
        /// with Subject and Teacher loaded (read-only).
        /// </summary>
        /// <summary>
        /// A class's lessons in an academic year and semester (null: the semester running today). The semester's own
        /// timetables are used; when it has none, the year's timetables saved without a semester.
        /// </summary>
        /// <summary>
        /// A teacher's lessons in an academic year and semester, with subject / grade / section. For every grade the
        /// semester's own timetables are used; when the grade has none, its timetables saved without a semester.
        /// </summary>
        Task<List<TimetableSlot>> GetTeacherSlotsAsync(Guid teacherId, Guid academicYearId, Guid? semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// The lessons of these teachers in the other timetables of an academic year that run in the semester (its own
        /// and the whole-year ones), except the timetable excludeTimetableId (read-only).
        /// </summary>
        Task<List<TimetableSlot>> GetTeachersOtherSlotsAsync(IReadOnlyCollection<Guid> teacherIds, Guid academicYearId, Guid? semesterId,
            Guid? excludeTimetableId, CancellationToken cancellationToken = default);

        Task<List<TimetableSlot>> GetSectionSlotsAsync(Guid academicYearId, Guid? semesterId, Guid gradeId, Guid sectionId, CancellationToken cancellationToken = default);
    }
   
}
