using FluentValidation;

namespace Smartera.BLL.Dtos.CartDto
{
    public record CartItemResponse(
        int ProductId,
        string NameEn,
        string NameAr,
        string? MainImageUrl,
        decimal UnitPrice,
        int Quantity,
        decimal SubTotal
    );

    public record CartResponse(
        int Id,
        decimal TotalAmount,
        int TotalItems,
        List<CartItemResponse> Items
    );

    public record AddToCartRequest(int ProductId, int Quantity);

    public class AddToCartRequestValidator : AbstractValidator<AddToCartRequest>
    {
        public AddToCartRequestValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Valid product ID is required.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be at least 1.");
        }
    }

    public record UpdateCartItemRequest(int ProductId, int Quantity);

    public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
    {
        public UpdateCartItemRequestValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Valid product ID is required.");
            RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
        }
    }
}
