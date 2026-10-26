using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class CartProductConfiguration : IEntityTypeConfiguration<CartProduct>
    {
        public void Configure(EntityTypeBuilder<CartProduct> builder)
        {
            builder.HasKey(cp => new { cp.CartId, cp.ProductId });

            builder.Property(cp => cp.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.HasOne(cp => cp.Cart)
                .WithMany(c => c.CartProducts)
                .HasForeignKey(cp => cp.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cp => cp.Product)
                .WithMany(p => p.CartProducts)
                .HasForeignKey(cp => cp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
