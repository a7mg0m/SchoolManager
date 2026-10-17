using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public interface ISubjectRepository : IRepository<Subject>
    {
        Task<Subject?> GetByIdWithTeachersAsync(Guid id);
    }
   
}
