using System.Linq.Expressions;
using Smartera.BLL.Abstractions;

namespace Smartera.BLL.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<Result> AddAsync(T entity);
        Task<Result> UpdateAsync(T entity);
        Task<Result> RemoveAsync(int id);
        Task<Result<IEnumerable<T>>> GetAllAsync();
        Task<Result<T>> GetByIdAsync(int id);
        Task<Result<T>> FindByExpress(Expression<Func<T, bool>> criteria);
        Task<Result<IEnumerable<T>>> FindAllByExpress(Expression<Func<T, bool>> criteria);
        Task<Result<IEnumerable<T>>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includes);
        Task<Result<T>> GetFirstOrDefaultWithIncludeAsync(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes);
        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
    }
}
