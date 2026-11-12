using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public interface IStudentRepository : IRepository<Student>
    {
        /// <summary>
        /// Returns the subset of the given national IDs that already belong to a student.
        /// </summary>
        Task<List<string>> GetExistingNationalIdsAsync(IEnumerable<string> nationalIds, CancellationToken cancellationToken = default);
    }

}
