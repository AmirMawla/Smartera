using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.AuthenticationDto;

namespace Smartera.BLL.Interfaces
{
    public interface IAuthRepository
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
        Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default);
    }
}
