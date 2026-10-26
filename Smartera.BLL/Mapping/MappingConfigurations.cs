using Mapster;
using Smartera.BLL.Dtos.CartDto;
using Smartera.BLL.Dtos.CategoryDto;
using Smartera.BLL.Dtos.ProductDto;
using Smartera.BLL.Dtos.ReviewDto;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Mapping
{
    public class MappingConfigurations : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Category, CategoryResponse>()
                .Map(dest => dest.ProductCount, src => src.Products.Count);

            config.NewConfig<Product, ProductResponse>()
                .Map(dest => dest.CategoryNameEn, src => src.Category.NameEn)
                .Map(dest => dest.CategoryNameAr, src => src.Category.NameAr)
                .Map(dest => dest.ReviewsCount, src => src.Reviews.Count)
                .Map(dest => dest.Rating, src => src.Reviews.Any() ? Math.Round(src.Reviews.Average(r => (double)r.Rating), 1) : 0.0);

            config.NewConfig<Review, ReviewResponse>()
                .Map(dest => dest.ProductNameEn, src => src.Product.NameEn)
                .Map(dest => dest.ProductNameAr, src => src.Product.NameAr)
                .Map(dest => dest.UserName, src => src.User.FullName)
                .Map(dest => dest.UserEmail, src => src.User.Email);

            config.NewConfig<CartProduct, CartItemResponse>()
                .Map(dest => dest.NameEn, src => src.Product.NameEn)
                .Map(dest => dest.NameAr, src => src.Product.NameAr)
                .Map(dest => dest.MainImageUrl, src => src.Product.MainImageUrl)
                .Map(dest => dest.UnitPrice, src => src.Product.Price)
                .Map(dest => dest.SubTotal, src => src.Product.Price * src.Quantity);
        }
    }
}
