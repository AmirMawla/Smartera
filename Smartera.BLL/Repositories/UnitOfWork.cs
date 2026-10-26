using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public ICategoryRepository Categories { get; }
        public IProductRepository Products { get; }
        public ICartRepository Carts { get; }
        public IOrderRepository Orders { get; }
        public IPaymentRepository Payments { get; }
        public IReviewRepository Reviews { get; }
        public IApplicationUserRepository Users { get; }

        public UnitOfWork(
            AppDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;

            Categories = new CategoryRepository(_context, _env);
            Products = new ProductRepository(_context, _env);
            Carts = new CartRepository(_context);
            Orders = new OrderRepository(_context);
            Payments = new PaymentRepository(_context);
            Reviews = new ReviewRepository(_context);
            Users = new ApplicationUserRepository(_context, _userManager);
        }

        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
