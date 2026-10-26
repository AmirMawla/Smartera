using Microsoft.EntityFrameworkCore;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;
using Xunit;

namespace Smartera.Tests
{
    public class OrderAndCartTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CategoryProductRelationship_DirectOneToMany_Succeeds()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();

            var category = new Category
            {
                NameEn = "Smartphones",
                NameAr = "الهواتف الذكية",
                DescriptionEn = "Latest smartphones and mobile devices",
                DescriptionAr = "أحدث الهواتف الذكية والأجهزة المحمولة",
                ImageUrl = "/images/categories/smartphones.png"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product1 = new Product
            {
                NameEn = "iPhone 16 Pro Max",
                NameAr = "آيفون 16 برو ماكس",
                Price = 65000m,
                StockQuantity = 10,
                CategoryId = category.Id
            };

            var product2 = new Product
            {
                NameEn = "Samsung Galaxy S24 Ultra",
                NameAr = "سامسونج جالاكسي اس 24 الترا",
                Price = 58000m,
                StockQuantity = 15,
                CategoryId = category.Id
            };

            context.Products.AddRange(product1, product2);
            await context.SaveChangesAsync();

            // Act
            var categoryWithProducts = await context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == category.Id);

            // Assert
            Assert.NotNull(categoryWithProducts);
            Assert.Equal(2, categoryWithProducts.Products.Count);
            Assert.Contains(categoryWithProducts.Products, p => p.NameEn == "iPhone 16 Pro Max");
            Assert.Contains(categoryWithProducts.Products, p => p.NameEn == "Samsung Galaxy S24 Ultra");
        }

        [Fact]
        public async Task ProductMultiImages_SupportsMultipleImagesWithPrimary()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();

            var category = new Category
            {
                NameEn = "Laptops",
                NameAr = "أجهزة اللابتوب"
            };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product
            {
                NameEn = "MacBook Pro M3 Max",
                NameAr = "ماك بوك برو إم 3 ماكس",
                Price = 120000m,
                StockQuantity = 5,
                CategoryId = category.Id
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var images = new List<ProductImage>
            {
                new ProductImage { ProductId = product.Id, ImageUrl = "/images/products/macbook-front.png", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ProductId = product.Id, ImageUrl = "/images/products/macbook-side.png", IsPrimary = false, DisplayOrder = 2 },
                new ProductImage { ProductId = product.Id, ImageUrl = "/images/products/macbook-keyboard.png", IsPrimary = false, DisplayOrder = 3 }
            };

            context.ProductImages.AddRange(images);
            await context.SaveChangesAsync();

            // Act
            var productWithImages = await context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            // Assert
            Assert.NotNull(productWithImages);
            Assert.Equal(3, productWithImages.Images.Count);
            Assert.Equal(1, productWithImages.Images.Count(i => i.IsPrimary));
            Assert.Equal("/images/products/macbook-front.png", productWithImages.MainImageUrl);
        }

        [Fact]
        public async Task OrderCreation_CalculatesTotalFromTrustedServerPrices()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();

            var category = new Category { NameEn = "Audio", NameAr = "الصوتيات" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var headphone = new Product
            {
                NameEn = "Sony WH-1000XM5",
                NameAr = "سوني WH-1000XM5",
                Price = 16000m, // Server-side trusted price
                StockQuantity = 20,
                CategoryId = category.Id
            };
            context.Products.Add(headphone);
            await context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "customer@smartera.com",
                Email = "customer@smartera.com",
                FullName = "Customer Test"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act - simulate order creation with server price lookup
            var orderedQuantity = 2;
            var serverProduct = await context.Products.FindAsync(headphone.Id);
            Assert.NotNull(serverProduct);

            var calculatedTotal = serverProduct.Price * orderedQuantity;

            var order = new Order
            {
                ApplicationUserId = user.Id,
                TotalAmount = calculatedTotal,
                Status = OrderStatus.Pending,
                ShippingAddress = "123 Smart St, Cairo"
            };

            order.OrderProducts.Add(new OrderProduct
            {
                ProductId = serverProduct.Id,
                Quantity = orderedQuantity,
                UnitPrice = serverProduct.Price // Server trusted price, NOT client submitted
            });

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Assert
            var savedOrder = await context.Orders
                .Include(o => o.OrderProducts)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            Assert.NotNull(savedOrder);
            Assert.Equal(32000m, savedOrder.TotalAmount);
            Assert.Single(savedOrder.OrderProducts);
            Assert.Equal(16000m, savedOrder.OrderProducts.First().UnitPrice);
        }

        [Fact]
        public async Task Cart_ComputesTotalAmountCorrectly()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();

            var category = new Category { NameEn = "Wearables", NameAr = "الأجهزة القابلة للارتداء" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var watch1 = new Product { NameEn = "Apple Watch Ultra 2", NameAr = "آبل ووتش ألترا 2", Price = 38000m, CategoryId = category.Id };
            var watch2 = new Product { NameEn = "Galaxy Watch 7", NameAr = "جالاكسي ووتش 7", Price = 12000m, CategoryId = category.Id };
            context.Products.AddRange(watch1, watch2);
            await context.SaveChangesAsync();

            var user = new ApplicationUser { Id = "user-cart-test", UserName = "cart@smartera.com", FullName = "Cart User" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var cart = new Cart { ApplicationUserId = user.Id };
            cart.CartProducts.Add(new CartProduct { ProductId = watch1.Id, Quantity = 1 });
            cart.CartProducts.Add(new CartProduct { ProductId = watch2.Id, Quantity = 2 });
            context.Carts.Add(cart);
            await context.SaveChangesAsync();

            // Act
            var retrievedCart = await context.Carts
                .Include(c => c.CartProducts)
                .ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

            Assert.NotNull(retrievedCart);
            var total = retrievedCart.CartProducts.Sum(cp => cp.Product.Price * cp.Quantity);

            // Assert
            // 38000 * 1 + 12000 * 2 = 38000 + 24000 = 62000
            Assert.Equal(62000m, total);
        }

        [Fact]
        public void Localization_Entities_SupportBilingualFields()
        {
            // Verify Category bilingual fields
            var category = new Category
            {
                NameEn = "Smart Home",
                NameAr = "المنزل الذكي",
                DescriptionEn = "Smart lighting and security systems",
                DescriptionAr = "أنظمة الإضاءة والأمان الذكية"
            };

            Assert.Equal("Smart Home", category.NameEn);
            Assert.Equal("المنزل الذكي", category.NameAr);
            Assert.Equal("Smart lighting and security systems", category.DescriptionEn);
            Assert.Equal("أنظمة الإضاءة والأمان الذكية", category.DescriptionAr);

            // Verify Product bilingual fields
            var product = new Product
            {
                NameEn = "Smart Speaker Echo",
                NameAr = "مكبر صوت ذكي إيكو",
                DescriptionEn = "Voice-controlled smart assistant",
                DescriptionAr = "مساعد ذكي يتم التحكم فيه بالصوت"
            };

            Assert.Equal("Smart Speaker Echo", product.NameEn);
            Assert.Equal("مكبر صوت ذكي إيكو", product.NameAr);
            Assert.Equal("Voice-controlled smart assistant", product.DescriptionEn);
            Assert.Equal("مساعد ذكي يتم التحكم فيه بالصوت", product.DescriptionAr);
        }
    }
}
