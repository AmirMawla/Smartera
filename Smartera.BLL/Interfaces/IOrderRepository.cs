using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.OrderDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface IOrderRepository : IBaseRepository<Order>
    {
        Task<Result<OrderResponse>> CreateOrderAsync(string userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
        Task<Result<OrderResponse>> GetOrderByIdAsync(int orderId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<Result<PaginatedList<OrderResponse>>> GetUserOrdersAsync(string userId, RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result<PaginatedList<OrderResponse>>> GetAllOrdersAsync(RequestFilter request, CancellationToken cancellationToken = default);
        Task<Result> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
        Task<Result> CancelOrderAsync(int orderId, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
