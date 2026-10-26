using System.ComponentModel.DataAnnotations;

namespace Smartera.BLL.Authentication
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 43200)]
        public int ExpireyMinutes { get; set; } = 60;
    }
}
