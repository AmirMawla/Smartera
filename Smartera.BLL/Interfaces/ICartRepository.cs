using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.CartDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface ICartRepository : IBaseRepository<Cart>
    {
        Task<Result<CartResponse>> GetCartAsync(string userId, CancellationToken cancellationToken = default);
        Task<Result<CartResponse>> AddToCartAsync(string userId, AddToCartRequest request, CancellationToken cancellationToken = default);
        Task<Result<CartResponse>> UpdateCartItemAsync(string userId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
        Task<Result<CartResponse>> RemoveFromCartAsync(string userId, int productId, CancellationToken cancellationToken = default);
        Task<Result> ClearCartAsync(string userId, CancellationToken cancellationToken = default);
    }
}
