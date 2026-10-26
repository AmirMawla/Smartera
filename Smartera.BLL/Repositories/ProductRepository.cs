using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.ProductDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        private readonly IWebHostEnvironment _env;

        public ProductRepository(AppDbContext context, IWebHostEnvironment env) : base(context)
        {
            _env = env;
        }

        public async Task<Result<PaginatedList<ProductResponse>>> GetAllProductsAsync(
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .AsNoTracking();

            if (request.CategoryId.HasValue && request.CategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var term = $"%{request.SearchValue.Trim().ToLower()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.NameEn.ToLower(), term) ||
                    EF.Functions.Like(p.NameAr.ToLower(), term) ||
                    EF.Functions.Like(p.DescriptionEn != null ? p.DescriptionEn.ToLower() : "", term) ||
                    EF.Functions.Like(p.DescriptionAr != null ? p.DescriptionAr.ToLower() : "", term) ||
                    EF.Functions.Like(p.Category.NameEn.ToLower(), term) ||
                    EF.Functions.Like(p.Category.NameAr.ToLower(), term));
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (request.MinRating.HasValue)
            {
                query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= request.MinRating.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SortColumn))
            {
                var direction = !string.IsNullOrWhiteSpace(request.SortDirection) ? request.SortDirection : "asc";
                query = query.OrderBy($"{request.SortColumn} {direction}");
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            var source = query.Select(p => new ProductResponse(
                p.Id,
                p.NameEn,
                p.NameAr,
                p.DescriptionEn,
                p.DescriptionAr,
                p.Price,
                p.StockQuantity,
                p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary) != null
                    ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary)!.ImageUrl
                    : (p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null
                        ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl
                        : null),
                p.CategoryId,
                p.Category.NameEn,
                p.Category.NameAr,
                p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => (double)r.Rating), 1) : 0.0,
                p.Reviews.Count,
                p.Images.OrderBy(i => i.DisplayOrder).Select(img => new ProductImageResponse(
                    img.Id,
                    img.ImageUrl,
                    img.IsPrimary,
                    img.DisplayOrder
                )).ToList(),
                p.CreatedAt
            ));

            var paginated = await PaginatedList<ProductResponse>.CreateAsync(
                source,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return Result.Success(paginated);
        }

        public async Task<Result<PaginatedList<ProductResponse>>> GetProductsByCategoryAsync(
            int categoryId,
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            request.CategoryId = categoryId;
            return await GetAllProductsAsync(request, cancellationToken);
        }

        public async Task<Result<ProductResponse>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .Where(p => p.Id == id)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.NameEn,
                    p.NameAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.StockQuantity,
                    p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary) != null
                        ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary)!.ImageUrl
                        : (p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null
                            ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl
                            : null),
                    p.CategoryId,
                    p.Category.NameEn,
                    p.Category.NameAr,
                    p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => (double)r.Rating), 1) : 0.0,
                    p.Reviews.Count,
                    p.Images.OrderBy(i => i.DisplayOrder).Select(img => new ProductImageResponse(
                        img.Id,
                        img.ImageUrl,
                        img.IsPrimary,
                        img.DisplayOrder
                    )).ToList(),
                    p.CreatedAt
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
                return Result.Failure<ProductResponse>(ProductErrors.ProductNotFound);

            return Result.Success(product);
        }

        public async Task<Result<ProductResponse>> CreateProductAsync(
            CreateProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
                return Result.Failure<ProductResponse>(CategoryErrors.CategoryNotFound);

            var product = new Product
            {
                NameEn = request.NameEn,
                NameAr = request.NameAr,
                DescriptionEn = request.DescriptionEn,
                DescriptionAr = request.DescriptionAr,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            if (request.Images != null && request.Images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "products");
                Directory.CreateDirectory(uploadsFolder);

                var order = 1;
                foreach (var imgFile in request.Images)
                {
                    if (imgFile.Length == 0) continue;

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imgFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imgFile.CopyToAsync(stream, cancellationToken);

                    var productImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = $"/images/products/{uniqueFileName}",
                        IsPrimary = order == 1,
                        DisplayOrder = order++
                    };

                    await _context.ProductImages.AddAsync(productImage, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            return await GetProductByIdAsync(product.Id, cancellationToken);
        }

        public async Task<Result> UpdateProductAsync(
            int id,
            UpdateProductRequest request,
            CancellationToken cancellationToken = default)
        {
            var product = await _context.Products.FindAsync([id], cancellationToken);
            if (product == null)
                return Result.Failure(ProductErrors.ProductNotFound);

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
                return Result.Failure(CategoryErrors.CategoryNotFound);

            product.NameEn = request.NameEn;
            product.NameAr = request.NameAr;
            product.DescriptionEn = request.DescriptionEn;
            product.DescriptionAr = request.DescriptionAr;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.CategoryId = request.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product == null)
                return Result.Failure(ProductErrors.ProductNotFound);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<ProductResponse>> AddProductImagesAsync(
            int productId,
            List<IFormFile> images,
            CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null)
                return Result.Failure<ProductResponse>(ProductErrors.ProductNotFound);

            if (images != null && images.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "products");
                Directory.CreateDirectory(uploadsFolder);

                var startOrder = product.Images.Any() ? product.Images.Max(i => i.DisplayOrder) + 1 : 1;
                var hasPrimary = product.Images.Any(i => i.IsPrimary);

                foreach (var imgFile in images)
                {
                    if (imgFile.Length == 0) continue;

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imgFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imgFile.CopyToAsync(stream, cancellationToken);

                    var productImage = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = $"/images/products/{uniqueFileName}",
                        IsPrimary = !hasPrimary && startOrder == 1,
                        DisplayOrder = startOrder++
                    };

                    await _context.ProductImages.AddAsync(productImage, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            return await GetProductByIdAsync(productId, cancellationToken);
        }

        public async Task<Result> RemoveProductImageAsync(
            int productId,
            int imageId,
            CancellationToken cancellationToken = default)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken);

            if (image == null)
                return Result.Failure(ProductErrors.ImageNotFound);

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);

            var remainingImages = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync(cancellationToken);

            if (remainingImages.Any() && !remainingImages.Any(i => i.IsPrimary))
            {
                remainingImages.First().IsPrimary = true;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }

        public async Task<Result> SetPrimaryImageAsync(
            int productId,
            int imageId,
            CancellationToken cancellationToken = default)
        {
            var images = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .ToListAsync(cancellationToken);

            var target = images.FirstOrDefault(i => i.Id == imageId);
            if (target == null)
                return Result.Failure(ProductErrors.ImageNotFound);

            foreach (var img in images)
            {
                img.IsPrimary = img.Id == imageId;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<IEnumerable<ProductResponse>>> GetFeaturedProductsAsync(
            int count = 8,
            CancellationToken cancellationToken = default)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.NameEn,
                    p.NameAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.StockQuantity,
                    p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary) != null
                        ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary)!.ImageUrl
                        : (p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null
                            ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl
                            : null),
                    p.CategoryId,
                    p.Category.NameEn,
                    p.Category.NameAr,
                    p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => (double)r.Rating), 1) : 0.0,
                    p.Reviews.Count,
                    p.Images.OrderBy(i => i.DisplayOrder).Select(img => new ProductImageResponse(
                        img.Id,
                        img.ImageUrl,
                        img.IsPrimary,
                        img.DisplayOrder
                    )).ToList(),
                    p.CreatedAt
                ))
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<ProductResponse>>(products);
        }

        public async Task<Result<IEnumerable<ProductResponse>>> GetNewArrivalsAsync(
            int count = 8,
            CancellationToken cancellationToken = default)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.NameEn,
                    p.NameAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.StockQuantity,
                    p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary) != null
                        ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary)!.ImageUrl
                        : (p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault() != null
                            ? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()!.ImageUrl
                            : null),
                    p.CategoryId,
                    p.Category.NameEn,
                    p.Category.NameAr,
                    p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => (double)r.Rating), 1) : 0.0,
                    p.Reviews.Count,
                    p.Images.OrderBy(i => i.DisplayOrder).Select(img => new ProductImageResponse(
                        img.Id,
                        img.ImageUrl,
                        img.IsPrimary,
                        img.DisplayOrder
                    )).ToList(),
                    p.CreatedAt
                ))
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<ProductResponse>>(products);
        }
    }
}
