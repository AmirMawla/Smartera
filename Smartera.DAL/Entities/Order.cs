namespace Smartera.DAL.Entities
{
    public enum OrderStatus
    {
        Pending = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }

    public class Order
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? ShippingAddress { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;

        public virtual ApplicationUser User { get; set; } = default!;
        public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
        public virtual Payment? Payment { get; set; }
    }
}
