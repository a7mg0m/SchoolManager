using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace SchoolManager.ExamsHomework.Infrastructure
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly ExamsHomeworkDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(ExamsHomeworkDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public Task<T?> GetByIdAsync(Guid id)
            => _dbSet.FindAsync(id).AsTask();

        public Task AddAsync(T entity)
            => _dbSet.AddAsync(entity).AsTask();

        public Task UpdateAsync(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Update(entity);
            }

            return Task.CompletedTask;
        }

        public void Remove(T entity)
            => _dbSet.Remove(entity);

        public virtual async Task<List<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task<List<T>> SearchAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();
    }
}
