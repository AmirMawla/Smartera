using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;
using Smartera.PL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SMARTERA API V1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at application root
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// Request Localization (en/ar)
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/swagger", () => Results.Redirect("/"));

// Initialize and seed database safely on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.EnsureCreated();
        await SeedData.InitializeAsync(context, userManager, roleManager);
        logger.LogInformation("SMARTERA database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database on startup.");
    }
}

app.Run();
