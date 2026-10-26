using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smartera.DAL.Entities;

namespace Smartera.DAL.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Seed Roles
            string[] roles = ["Admin", "User"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@smartera.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Smartera Admin",
                    EmailConfirmed = true,
                    PhoneNumber = "+201000000001",
                    Address = "Cairo, Egypt",
                    RegistrationDate = DateTime.UtcNow
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, "Admin@Smartera2026!");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Regular Customer User
            var demoUserEmail = "customer@smartera.com";
            var demoUser = await userManager.FindByEmailAsync(demoUserEmail);
            if (demoUser == null)
            {
                demoUser = new ApplicationUser
                {
                    UserName = demoUserEmail,
                    Email = demoUserEmail,
                    FullName = "Amir Customer",
                    EmailConfirmed = true,
                    PhoneNumber = "+201000000002",
                    Address = "Giza, Egypt",
                    RegistrationDate = DateTime.UtcNow
                };

                var createDemoResult = await userManager.CreateAsync(demoUser, "User@Smartera2026!");
                if (createDemoResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(demoUser, "User");
                }
            }

            // Seed Categories and Products
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category
                    {
                        NameEn = "Smartphones & Tablets",
                        NameAr = "الهواتف الذكية والأجهزة اللوحية",
                        DescriptionEn = "Latest flagship smartphones, foldable devices, tablets and smart accessories.",
                        DescriptionAr = "أحدث الهواتف الذكية الرائدة والأجهزة القابلة للطي والأجهزة اللوحية.",
                        ImageUrl = "/images/categories/smartphones.jpg"
                    },
                    new Category
                    {
                        NameEn = "Laptops & Computers",
                        NameAr = "الحواسيب المحمولة وأجهزة الكمبيوتر",
                        DescriptionEn = "High performance work laptops, gaming notebooks, and desktop computers.",
                        DescriptionAr = "حواسيب محمولة عالية الأداء للعمل والألعاب وأجهزة سطح المكتب.",
                        ImageUrl = "/images/categories/laptops.jpg"
                    },
                    new Category
                    {
                        NameEn = "Audio & Headphones",
                        NameAr = "الصوتيات وسماعات الرأس",
                        DescriptionEn = "Wireless earbuds, noise-cancelling headphones, soundbars, and smart speakers.",
                        DescriptionAr = "سماعات أذن لاسلكية، سماعات مانعة للضوضاء، ومكبرات صوت ذكية.",
                        ImageUrl = "/images/categories/audio.jpg"
                    },
                    new Category
                    {
                        NameEn = "Smartwatches & Wearables",
                        NameAr = "الساعات الذكية والأجهزة القابلة للارتداء",
                        DescriptionEn = "Fitness trackers, cellular smartwatches, and smart health monitors.",
                        DescriptionAr = "أجهزة تتبع اللياقة البدنية، الساعات الذكية ومراقبة الصحة.",
                        ImageUrl = "/images/categories/smartwatches.jpg"
                    },
                    new Category
                    {
                        NameEn = "Smart Home & IoT",
                        NameAr = "المنزل الذكي وإنترنت الأشياء",
                        DescriptionEn = "Smart security cameras, smart lighting, automated vacuum cleaners, and hubs.",
                        DescriptionAr = "كاميرات المراقبة الذكية، الإضاءة الذكية، والمكانس الروبوتية.",
                        ImageUrl = "/images/categories/smarthome.jpg"
                    },
                    new Category
                    {
                        NameEn = "Gaming & Consoles",
                        NameAr = "أجهزة الألعاب وإكسسواراتها",
                        DescriptionEn = "Next-generation gaming consoles, controllers, VR headsets, and gaming gear.",
                        DescriptionAr = "منصات الألعاب من الجيل الجديد، أذرع التحكم، ونظارات الواقع الافتراضي.",
                        ImageUrl = "/images/categories/gaming.jpg"
                    }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();

                // Products
                var catPhones = categories[0];
                var catLaptops = categories[1];
                var catAudio = categories[2];
                var catWatches = categories[3];
                var catHome = categories[4];
                var catGaming = categories[5];

                var products = new List<Product>
                {
                    // Phones
                    new Product
                    {
                        CategoryId = catPhones.Id,
                        NameEn = "Samsung Galaxy S24 Ultra 512GB",
                        NameAr = "سامسونج جالاكسي S24 ألترا 512 جيجابايت",
                        DescriptionEn = "Titanium frame, 200MP camera with Galaxy AI features, and integrated S Pen.",
                        DescriptionAr = "هيكل من التيتانيوم، كاميرا بدقة 200 ميجابكسل مع مزايا الذكاء الاصطناعي وقلم S Pen مدمج.",
                        Price = 58999.00m,
                        StockQuantity = 45,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/s24u_front.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/s24u_back.jpg", IsPrimary = false, DisplayOrder = 2 },
                            new ProductImage { ImageUrl = "/images/products/s24u_pen.jpg", IsPrimary = false, DisplayOrder = 3 }
                        }
                    },
                    new Product
                    {
                        CategoryId = catPhones.Id,
                        NameEn = "Apple iPhone 16 Pro Max 256GB",
                        NameAr = "آبل آيفون 16 برو ماكس 256 جيجابايت",
                        DescriptionEn = "Grade 5 Titanium design with Camera Control button and A18 Pro Bionic processor.",
                        DescriptionAr = "تصميم تيتانيوم من الدرجة الخامسة مع زر التحكم في الكاميرا ومعالج A18 Pro الخارق.",
                        Price = 67999.00m,
                        StockQuantity = 30,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/iphone16pm_desert.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/iphone16pm_side.jpg", IsPrimary = false, DisplayOrder = 2 },
                            new ProductImage { ImageUrl = "/images/products/iphone16pm_camera.jpg", IsPrimary = false, DisplayOrder = 3 }
                        }
                    },

                    // Laptops
                    new Product
                    {
                        CategoryId = catLaptops.Id,
                        NameEn = "MacBook Pro 16\" M3 Max 36GB / 1TB",
                        NameAr = "ماك بوك برو 16 بوصة M3 Max سعة 1 تيرابايت",
                        DescriptionEn = "Liquid Retina XDR display with up to 22 hours battery life and extreme computing performance.",
                        DescriptionAr = "شاشة Liquid Retina XDR وبطارية تدوم حتى 22 ساعة وأداء حوسبي فائق للمحترفين.",
                        Price = 139999.00m,
                        StockQuantity = 15,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/macbook_front.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/macbook_open.jpg", IsPrimary = false, DisplayOrder = 2 },
                            new ProductImage { ImageUrl = "/images/products/macbook_ports.jpg", IsPrimary = false, DisplayOrder = 3 }
                        }
                    },
                    new Product
                    {
                        CategoryId = catLaptops.Id,
                        NameEn = "Dell XPS 15 OLED Core i9 32GB / 1TB RTX 4070",
                        NameAr = "ديل XPS 15 شاشة أوليد معالج كور i9",
                        DescriptionEn = "Stunning 3.5K OLED touch screen, sleek CNC machined aluminum, and dedicated NVIDIA RTX graphics.",
                        DescriptionAr = "شاشة OLED لمس بدقة 3.5K وهيكل ألومنيوم أنيق مع معالج رسوميات NVIDIA RTX متطور.",
                        Price = 98500.00m,
                        StockQuantity = 20,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/dell_xps_main.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/dell_xps_keyboard.jpg", IsPrimary = false, DisplayOrder = 2 }
                        }
                    },

                    // Audio
                    new Product
                    {
                        CategoryId = catAudio.Id,
                        NameEn = "Sony WH-1000XM5 Wireless Noise-Cancelling Headphones",
                        NameAr = "سماعات سوني WH-1000XM5 اللاسلكية المانعة للضوضاء",
                        DescriptionEn = "Industry-leading active noise cancellation with 8 microphones and 30-hour battery life.",
                        DescriptionAr = "عزل ضوضاء رائد في الصناعة مع 8 ميكروفونات وعمر بطارية يصل إلى 30 ساعة.",
                        Price = 18499.00m,
                        StockQuantity = 50,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/sony_xm5_black.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/sony_xm5_case.jpg", IsPrimary = false, DisplayOrder = 2 }
                        }
                    },
                    new Product
                    {
                        CategoryId = catAudio.Id,
                        NameEn = "Apple AirPods Pro 2 with USB-C",
                        NameAr = "آبل إيربودز برو 2 بمنفذ تايب سي",
                        DescriptionEn = "H2 chip with Adaptive Audio, Active Noise Cancellation, and MagSafe charging case.",
                        DescriptionAr = "شريحة H2 مع الصوت التكيفي وعزل الضوضاء النشط وعلبة شحن MagSafe.",
                        Price = 12999.00m,
                        StockQuantity = 75,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/airpodspro2_main.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/airpodspro2_case.jpg", IsPrimary = false, DisplayOrder = 2 }
                        }
                    },

                    // Watches
                    new Product
                    {
                        CategoryId = catWatches.Id,
                        NameEn = "Apple Watch Ultra 2 Titanium 49mm",
                        NameAr = "ساعة آبل الترا 2 تيتانيوم 49 مم",
                        DescriptionEn = "Rugged titanium case, precision dual-frequency GPS, 3000 nits brightness, and water resistance to 100m.",
                        DescriptionAr = "هيكل تيتانيوم متين، نظام GPS مزدوج عالي الدقة، وسطوع 3000 شمعة ومقاومة للماء حتى 100 متر.",
                        Price = 44999.00m,
                        StockQuantity = 25,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/apple_watch_ultra2.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/apple_watch_band.jpg", IsPrimary = false, DisplayOrder = 2 }
                        }
                    },

                    // Smart Home
                    new Product
                    {
                        CategoryId = catHome.Id,
                        NameEn = "Roborock S8 Pro Ultra Robot Vacuum and Mop",
                        NameAr = "مكنسة روبوروك S8 برو الترا الروبوتية",
                        DescriptionEn = "All-in-one docking system with self-washing, self-drying, self-emptying and dual roller brushes.",
                        DescriptionAr = "قاعدة تنظيف وغسيل وتفريغ ذاتي بالكامل مع فرشاة مزدوجة وقوة شفط استثنائية.",
                        Price = 52000.00m,
                        StockQuantity = 12,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/roborock_dock.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/roborock_robot.jpg", IsPrimary = false, DisplayOrder = 2 }
                        }
                    },

                    // Gaming
                    new Product
                    {
                        CategoryId = catGaming.Id,
                        NameEn = "Sony PlayStation 5 Slim Digital 1TB",
                        NameAr = "سوني بلايستيشن 5 سليم إصدار رقمي 1 تيرابايت",
                        DescriptionEn = "Sleek compact design, ultra-high speed SSD, ray tracing, and 3D audio immersion.",
                        DescriptionAr = "تصميم مدمج وأنيق، قرص SSD فائق السرعة، تتبع الأشعة وصوت ثلاثي الأبعاد غامر.",
                        Price = 28999.00m,
                        StockQuantity = 35,
                        Images = new List<ProductImage>
                        {
                            new ProductImage { ImageUrl = "/images/products/ps5_slim_console.jpg", IsPrimary = true, DisplayOrder = 1 },
                            new ProductImage { ImageUrl = "/images/products/ps5_controller.jpg", IsPrimary = false, DisplayOrder = 2 }
                        }
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
