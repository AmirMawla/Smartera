using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.PaymentDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Services.Payment
{
    public interface IPaymobService
    {
        Task<Result<PaymobInitiateResponse>> InitiatePaymentAsync(Order order, ApplicationUser user, CancellationToken cancellationToken = default);
        bool ValidateHmac(PaymobTransactionObject obj, string receivedHmac);
        Task<Result<PaymentStatusResponse>> ProcessWebhookAsync(PaymobWebhookPayload payload, string hmac, CancellationToken cancellationToken = default);
        Task<Result<PaymentStatusResponse>> GetPaymentStatusAsync(int paymentId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
