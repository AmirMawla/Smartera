using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.OrderDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(o => o.Status)
                .IsRequired()
                .HasConversion(
                    s => s.ToString(),
                    s => (OrderStatus)Enum.Parse(typeof(OrderStatus), s)
                )
                .HasMaxLength(50);

            builder.Property(o => o.ShippingAddress)
                .HasMaxLength(500);

            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
