using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Authentication;
using Smartera.BLL.Dtos.AuthenticationDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtProvider _jwtProvider;
        private readonly AppDbContext _context;
        private const int RefreshTokenExpiryDays = 14;

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            IJwtProvider jwtProvider,
            AppDbContext context)
        {
            _userManager = userManager;
            _jwtProvider = jwtProvider;
            _context = context;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValidPassword)
                return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresIn) = _jwtProvider.GenerateToken(user, roles);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                ExpiresOn = refreshTokenExpiration
            });

            await _userManager.UpdateAsync(user);

            var response = new AuthResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                roles,
                token,
                expiresIn,
                refreshToken,
                refreshTokenExpiration
            );

            return Result.Success(response);
        }

        public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return Result.Failure<AuthResponse>(UserErrors.DuplicatedEmail);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var firstError = createResult.Errors.First();
                    return Result.Failure<AuthResponse>(new Error(firstError.Code, firstError.Description, 400));
                }

                await _userManager.AddToRoleAsync(user, "User");

                // Initialize shopping cart for user
                var cart = new Cart
                {
                    ApplicationUserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Carts.AddAsync(cart, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var roles = new List<string> { "User" };
                var (token, expiresIn) = _jwtProvider.GenerateToken(user, roles);

                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiration = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

                user.RefreshTokens.Add(new RefreshToken
                {
                    Token = refreshToken,
                    ExpiresOn = refreshTokenExpiration
                });

                await _userManager.UpdateAsync(user);
                await transaction.CommitAsync(cancellationToken);

                var response = new AuthResponse(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FullName,
                    roles,
                    token,
                    expiresIn,
                    refreshToken,
                    refreshTokenExpiration
                );

                return Result.Success(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<AuthResponse>(new Error("RegistrationFailed", ex.Message, 500));
            }
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var userId = _jwtProvider.ValidateToken(request.Token);
            if (string.IsNullOrEmpty(userId))
                return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

            var currentRefreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == request.RefreshToken && x.IsActive);
            if (currentRefreshToken == null)
                return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

            currentRefreshToken.RevokedOn = DateTime.UtcNow;

            var roles = await _userManager.GetRolesAsync(user);
            var (newToken, expiresIn) = _jwtProvider.GenerateToken(user, roles);

            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                ExpiresOn = refreshTokenExpiration
            });

            await _userManager.UpdateAsync(user);

            var response = new AuthResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                roles,
                newToken,
                expiresIn,
                newRefreshToken,
                refreshTokenExpiration
            );

            return Result.Success(response);
        }

        public async Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
        {
            var userId = _jwtProvider.ValidateToken(token);
            if (string.IsNullOrEmpty(userId))
                return Result.Failure(UserErrors.InvalidCredentials);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure(UserErrors.UserNotFound);

            var targetRefreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == refreshToken && x.IsActive);
            if (targetRefreshToken == null)
                return Result.Failure(UserErrors.InvalidRefreshToken);

            targetRefreshToken.RevokedOn = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Result.Success();
        }

        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
