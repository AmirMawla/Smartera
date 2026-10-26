using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.ReviewDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class ReviewRepository : BaseRepository<Review>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Result<PaginatedList<ReviewResponse>>> GetProductReviewsAsync(
            int productId,
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId, cancellationToken);
            if (!productExists)
                return Result.Failure<PaginatedList<ReviewResponse>>(ProductErrors.ProductNotFound);

            var query = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking();

            var count = await query.CountAsync(cancellationToken);
            var reviews = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mapped = reviews.Select(r => new ReviewResponse(
                r.Id,
                r.ProductId,
                r.Product?.NameEn ?? string.Empty,
                r.Product?.NameAr ?? string.Empty,
                r.User?.FullName ?? "Anonymous",
                r.User?.Email ?? string.Empty,
                r.Rating,
                r.Comment,
                r.CreatedAt
            )).ToList();

            var paginated = new PaginatedList<ReviewResponse>(mapped, count, request.PageNumber, request.PageSize);
            return Result.Success(paginated);
        }

        public async Task<Result<ReviewResponse>> AddReviewAsync(
            int productId,
            string userId,
            ReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var product = await _context.Products.FindAsync([productId], cancellationToken);
            if (product == null)
                return Result.Failure<ReviewResponse>(ProductErrors.ProductNotFound);

            var hasPurchased = await _context.OrderProducts.AnyAsync(
                op => op.ProductId == productId &&
                      op.Order.ApplicationUserId == userId &&
                      op.Order.Status != OrderStatus.Cancelled,
                cancellationToken);

            if (!hasPurchased)
                return Result.Failure<ReviewResponse>(ReviewErrors.HasNotOrdered);

            var alreadyReviewed = await _context.Reviews.AnyAsync(
                r => r.ProductId == productId && r.ApplicationUserId == userId,
                cancellationToken);

            if (alreadyReviewed)
                return Result.Failure<ReviewResponse>(ReviewErrors.DuplicatedReview);

            var review = new Review
            {
                ProductId = productId,
                ApplicationUserId = userId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Reviews.AddAsync(review, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var user = await _context.Users.FindAsync([userId], cancellationToken);

            var response = new ReviewResponse(
                review.Id,
                productId,
                product.NameEn,
                product.NameAr,
                user?.FullName ?? "Customer",
                user?.Email ?? string.Empty,
                review.Rating,
                review.Comment,
                review.CreatedAt
            );

            return Result.Success(response);
        }

        public async Task<Result> DeleteReviewAsync(
            int reviewId,
            string currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var review = await _context.Reviews.FindAsync([reviewId], cancellationToken);
            if (review == null)
                return Result.Failure(ReviewErrors.ReviewNotFound);

            if (!isAdmin && review.ApplicationUserId != currentUserId)
                return Result.Failure(UserErrors.UnauthorizedAccess);

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
