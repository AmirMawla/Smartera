namespace Smartera.DAL.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = default!;

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

        public string? MainImageUrl =>
            Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary)?.ImageUrl 
            ?? Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl;
    }
}
