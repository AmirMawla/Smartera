using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.PaymentDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.BLL.Services.Payment;
using Smartera.DAL.Entities;

namespace Smartera.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymobService _paymobService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(
            IPaymobService paymobService,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            UserManager<ApplicationUser> userManager)
        {
            _paymobService = paymobService;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _userManager = userManager;
        }

        [Authorize]
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment(
            [FromBody] InitiatePaymentRequest request,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");

            var orderResult = await _orderRepository.GetOrderByIdAsync(request.OrderId, userId, isAdmin, cancellationToken);
            if (!orderResult.IsSuccess)
                return orderResult.ToProblem();

            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (!order.IsSuccess)
                return order.ToProblem();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized();

            var result = await _paymobService.InitiatePaymentAsync(order.Value, user, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(
            [FromBody] PaymobWebhookPayload payload,
            [FromQuery(Name = "hmac")] string? queryHmac,
            CancellationToken cancellationToken)
        {
            var hmac = queryHmac ?? Request.Headers["hmac"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(hmac))
            {
                return BadRequest(new { error = "Missing HMAC signature in callback." });
            }

            var result = await _paymobService.ProcessWebhookAsync(payload, hmac, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("return")]
        public async Task<IActionResult> PaymentReturn(
            [FromQuery(Name = "success")] string? successStr,
            [FromQuery(Name = "id")] string? transactionId,
            [FromQuery(Name = "order")] string? orderId,
            CancellationToken cancellationToken)
        {
            // Note: Per security requirements, never mark order as paid purely based on frontend redirect!
            // Order fulfillment and verification is handled strictly via HMAC-verified webhook.
            var isSuccess = bool.TryParse(successStr, out var s) && s;

            return Ok(new
            {
                Status = isSuccess ? "PaymentSubmitted" : "PaymentFailed",
                TransactionId = transactionId,
                OrderId = orderId,
                Message = isSuccess
                    ? "Your payment transaction was submitted and is being verified. Check your order history for confirmed status."
                    : "The payment attempt was declined or cancelled."
            });
        }

        [Authorize]
        [HttpGet("status/{orderId:int}")]
        public async Task<IActionResult> GetPaymentStatus(int orderId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");

            var result = await _paymentRepository.GetPaymentByOrderIdAsync(orderId, userId, isAdmin, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var result = await _paymentRepository.GetAllTransactionsAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize]
        [HttpGet("my-transactions")]
        public async Task<IActionResult> GetMyTransactions([FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _paymentRepository.GetUserTransactionsAsync(userId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
