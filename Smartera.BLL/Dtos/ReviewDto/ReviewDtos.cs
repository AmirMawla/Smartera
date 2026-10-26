using FluentValidation;

namespace Smartera.BLL.Dtos.ReviewDto
{
    public record ReviewRequest(int Rating, string? Comment);

    public class ReviewRequestValidator : AbstractValidator<ReviewRequest>
    {
        public ReviewRequestValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5 stars.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Comment));
        }
    }

    public record ReviewResponse(
        int Id,
        int ProductId,
        string ProductNameEn,
        string ProductNameAr,
        string UserName,
        string UserEmail,
        int Rating,
        string? Comment,
        DateTime CreatedAt
    );
}
