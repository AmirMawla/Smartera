using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.PaymentDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Result<PaginatedList<TransactionResponse>>> GetAllTransactionsAsync(
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.User)
                .AsNoTracking();

            if (request.Statuses != null && request.Statuses.Length > 0)
            {
                var validStatuses = request.Statuses
                    .Where(s => Enum.TryParse<PaymentStatus>(s, true, out _))
                    .Select(s => Enum.Parse<PaymentStatus>(s, true))
                    .ToList();

                if (validStatuses.Any())
                    query = query.Where(p => validStatuses.Contains(p.Status));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var term = $"%{request.SearchValue.Trim().ToLower()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.MerchantOrderId.ToLower(), term) ||
                    EF.Functions.Like(p.OrderId.ToString(), term) ||
                    EF.Functions.Like(p.Order.User.FullName.ToLower(), term) ||
                    EF.Functions.Like(p.Order.User.Email!.ToLower(), term));
            }

            if (request.DateFilter.HasValue)
            {
                var date = request.DateFilter.Value.ToDateTime(TimeOnly.MinValue);
                var nextDate = date.AddDays(1);
                query = query.Where(p => p.TransactionDate >= date && p.TransactionDate < nextDate);
            }

            query = !string.IsNullOrWhiteSpace(request.SortColumn)
                ? query.OrderBy($"{request.SortColumn} {request.SortDirection ?? "desc"}")
                : query.OrderByDescending(p => p.TransactionDate);

            var count = await query.CountAsync(cancellationToken);
            var payments = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mapped = payments.Select(p => new TransactionResponse(
                p.Id,
                p.OrderId,
                p.Order.User?.FullName ?? "Unknown",
                p.TotalAmount,
                p.Currency,
                p.Status.ToString(),
                p.PaymentMethod,
                p.MerchantOrderId,
                p.TransactionDate
            )).ToList();

            var paginated = new PaginatedList<TransactionResponse>(mapped, count, request.PageNumber, request.PageSize);
            return Result.Success(paginated);
        }

        public async Task<Result<PaginatedList<TransactionResponse>>> GetUserTransactionsAsync(
            string userId,
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o.User)
                .Where(p => p.Order.ApplicationUserId == userId)
                .OrderByDescending(p => p.TransactionDate)
                .AsNoTracking();

            var count = await query.CountAsync(cancellationToken);
            var payments = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mapped = payments.Select(p => new TransactionResponse(
                p.Id,
                p.OrderId,
                p.Order.User?.FullName ?? "Unknown",
                p.TotalAmount,
                p.Currency,
                p.Status.ToString(),
                p.PaymentMethod,
                p.MerchantOrderId,
                p.TransactionDate
            )).ToList();

            var paginated = new PaginatedList<TransactionResponse>(mapped, count, request.PageNumber, request.PageSize);
            return Result.Success(paginated);
        }

        public async Task<Result<PaymentStatusResponse>> GetPaymentByOrderIdAsync(
            int orderId,
            string currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);

            if (payment == null)
                return Result.Failure<PaymentStatusResponse>(PaymentErrors.PaymentNotFound);

            if (!isAdmin && payment.Order.ApplicationUserId != currentUserId)
                return Result.Failure<PaymentStatusResponse>(UserErrors.UnauthorizedAccess);

            var response = new PaymentStatusResponse(
                payment.Id,
                payment.OrderId,
                payment.TotalAmount,
                payment.Currency,
                payment.Status.ToString(),
                payment.PaymentMethod,
                payment.MerchantOrderId,
                payment.PaymobTransactionId,
                payment.PaymentConfirmedAt
            );

            return Result.Success(response);
        }
    }
}
