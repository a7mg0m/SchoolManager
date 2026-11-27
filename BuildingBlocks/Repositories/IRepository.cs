using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace BuildingBlocks
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        void Remove(T entity);
        Task<List<T>> GetAllAsync();

        /// <summary>All rows, not tracked: for lists that are only read (much faster on big tables)</summary>
        Task<List<T>> GetAllReadOnlyAsync();
        Task<List<T>> SearchAsync(Expression<Func<T, bool>> predicate);

    }
}
