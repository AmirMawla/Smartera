using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Smartera.BLL.Dtos.ProductDto
{
    public record ProductImageResponse(
        int Id,
        string ImageUrl,
        bool IsPrimary,
        int DisplayOrder
    );

    public record ProductResponse(
        int Id,
        string NameEn,
        string NameAr,
        string? DescriptionEn,
        string? DescriptionAr,
        decimal Price,
        int StockQuantity,
        string? MainImageUrl,
        int CategoryId,
        string CategoryNameEn,
        string CategoryNameAr,
        double Rating,
        int ReviewsCount,
        List<ProductImageResponse> Images,
        DateTime CreatedAt
    );

    public record CreateProductRequest(
        string NameEn,
        string NameAr,
        string? DescriptionEn,
        string? DescriptionAr,
        decimal Price,
        int StockQuantity,
        int CategoryId,
        List<IFormFile>? Images
    );

    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English product name is required.")
                .MaximumLength(200);

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic product name is required.")
                .MaximumLength(200);

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("A valid Category is required.");

            RuleFor(x => x.DescriptionEn)
                .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.DescriptionEn));

            RuleFor(x => x.DescriptionAr)
                .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.DescriptionAr));
        }
    }

    public record UpdateProductRequest(
        string NameEn,
        string NameAr,
        string? DescriptionEn,
        string? DescriptionAr,
        decimal Price,
        int StockQuantity,
        int CategoryId
    );

    public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English product name is required.")
                .MaximumLength(200);

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic product name is required.")
                .MaximumLength(200);

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("A valid Category is required.");
        }
    }
}
