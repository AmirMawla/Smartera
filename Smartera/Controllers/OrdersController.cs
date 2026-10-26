using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.OrderDto;
using Smartera.BLL.Interfaces;

namespace Smartera.PL.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;

        public OrdersController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderRepository.CreateOrderAsync(userId, request, cancellationToken);
            return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : result.ToProblem();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var result = await _orderRepository.GetOrderByIdAsync(id, userId, isAdmin, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderRepository.GetUserOrdersAsync(userId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var result = await _orderRepository.GetAllOrdersAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _orderRepository.UpdateOrderStatusAsync(id, request, cancellationToken);
            return result.IsSuccess ? Ok(new { message = "Order status updated successfully." }) : result.ToProblem();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");
            var result = await _orderRepository.CancelOrderAsync(id, userId, isAdmin, cancellationToken);
            return result.IsSuccess ? Ok(new { message = "Order cancelled successfully." }) : result.ToProblem();
        }
    }
}
