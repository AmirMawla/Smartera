using System;

namespace Smartera.DAL.Entities
{
    public enum OtpPurpose
    {
        EmailVerification = 1,
        PasswordReset = 2
    }

    public class OtpEntry
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string HashedOtp { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public DateTime Expiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
