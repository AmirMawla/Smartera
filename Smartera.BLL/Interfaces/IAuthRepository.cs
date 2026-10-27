using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.AuthenticationDto;

namespace Smartera.BLL.Interfaces
{
    public interface IAuthRepository
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<Result<MessageResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
        Task<Result<MessageResponse>> ResendVerificationOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default);
        Task<Result<MessageResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
        Task<Result<VerifyOtpResponse>> VerifyPasswordResetOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
        Task<Result<MessageResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default);
    }
}
