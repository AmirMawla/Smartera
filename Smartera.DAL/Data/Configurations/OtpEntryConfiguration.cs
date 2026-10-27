using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class OtpEntryConfiguration : IEntityTypeConfiguration<OtpEntry>
    {
        public void Configure(EntityTypeBuilder<OtpEntry> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(o => o.HashedOtp)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(o => o.Purpose)
                .IsRequired();

            builder.Property(o => o.Expiry)
                .IsRequired();

            builder.Property(o => o.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(o => new { o.Email, o.Purpose });
            builder.HasIndex(o => o.Expiry);
        }
    }
}
