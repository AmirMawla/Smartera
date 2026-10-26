using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(pi => pi.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(pi => pi.IsPrimary)
                .HasDefaultValue(false);

            builder.HasIndex(pi => new { pi.ProductId, pi.IsPrimary });
        }
    }
}
