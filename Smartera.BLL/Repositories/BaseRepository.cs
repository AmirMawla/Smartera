using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;

namespace Smartera.BLL.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<Result> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> RemoveAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity is null)
                return Result.Failure(new Error("NotFound", $"Entity with id {id} was not found", StatusCodes.Status404NotFound));

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<IEnumerable<T>>> GetAllAsync()
        {
            var entities = await _dbSet.AsNoTracking().ToListAsync();
            return entities is null || !entities.Any()
                ? Result.Failure<IEnumerable<T>>(new Error("NotFound", "No entities found", StatusCodes.Status404NotFound))
                : Result.Success<IEnumerable<T>>(entities);
        }

        public async Task<Result<T>> GetByIdAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            return entity is null
                ? Result.Failure<T>(new Error("NotFound", "Entity was not found", StatusCodes.Status404NotFound))
                : Result.Success(entity);
        }

        public async Task<Result<T>> FindByExpress(Expression<Func<T, bool>> criteria)
        {
            var entity = await _dbSet.SingleOrDefaultAsync(criteria);
            return entity is null
                ? Result.Failure<T>(new Error("NotFound", "Entity was not found", StatusCodes.Status404NotFound))
                : Result.Success(entity);
        }

        public async Task<Result<IEnumerable<T>>> FindAllByExpress(Expression<Func<T, bool>> criteria)
        {
            var entities = await _dbSet.Where(criteria).ToListAsync();
            return entities is null || !entities.Any()
                ? Result.Failure<IEnumerable<T>>(new Error("NotFound", "No entities found", StatusCodes.Status404NotFound))
                : Result.Success<IEnumerable<T>>(entities);
        }

        public async Task<Result<IEnumerable<T>>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            var result = await query.ToListAsync();
            return Result.Success<IEnumerable<T>>(result);
        }

        public async Task<Result<T>> GetFirstOrDefaultWithIncludeAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            var result = await query.FirstOrDefaultAsync(filter);
            return result is null
                ? Result.Failure<T>(new Error("NotFound", "Entity was not found", StatusCodes.Status404NotFound))
                : Result.Success(result);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter)
        {
            return await _dbSet.AnyAsync(filter);
        }
    }
}
