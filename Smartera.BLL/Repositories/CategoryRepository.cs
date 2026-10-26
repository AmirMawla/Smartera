using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.CategoryDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
    {
        private readonly IWebHostEnvironment _env;

        public CategoryRepository(AppDbContext context, IWebHostEnvironment env) : base(context)
        {
            _env = env;
        }

        public async Task<Result<IEnumerable<CategoryResponse>>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _context.Categories
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.NameEn,
                    c.NameAr,
                    c.DescriptionEn,
                    c.DescriptionAr,
                    c.ImageUrl,
                    c.Products.Count
                ))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<CategoryResponse>>(categories);
        }

        public async Task<Result<CategoryResponse>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id)
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.NameEn,
                    c.NameAr,
                    c.DescriptionEn,
                    c.DescriptionAr,
                    c.ImageUrl,
                    c.Products.Count
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (category == null)
                return Result.Failure<CategoryResponse>(CategoryErrors.CategoryNotFound);

            return Result.Success(category);
        }

        public async Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var exists = await _context.Categories.AnyAsync(
                c => c.NameEn.ToLower() == request.NameEn.ToLower() || c.NameAr == request.NameAr,
                cancellationToken);

            if (exists)
                return Result.Failure<CategoryResponse>(CategoryErrors.DuplicatedCategory);

            string? imagePath = null;
            if (request.Image != null && request.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "categories");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.Image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await request.Image.CopyToAsync(stream, cancellationToken);
                imagePath = $"/images/categories/{uniqueFileName}";
            }

            var category = new Category
            {
                NameEn = request.NameEn,
                NameAr = request.NameAr,
                DescriptionEn = request.DescriptionEn,
                DescriptionAr = request.DescriptionAr,
                ImageUrl = imagePath
            };

            await _context.Categories.AddAsync(category, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new CategoryResponse(
                category.Id,
                category.NameEn,
                category.NameAr,
                category.DescriptionEn,
                category.DescriptionAr,
                category.ImageUrl,
                0
            );

            return Result.Success(response);
        }

        public async Task<Result<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _context.Categories.FindAsync([id], cancellationToken);
            if (category == null)
                return Result.Failure<CategoryResponse>(CategoryErrors.CategoryNotFound);

            var duplicate = await _context.Categories.AnyAsync(
                c => c.Id != id && (c.NameEn.ToLower() == request.NameEn.ToLower() || c.NameAr == request.NameAr),
                cancellationToken);

            if (duplicate)
                return Result.Failure<CategoryResponse>(CategoryErrors.DuplicatedCategory);

            if (request.Image != null && request.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "categories");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.Image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await request.Image.CopyToAsync(stream, cancellationToken);
                category.ImageUrl = $"/images/categories/{uniqueFileName}";
            }

            category.NameEn = request.NameEn;
            category.NameAr = request.NameAr;
            category.DescriptionEn = request.DescriptionEn;
            category.DescriptionAr = request.DescriptionAr;

            await _context.SaveChangesAsync(cancellationToken);

            var productCount = await _context.Products.CountAsync(p => p.CategoryId == id, cancellationToken);

            var response = new CategoryResponse(
                category.Id,
                category.NameEn,
                category.NameAr,
                category.DescriptionEn,
                category.DescriptionAr,
                category.ImageUrl,
                productCount
            );

            return Result.Success(response);
        }

        public async Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (category == null)
                return Result.Failure(CategoryErrors.CategoryNotFound);

            if (category.Products.Any())
                return Result.Failure(CategoryErrors.CannotDeleteCategoryWithProducts);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
