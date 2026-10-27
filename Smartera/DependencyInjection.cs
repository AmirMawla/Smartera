using System.Globalization;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Smartera.BLL.Authentication;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.BLL.Repositories;
using Smartera.BLL.Services.Payment;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;
using Smartera.PL.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartera.PL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddControllers();
            services.AddHttpContextAccessor();

            // CORS configuration
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);
                });
            });

            // Swagger / OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            // Mapster
            var mappingConfig = TypeAdapterConfig.GlobalSettings;
            mappingConfig.Scan(typeof(UnitOfWork).Assembly);
            services.AddSingleton<IMapper>(new Mapper(mappingConfig));

            // FluentValidation
            services.AddFluentValidationAutoValidation()
                .AddValidatorsFromAssembly(typeof(UnitOfWork).Assembly);

            // Database Context
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\mssqllocaldb;Database=SmarteraDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseLazyLoadingProxies()
                       .UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            });

            // Identity & Auth
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.AddSingleton<IJwtProvider, JwtProvider>();

            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions
            {
                Key = "SuperSecretSecureKeyForSmarteraEcommerceApp2026!Min32BytesRequired",
                Issuer = "SmarteraApi",
                Audience = "SmarteraApp",
                ExpireyMinutes = 120
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
                options.AddPolicy("AdminOrUser", policy => policy.RequireRole("Admin", "User"));
            });

            // Repositories & Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();

            // Paymob Service & Options
            services.Configure<PaymobOptions>(configuration.GetSection(PaymobOptions.SectionName));
            services.AddHttpClient<IPaymobService, PaymobService>();

            // Email Service & MailSettings
            services.Configure<Smartera.BLL.Services.Email.MailSettings>(configuration.GetSection("MailSettings"));
            services.AddScoped<Smartera.BLL.Services.Email.IEmailService, Smartera.BLL.Services.Email.EmailService>();

            // Global Exception Handler
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            // Localization
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("en"),
                    new CultureInfo("ar-EG"),
                    new CultureInfo("ar")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.ApplyCurrentCultureToResponseHeaders = true;
            });

            return services;
        }
    }
}
