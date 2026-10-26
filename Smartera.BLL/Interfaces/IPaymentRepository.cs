using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.PaymentDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface IPaymentRepository : IBaseRepository<Payment>
    {
        Task<Result<PaginatedList<TransactionResponse>>> GetAllTransactionsAsync(RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result<PaginatedList<TransactionResponse>>> GetUserTransactionsAsync(string userId, RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result<PaymentStatusResponse>> GetPaymentByOrderIdAsync(int orderId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
