namespace Smartera.DAL.Entities
{
    public class CartProduct
    {
        public int CartId { get; set; }
        public virtual Cart Cart { get; set; } = default!;

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = default!;

        public int Quantity { get; set; }
    }
}
