namespace Smartera.DAL.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
