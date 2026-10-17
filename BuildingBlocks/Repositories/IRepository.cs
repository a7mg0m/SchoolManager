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
        Task<List<T>> SearchAsync(Expression<Func<T, bool>> predicate);

    }
}
