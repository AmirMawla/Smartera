using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Smartera.BLL.Dtos.CategoryDto
{
    public record CategoryResponse(
        int Id,
        string NameEn,
        string NameAr,
        string? DescriptionEn,
        string? DescriptionAr,
        string? ImageUrl,
        int ProductCount
    );

    public record CreateCategoryRequest(
        string NameEn,
        string NameAr,
        string? DescriptionEn,
        string? DescriptionAr,
        IFormFile? Image
    );

    public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
    {
        public CreateCategoryRequestValidator()
        {
            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English category name is required.")
                .MaximumLength(150);

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic category name is required.")
                .MaximumLength(150);

            RuleFor(x => x.DescriptionEn)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.DescriptionEn));

            RuleFor(x => x.DescriptionAr)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.DescriptionAr));
        }
    }

    public record UpdateCategoryRequest(
        string NameEn,
        string NameAr,
        string? DescriptionEn,
        string? DescriptionAr,
        IFormFile? Image
    );

    public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
    {
        public UpdateCategoryRequestValidator()
        {
            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English category name is required.")
                .MaximumLength(150);

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic category name is required.")
                .MaximumLength(150);

            RuleFor(x => x.DescriptionEn)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.DescriptionEn));

            RuleFor(x => x.DescriptionAr)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.DescriptionAr));
        }
    }
}
