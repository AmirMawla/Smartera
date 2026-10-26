using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.CategoryDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface ICategoryRepository : IBaseRepository<Category>
    {
        Task<Result<IEnumerable<CategoryResponse>>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
        Task<Result<CategoryResponse>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<Result<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    }
}
