namespace Smartera.DAL.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = default!;

        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = default!;

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
