using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.CartDto;
using Smartera.BLL.Interfaces;

namespace Smartera.PL.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepository;

        public CartController(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartRepository.GetCartAsync(userId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartRepository.AddToCartAsync(userId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPut("items")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartRepository.UpdateCartItemAsync(userId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpDelete("items/{productId:int}")]
        public async Task<IActionResult> RemoveItem(int productId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartRepository.RemoveFromCartAsync(userId, productId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartRepository.ClearCartAsync(userId, cancellationToken);
            return result.IsSuccess ? Ok(new { message = "Cart cleared successfully." }) : result.ToProblem();
        }
    }
}
