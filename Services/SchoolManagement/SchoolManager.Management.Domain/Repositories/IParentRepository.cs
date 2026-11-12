using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Management.Domain
{
    public interface IParentRepository : IRepository<Parent>
    {
        Task<Parent?> GetByIdWithStudentsAsync(Guid id);
        Task<List<Parent>> GetAllWithStudentsAsync();
    }
   
}
