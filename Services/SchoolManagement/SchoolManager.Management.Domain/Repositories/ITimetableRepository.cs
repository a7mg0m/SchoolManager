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
        Task<List<TimetableSlot>> GetSectionSlotsAsync(Guid academicYearId, Guid gradeId, Guid sectionId, CancellationToken cancellationToken = default);
    }
   
}
