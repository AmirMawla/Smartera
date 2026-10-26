using Microsoft.AspNetCore.Http;
using Smartera.BLL.Abstractions;

namespace Smartera.BLL.Errors
{
    public static class UserErrors
    {
        public static readonly Error InvalidCredentials =
            new("User.InvalidCredentials", "Invalid email or password", StatusCodes.Status401Unauthorized);

        public static readonly Error UserNotFound =
            new("User.NotFound", "User was not found", StatusCodes.Status404NotFound);

        public static readonly Error DuplicatedEmail =
            new("User.DuplicatedEmail", "A user with this email address already exists", StatusCodes.Status409Conflict);

        public static readonly Error EmailNotConfirmed =
            new("User.EmailNotConfirmed", "Email has not been confirmed", StatusCodes.Status400BadRequest);

        public static readonly Error InvalidRefreshToken =
            new("User.InvalidRefreshToken", "The refresh token is invalid or expired", StatusCodes.Status401Unauthorized);

        public static readonly Error UnauthorizedAccess =
            new("User.UnauthorizedAccess", "You are not authorized to perform this action", StatusCodes.Status403Forbidden);
    }

    public static class CategoryErrors
    {
        public static readonly Error CategoryNotFound =
            new("Category.NotFound", "Category was not found", StatusCodes.Status404NotFound);

        public static readonly Error CategoriesNotFound =
            new("Category.NotFound", "No categories found", StatusCodes.Status404NotFound);

        public static readonly Error DuplicatedCategory =
            new("Category.DuplicatedName", "A category with this name already exists", StatusCodes.Status409Conflict);

        public static readonly Error CannotDeleteCategoryWithProducts =
            new("Category.HasProducts", "Cannot delete category that contains products", StatusCodes.Status400BadRequest);
    }

    public static class ProductErrors
    {
        public static readonly Error ProductNotFound =
            new("Product.NotFound", "Product was not found", StatusCodes.Status404NotFound);

        public static readonly Error ProductsNotFound =
            new("Product.NotFound", "No products match the requested criteria", StatusCodes.Status404NotFound);

        public static readonly Error InsufficientStock =
            new("Product.InsufficientStock", "Insufficient stock available for the requested product", StatusCodes.Status400BadRequest);

        public static readonly Error ImageNotFound =
            new("Product.ImageNotFound", "Product image was not found", StatusCodes.Status404NotFound);
    }

    public static class CartErrors
    {
        public static readonly Error CartNotFound =
            new("Cart.NotFound", "Shopping cart was not found", StatusCodes.Status404NotFound);

        public static readonly Error CartNotFoundOrEmpty =
            new("Cart.Empty", "Your shopping cart is empty", StatusCodes.Status400BadRequest);

        public static readonly Error ItemNotFoundInCart =
            new("Cart.ItemNotFound", "The specified product is not in your cart", StatusCodes.Status404NotFound);
    }

    public static class OrderErrors
    {
        public static readonly Error OrderNotFound =
            new("Order.NotFound", "Order was not found", StatusCodes.Status404NotFound);

        public static readonly Error NoOrdersFound =
            new("Order.NotFound", "No orders found", StatusCodes.Status404NotFound);

        public static readonly Error OrderCreationFailed =
            new("Order.CreationFailed", "Failed to create order. Please try again.", StatusCodes.Status500InternalServerError);

        public static readonly Error AccessDenied =
            new("Order.AccessDenied", "You do not have permission to access or modify this order", StatusCodes.Status403Forbidden);

        public static readonly Error CannotCancel =
            new("Order.CannotCancel", "Cannot cancel an order in its current status", StatusCodes.Status400BadRequest);

        public static readonly Error AlreadyCancelled =
            new("Order.AlreadyCancelled", "This order is already cancelled", StatusCodes.Status400BadRequest);
    }

    public static class PaymentErrors
    {
        public static readonly Error PaymentNotFound =
            new("Payment.NotFound", "Payment record was not found", StatusCodes.Status404NotFound);

        public static readonly Error PaymentInitiationFailed =
            new("Payment.InitiationFailed", "Failed to initiate payment gateway transaction", StatusCodes.Status502BadGateway);

        public static readonly Error InvalidHmacSignature =
            new("Payment.InvalidSignature", "Payment callback HMAC verification failed", StatusCodes.Status400BadRequest);

        public static readonly Error OrderAlreadyPaid =
            new("Payment.AlreadyPaid", "This order has already been paid and verified", StatusCodes.Status400BadRequest);

        public static readonly Error AmountMismatch =
            new("Payment.AmountMismatch", "Verified payment amount does not match expected order total", StatusCodes.Status400BadRequest);

        public static readonly Error PaymentFailed =
            new("Payment.Failed", "Payment transaction failed or was declined", StatusCodes.Status402PaymentRequired);
    }

    public static class ReviewErrors
    {
        public static readonly Error ReviewNotFound =
            new("Review.NotFound", "Review was not found", StatusCodes.Status404NotFound);

        public static readonly Error ReviewsNotFound =
            new("Review.NotFound", "No reviews found for this product", StatusCodes.Status404NotFound);

        public static readonly Error DuplicatedReview =
            new("Review.DuplicatedReview", "You have already submitted a review for this product", StatusCodes.Status409Conflict);

        public static readonly Error HasNotOrdered =
            new("Review.HasNotOrdered", "You can only review products you have purchased", StatusCodes.Status403Forbidden);
    }
}
