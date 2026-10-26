using Microsoft.AspNetCore.Identity;

namespace Smartera.DAL.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateOnly? BirthDate { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual Cart? Cart { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
