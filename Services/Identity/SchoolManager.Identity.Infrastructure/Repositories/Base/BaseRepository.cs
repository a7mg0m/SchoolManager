using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SchoolManager.Identity.Infrastructure
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IdentityDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(IdentityDbContext context)
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
            // If the entity was loaded through this context (e.g. via GetById), it is already
            // tracked and any changes (including new child entities in owned collections) are
            // picked up automatically. Calling Update() on an already-tracked entity would
            // re-mark the whole graph as Modified, which incorrectly turns newly added child
            // entities (that already have a non-default key) into Modified instead of Added,
            // causing EF to issue an UPDATE for a row that doesn't exist yet.
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
