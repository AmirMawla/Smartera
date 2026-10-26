using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.CartDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class CartRepository : BaseRepository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Result<CartResponse>> GetCartAsync(string userId, CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(userId, cancellationToken);

            var items = cart.CartProducts.Select(cp => new CartItemResponse(
                cp.ProductId,
                cp.Product.NameEn,
                cp.Product.NameAr,
                cp.Product.MainImageUrl,
                cp.Product.Price,
                cp.Quantity,
                cp.Product.Price * cp.Quantity
            )).ToList();

            var totalAmount = items.Sum(i => i.SubTotal);
            var totalItems = items.Sum(i => i.Quantity);

            return Result.Success(new CartResponse(cart.Id, totalAmount, totalItems, items));
        }

        public async Task<Result<CartResponse>> AddToCartAsync(
            string userId,
            AddToCartRequest request,
            CancellationToken cancellationToken = default)
        {
            var product = await _context.Products.FindAsync([request.ProductId], cancellationToken);
            if (product == null)
                return Result.Failure<CartResponse>(ProductErrors.ProductNotFound);

            var cart = await GetOrCreateCartAsync(userId, cancellationToken);
            var cartProduct = cart.CartProducts.FirstOrDefault(cp => cp.ProductId == request.ProductId);

            if (cartProduct == null)
            {
                cartProduct = new CartProduct
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                };
                await _context.CartProducts.AddAsync(cartProduct, cancellationToken);
            }
            else
            {
                cartProduct.Quantity += request.Quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return await GetCartAsync(userId, cancellationToken);
        }

        public async Task<Result<CartResponse>> UpdateCartItemAsync(
            string userId,
            UpdateCartItemRequest request,
            CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(userId, cancellationToken);
            var cartProduct = cart.CartProducts.FirstOrDefault(cp => cp.ProductId == request.ProductId);

            if (cartProduct == null)
                return Result.Failure<CartResponse>(CartErrors.ItemNotFoundInCart);

            if (request.Quantity <= 0)
            {
                _context.CartProducts.Remove(cartProduct);
            }
            else
            {
                cartProduct.Quantity = request.Quantity;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return await GetCartAsync(userId, cancellationToken);
        }

        public async Task<Result<CartResponse>> RemoveFromCartAsync(
            string userId,
            int productId,
            CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(userId, cancellationToken);
            var cartProduct = cart.CartProducts.FirstOrDefault(cp => cp.ProductId == productId);

            if (cartProduct != null)
            {
                _context.CartProducts.Remove(cartProduct);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return await GetCartAsync(userId, cancellationToken);
        }

        public async Task<Result> ClearCartAsync(string userId, CancellationToken cancellationToken = default)
        {
            var cart = await _context.Carts
                .Include(c => c.CartProducts)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId, cancellationToken);

            if (cart != null && cart.CartProducts.Any())
            {
                _context.CartProducts.RemoveRange(cart.CartProducts);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }

        private async Task<Cart> GetOrCreateCartAsync(string userId, CancellationToken cancellationToken)
        {
            var cart = await _context.Carts
                .Include(c => c.CartProducts)
                .ThenInclude(cp => cp.Product)
                .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId, cancellationToken);

            if (cart == null)
            {
                cart = new Cart
                {
                    ApplicationUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Carts.AddAsync(cart, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return cart;
        }
    }
}
