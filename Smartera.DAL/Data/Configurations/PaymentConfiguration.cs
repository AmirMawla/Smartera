using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("EGP");

            builder.Property(p => p.TransactionDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.Status)
                .IsRequired()
                .HasConversion(
                    s => s.ToString(),
                    s => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), s)
                )
                .HasMaxLength(50);

            builder.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.MerchantOrderId)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(p => p.PaymobTransactionId)
                .HasMaxLength(150);

            builder.Property(p => p.PaymobIntentionId)
                .HasMaxLength(250);

            builder.Property(p => p.ProviderResponse)
                .HasMaxLength(2000);

            builder.HasIndex(p => p.MerchantOrderId);
            builder.HasIndex(p => p.PaymobTransactionId);

            builder.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
