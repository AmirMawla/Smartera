using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.OrderDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Result<OrderResponse>> CreateOrderAsync(
            string userId,
            CreateOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            var cart = await _context.Carts
                .Include(c => c.CartProducts)
                .ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId, cancellationToken);

            if (cart == null || !cart.CartProducts.Any())
                return Result.Failure<OrderResponse>(CartErrors.CartNotFoundOrEmpty);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Calculate trusted server-side total amount
                decimal totalAmount = 0;
                var orderProducts = new List<OrderProduct>();

                foreach (var cp in cart.CartProducts)
                {
                    var product = cp.Product;
                    if (product.StockQuantity < cp.Quantity)
                    {
                        return Result.Failure<OrderResponse>(
                            new Error("Product.InsufficientStock", $"Insufficient stock for product '{product.NameEn}'. Available: {product.StockQuantity}.", 400));
                    }

                    // Deduct stock
                    product.StockQuantity -= cp.Quantity;

                    var itemTotal = product.Price * cp.Quantity;
                    totalAmount += itemTotal;

                    orderProducts.Add(new OrderProduct
                    {
                        ProductId = product.Id,
                        Quantity = cp.Quantity,
                        UnitPrice = product.Price
                    });
                }

                var order = new Order
                {
                    ApplicationUserId = userId,
                    TotalAmount = totalAmount,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    ShippingAddress = request.ShippingAddress
                };

                await _context.Orders.AddAsync(order, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var op in orderProducts)
                {
                    op.OrderId = order.Id;
                }
                await _context.OrderProducts.AddRangeAsync(orderProducts, cancellationToken);

                // Create initial pending payment record
                var merchantOrderId = $"SMARTERA-ORD-{order.Id}-{DateTime.UtcNow.Ticks}";
                var payment = new Payment
                {
                    OrderId = order.Id,
                    TotalAmount = totalAmount,
                    Currency = "EGP",
                    Status = PaymentStatus.Pending,
                    PaymentMethod = request.PaymentMethod,
                    MerchantOrderId = merchantOrderId,
                    TransactionDate = DateTime.UtcNow
                };

                await _context.Payments.AddAsync(payment, cancellationToken);

                // Clear user's cart
                _context.CartProducts.RemoveRange(cart.CartProducts);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await GetOrderByIdAsync(order.Id, userId, true, cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<OrderResponse>(OrderErrors.OrderCreationFailed);
            }
        }

        public async Task<Result<OrderResponse>> GetOrderByIdAsync(
            int orderId,
            string currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .ThenInclude(p => p.Images)
                .Where(o => o.Id == orderId);

            if (!isAdmin)
            {
                query = query.Where(o => o.ApplicationUserId == currentUserId);
            }

            var order = await query.FirstOrDefaultAsync(cancellationToken);
            if (order == null)
                return Result.Failure<OrderResponse>(OrderErrors.OrderNotFound);

            var response = MapToOrderResponse(order);
            return Result.Success(response);
        }

        public async Task<Result<PaginatedList<OrderResponse>>> GetUserOrdersAsync(
            string userId,
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .ThenInclude(p => p.Images)
                .Where(o => o.ApplicationUserId == userId)
                .AsNoTracking();

            if (request.Statuses != null && request.Statuses.Length > 0)
            {
                var validStatuses = request.Statuses
                    .Where(s => Enum.TryParse<OrderStatus>(s, true, out _))
                    .Select(s => Enum.Parse<OrderStatus>(s, true))
                    .ToList();

                if (validStatuses.Any())
                    query = query.Where(o => validStatuses.Contains(o.Status));
            }

            query = query.OrderByDescending(o => o.OrderDate);

            var count = await query.CountAsync(cancellationToken);
            var orders = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mappedItems = orders.Select(MapToOrderResponse).ToList();
            var paginated = new PaginatedList<OrderResponse>(mappedItems, count, request.PageNumber, request.PageSize);

            return Result.Success(paginated);
        }

        public async Task<Result<PaginatedList<OrderResponse>>> GetAllOrdersAsync(
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .ThenInclude(p => p.Images)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var term = $"%{request.SearchValue.Trim().ToLower()}%";
                query = query.Where(o =>
                    EF.Functions.Like(o.User.FullName.ToLower(), term) ||
                    EF.Functions.Like(o.User.Email!.ToLower(), term) ||
                    EF.Functions.Like(o.Id.ToString(), term));
            }

            if (request.Statuses != null && request.Statuses.Length > 0)
            {
                var validStatuses = request.Statuses
                    .Where(s => Enum.TryParse<OrderStatus>(s, true, out _))
                    .Select(s => Enum.Parse<OrderStatus>(s, true))
                    .ToList();

                if (validStatuses.Any())
                    query = query.Where(o => validStatuses.Contains(o.Status));
            }

            if (request.DateFilter.HasValue)
            {
                var date = request.DateFilter.Value.ToDateTime(TimeOnly.MinValue);
                var nextDate = date.AddDays(1);
                query = query.Where(o => o.OrderDate >= date && o.OrderDate < nextDate);
            }

            query = !string.IsNullOrWhiteSpace(request.SortColumn)
                ? query.OrderBy($"{request.SortColumn} {request.SortDirection ?? "desc"}")
                : query.OrderByDescending(o => o.OrderDate);

            var count = await query.CountAsync(cancellationToken);
            var orders = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mappedItems = orders.Select(MapToOrderResponse).ToList();
            var paginated = new PaginatedList<OrderResponse>(mappedItems, count, request.PageNumber, request.PageSize);

            return Result.Success(paginated);
        }

        public async Task<Result> UpdateOrderStatusAsync(
            int orderId,
            UpdateOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders.FindAsync([orderId], cancellationToken);
            if (order == null)
                return Result.Failure(OrderErrors.OrderNotFound);

            if (Enum.TryParse<OrderStatus>(request.NewStatus, true, out var newStatus))
            {
                order.Status = newStatus;
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            return Result.Failure(new Error("Order.InvalidStatus", $"Status '{request.NewStatus}' is invalid.", 400));
        }

        public async Task<Result> CancelOrderAsync(
            int orderId,
            string currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
                return Result.Failure(OrderErrors.OrderNotFound);

            if (!isAdmin && order.ApplicationUserId != currentUserId)
                return Result.Failure(OrderErrors.AccessDenied);

            if (order.Status == OrderStatus.Delivered)
                return Result.Failure(new Error("Order.Delivered", "Delivered orders cannot be cancelled.", 400));

            if (order.Status == OrderStatus.Cancelled)
                return Result.Failure(OrderErrors.AlreadyCancelled);

            order.Status = OrderStatus.Cancelled;

            // Restore product stock
            foreach (var op in order.OrderProducts)
            {
                op.Product.StockQuantity += op.Quantity;
            }

            if (order.Payment != null)
            {
                order.Payment.Status = order.Payment.Status == PaymentStatus.Completed
                    ? PaymentStatus.Refunded
                    : PaymentStatus.Cancelled;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        private static OrderResponse MapToOrderResponse(Order order)
        {
            var items = order.OrderProducts.Select(op => new OrderItemResponse(
                op.ProductId,
                op.Product?.NameEn ?? string.Empty,
                op.Product?.NameAr ?? string.Empty,
                op.Product?.MainImageUrl,
                op.UnitPrice,
                op.Quantity,
                op.UnitPrice * op.Quantity
            )).ToList();

            OrderPaymentSummary? paymentSummary = null;
            if (order.Payment != null)
            {
                paymentSummary = new OrderPaymentSummary(
                    order.Payment.Id,
                    order.Payment.TotalAmount,
                    order.Payment.Currency,
                    order.Payment.Status.ToString(),
                    order.Payment.PaymentMethod,
                    order.Payment.MerchantOrderId,
                    order.Payment.TransactionDate
                );
            }

            return new OrderResponse(
                order.Id,
                order.ApplicationUserId,
                order.User?.FullName ?? "Unknown",
                order.User?.Email ?? "Unknown",
                order.TotalAmount,
                order.Status.ToString(),
                order.ShippingAddress,
                order.OrderDate,
                paymentSummary,
                items
            );
        }
    }
}
