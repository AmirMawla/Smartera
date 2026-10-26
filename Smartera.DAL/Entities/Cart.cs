namespace Smartera.DAL.Entities
{
    public class Cart
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;

        public virtual ICollection<CartProduct> CartProducts { get; set; } = new List<CartProduct>();
    }
}
