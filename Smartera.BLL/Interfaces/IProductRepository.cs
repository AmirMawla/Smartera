using Microsoft.AspNetCore.Http;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.ProductDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface IProductRepository : IBaseRepository<Product>
    {
        Task<Result<PaginatedList<ProductResponse>>> GetAllProductsAsync(RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result<PaginatedList<ProductResponse>>> GetProductsByCategoryAsync(int categoryId, RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result<ProductResponse>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
        Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<ProductResponse>> AddProductImagesAsync(int productId, List<IFormFile> images, CancellationToken cancellationToken = default);
        Task<Result> RemoveProductImageAsync(int productId, int imageId, CancellationToken cancellationToken = default);
        Task<Result> SetPrimaryImageAsync(int productId, int imageId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<ProductResponse>>> GetFeaturedProductsAsync(int count = 8, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<ProductResponse>>> GetNewArrivalsAsync(int count = 8, CancellationToken cancellationToken = default);
    }
}
