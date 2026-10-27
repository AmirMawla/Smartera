using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartProduct> CartProducts => Set<CartProduct>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderProduct> OrderProducts => Set<OrderProduct>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<OtpEntry> OtpEntries => Set<OtpEntry>();
    }
}
