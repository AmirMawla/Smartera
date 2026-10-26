using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.ReviewDto;
using Smartera.BLL.Interfaces;

namespace Smartera.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewsController(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetProductReviews(int productId, [FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var result = await _reviewRepository.GetProductReviewsAsync(productId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize]
        [HttpPost("product/{productId:int}")]
        public async Task<IActionResult> AddReview(int productId, [FromBody] ReviewRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _reviewRepository.AddReviewAsync(productId, userId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReview(int id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var result = await _reviewRepository.DeleteReviewAsync(id, userId, isAdmin, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}
