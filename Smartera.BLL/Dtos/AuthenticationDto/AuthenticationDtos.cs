using FluentValidation;

namespace Smartera.BLL.Dtos.AuthenticationDto
{
    public record RegisterRequest(
        string FullName,
        string Email,
        string Password,
        string? PhoneNumber,
        string? Address
    );

    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(200);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
                .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(25).When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.Address)
                .MaximumLength(250).When(x => !string.IsNullOrEmpty(x.Address));
        }
    }

    public record LoginRequest(string Email, string Password);

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    public record RefreshTokenRequest(string Token, string RefreshToken);

    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required.");
        }
    }

    public record AuthResponse(
        string Id,
        string Email,
        string FullName,
        IEnumerable<string> Roles,
        string Token,
        int ExpiresIn,
        string RefreshToken,
        DateTime RefreshTokenExpiration
    );

    public record UserProfileResponse(
        string Id,
        string Email,
        string FullName,
        string? PhoneNumber,
        string? Address,
        string? ImageUrl,
        IEnumerable<string> Roles,
        DateTime RegistrationDate
    );

    public record VerifyEmailRequest(string Email, string Otp);

    public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
    {
        public VerifyEmailRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Otp).NotEmpty().Length(6).WithMessage("OTP must be 6 digits.");
        }
    }

    public record ResendOtpRequest(string Email);

    public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
    {
        public ResendOtpRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public record ForgotPasswordRequest(string Email);

    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
    {
        public ForgotPasswordRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public record VerifyOtpRequest(string Email, string Otp);

    public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
    {
        public VerifyOtpRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Otp).NotEmpty().Length(6).WithMessage("OTP must be 6 digits.");
        }
    }

    public record VerifyOtpResponse(string ResetToken);

    public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.ResetToken).NotEmpty().WithMessage("Reset token is required.");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
                .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Password must contain at least one special character.");
        }
    }

    public record MessageResponse(string Message);
}
