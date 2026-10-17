using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
