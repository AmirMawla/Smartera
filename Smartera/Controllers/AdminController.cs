using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.PL.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IApplicationUserRepository _userRepository;

        public AdminController(AppDbContext context, IApplicationUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
        }

        public record AdminDashboardStats(
            int TotalUsers,
            int TotalCategories,
            int TotalProducts,
            int TotalOrders,
            int PendingOrders,
            int ProcessingOrders,
            int DeliveredOrders,
            decimal TotalRevenue,
            decimal MonthlyRevenue
        );

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
        {
            var totalUsers = await _context.Users.CountAsync(cancellationToken);
            var totalCategories = await _context.Categories.CountAsync(cancellationToken);
            var totalProducts = await _context.Products.CountAsync(cancellationToken);
            var totalOrders = await _context.Orders.CountAsync(cancellationToken);

            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing, cancellationToken);
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered, cancellationToken);

            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0m;

            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthlyRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.TransactionDate >= firstDayOfMonth)
                .SumAsync(p => (decimal?)p.TotalAmount, cancellationToken) ?? 0m;

            var stats = new AdminDashboardStats(
                totalUsers,
                totalCategories,
                totalProducts,
                totalOrders,
                pendingOrders,
                processingOrders,
                deliveredOrders,
                totalRevenue,
                monthlyRevenue
            );

            return Ok(stats);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var result = await _userRepository.GetAllUsersAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }
    }
}
