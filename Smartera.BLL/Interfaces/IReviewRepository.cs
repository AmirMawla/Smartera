using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.ReviewDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface IReviewRepository : IBaseRepository<Review>
    {
        Task<Result<PaginatedList<ReviewResponse>>> GetProductReviewsAsync(int productId, RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result<ReviewResponse>> AddReviewAsync(int productId, string userId, ReviewRequest request, CancellationToken cancellationToken = default);
        Task<Result> DeleteReviewAsync(int reviewId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
