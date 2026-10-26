using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.Address)
                .HasMaxLength(250);

            builder.Property(u => u.BirthDate)
                .HasColumnType("date");

            builder.OwnsMany(x => x.RefreshTokens, owned =>
            {
                owned.ToTable("RefreshTokens");
                owned.WithOwner().HasForeignKey("UserId");
                owned.Property(rt => rt.Token).IsRequired().HasMaxLength(256);
            });

            builder.HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Reviews)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
