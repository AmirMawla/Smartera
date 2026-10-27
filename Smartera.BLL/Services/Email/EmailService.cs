using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Smartera.BLL.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            // If mail settings are not configured (e.g. local development), log safely and return
            if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.Mail))
            {
                _logger.LogWarning("Email settings are not configured. Email to {ToEmail} with subject '{Subject}' was not sent via SMTP.", toEmail, subject);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.Mail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                var secureOption = _settings.Port == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(_settings.Host, _settings.Port, secureOption, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_settings.Password))
                {
                    await smtp.AuthenticateAsync(_settings.Mail, _settings.Password, cancellationToken);
                }

                await smtp.SendAsync(message, cancellationToken);
                await smtp.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email successfully sent to {ToEmail} with subject '{Subject}'.", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'.", toEmail, subject);
                // Do not throw to avoid crashing auth flows if SMTP provider temporarily fails
            }
        }

        public async Task SendVerificationOtpEmailAsync(string toEmail, string userName, string otpCode, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[DEV-OTP] Verification code for {Email} is: {Otp} (Valid for {Minutes} mins)", toEmail, otpCode, expiry.TotalMinutes);
            Console.WriteLine($"\n======================================================\n[SMARTERA OTP] Email Verification Code for {toEmail}: >>> {otpCode} <<<\n======================================================\n");

            var subject = "Verify your SMARTERA Account";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; }}
        .card {{ max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 12px; padding: 32px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }}
        .brand {{ text-align: center; color: #4f46e5; font-size: 26px; font-weight: bold; letter-spacing: 1px; }}
        .title {{ text-align: center; font-size: 20px; color: #1e293b; margin-top: 16px; }}
        .content {{ color: #475569; font-size: 15px; line-height: 1.6; margin-top: 16px; text-align: center; }}
        .otp-badge {{ display: inline-block; background: #e0e7ff; color: #3730a3; font-size: 32px; font-weight: 800; letter-spacing: 8px; padding: 14px 28px; border-radius: 10px; margin: 24px auto; text-align: center; border: 1px dashed #6366f1; }}
        .expiry {{ font-size: 13px; color: #dc2626; margin-top: 8px; }}
        .footer {{ font-size: 12px; color: #94a3b8; text-align: center; margin-top: 32px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='brand'>SMARTERA</div>
        <div class='title'>Confirm Your Email Address</div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <p>Thank you for joining SMARTERA Electronics. Please use the verification code below to confirm your account:</p>
            <div style='text-align: center;'>
                <div class='otp-badge'>{otpCode}</div>
            </div>
            <p class='expiry'>⏱ This code will expire in {(int)expiry.TotalMinutes} minutes.</p>
            <p>If you did not sign up for SMARTERA, you can safely ignore this email.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} SMARTERA Electronics & Smart Devices. All rights reserved.
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }

        public async Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otpCode, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[DEV-OTP] Password reset code for {Email} is: {Otp} (Valid for {Minutes} mins)", toEmail, otpCode, expiry.TotalMinutes);
            Console.WriteLine($"\n======================================================\n[SMARTERA OTP] Password Reset Code for {toEmail}: >>> {otpCode} <<<\n======================================================\n");

            var subject = "Reset your SMARTERA Password";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; }}
        .card {{ max-width: 520px; margin: 0 auto; background: #ffffff; border-radius: 12px; padding: 32px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }}
        .brand {{ text-align: center; color: #e11d48; font-size: 26px; font-weight: bold; letter-spacing: 1px; }}
        .title {{ text-align: center; font-size: 20px; color: #1e293b; margin-top: 16px; }}
        .content {{ color: #475569; font-size: 15px; line-height: 1.6; margin-top: 16px; text-align: center; }}
        .otp-badge {{ display: inline-block; background: #ffe4e6; color: #9f1239; font-size: 32px; font-weight: 800; letter-spacing: 8px; padding: 14px 28px; border-radius: 10px; margin: 24px auto; text-align: center; border: 1px dashed #f43f5e; }}
        .expiry {{ font-size: 13px; color: #dc2626; margin-top: 8px; }}
        .footer {{ font-size: 12px; color: #94a3b8; text-align: center; margin-top: 32px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='brand'>SMARTERA</div>
        <div class='title'>Password Reset Request</div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <p>We received a request to reset your SMARTERA password. Enter the one-time verification code below to proceed:</p>
            <div style='text-align: center;'>
                <div class='otp-badge'>{otpCode}</div>
            </div>
            <p class='expiry'>⏱ This code will expire in {(int)expiry.TotalMinutes} minutes.</p>
            <p>If you did not request a password reset, please secure your account immediately.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} SMARTERA Electronics & Smart Devices. All rights reserved.
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
    }
}
