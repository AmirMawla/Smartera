namespace Smartera.BLL.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendVerificationOtpEmailAsync(string toEmail, string userName, string otpCode, TimeSpan expiry, CancellationToken cancellationToken = default);
        Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otpCode, TimeSpan expiry, CancellationToken cancellationToken = default);
    }
}
