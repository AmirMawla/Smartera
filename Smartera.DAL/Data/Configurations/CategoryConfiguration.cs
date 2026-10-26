using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.NameEn)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.NameAr)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.DescriptionEn)
                .HasMaxLength(500);

            builder.Property(c => c.DescriptionAr)
                .HasMaxLength(500);

            builder.Property(c => c.ImageUrl)
                .HasMaxLength(300);

            builder.HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
