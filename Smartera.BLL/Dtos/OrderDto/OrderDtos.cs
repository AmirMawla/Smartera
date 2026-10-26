using FluentValidation;

namespace Smartera.BLL.Dtos.OrderDto
{
    public record OrderItemResponse(
        int ProductId,
        string NameEn,
        string NameAr,
        string? MainImageUrl,
        decimal UnitPrice,
        int Quantity,
        decimal SubTotal
    );

    public record OrderPaymentSummary(
        int PaymentId,
        decimal TotalAmount,
        string Currency,
        string Status,
        string PaymentMethod,
        string MerchantOrderId,
        DateTime TransactionDate
    );

    public record OrderResponse(
        int Id,
        string UserId,
        string CustomerName,
        string CustomerEmail,
        decimal TotalAmount,
        string Status,
        string? ShippingAddress,
        DateTime OrderDate,
        OrderPaymentSummary? Payment,
        List<OrderItemResponse> Items
    );

    public record CreateOrderRequest(
        string ShippingAddress,
        string PaymentMethod = "Paymob"
    );

    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required.")
                .MaximumLength(500);

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Payment method is required.");
        }
    }

    public record UpdateOrderStatusRequest(string NewStatus);

    public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
    {
        public UpdateOrderStatusRequestValidator()
        {
            RuleFor(x => x.NewStatus)
                .NotEmpty().WithMessage("New status is required.")
                .Must(s => new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" }.Contains(s))
                .WithMessage("Invalid order status. Allowed: Pending, Processing, Shipped, Delivered, Cancelled.");
        }
    }
}
