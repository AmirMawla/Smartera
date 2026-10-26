namespace Smartera.DAL.Entities
{
    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4,
        Cancelled = 5
    }

    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = default!;

        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "EGP";
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string PaymentMethod { get; set; } = "Paymob";
        public string MerchantOrderId { get; set; } = string.Empty;
        public string? PaymobTransactionId { get; set; }
        public string? PaymobIntentionId { get; set; }
        public DateTime? PaymentConfirmedAt { get; set; }
        public string? ProviderResponse { get; set; }
    }
}
