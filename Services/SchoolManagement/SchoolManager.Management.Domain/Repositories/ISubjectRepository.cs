using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public interface ISubjectRepository : IRepository<Subject>
    {
        Task<Subject?> GetByIdWithTeachersAsync(Guid id);

        /// <summary>Subjects linked to the teacher or taught by them in a timetable slot, with their grade</summary>
        Task<List<Subject>> GetForTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);
    }
   
}
