using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Authentication;
using Smartera.BLL.Dtos.AuthenticationDto;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.BLL.Services.Email;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtProvider _jwtProvider;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        private const int RefreshTokenExpiryDays = 14;
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            IJwtProvider jwtProvider,
            AppDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _jwtProvider = jwtProvider;
            _context = context;
            _emailService = emailService;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValidPassword)
                return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            if (!user.EmailConfirmed)
                return Result.Failure<AuthResponse>(UserErrors.EmailNotConfirmed);

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

        public async Task<Result<MessageResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return Result.Failure<MessageResponse>(UserErrors.DuplicatedEmail);

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
                    EmailConfirmed = false, // Must verify via OTP
                    RegistrationDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var firstError = createResult.Errors.First();
                    return Result.Failure<MessageResponse>(new Error(firstError.Code, firstError.Description, 400));
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

                // Generate and store OTP
                var otp = GenerateOtp();
                var hashedOtp = BCrypt.Net.BCrypt.HashPassword(otp);

                // Remove any previous verification OTPs for this email
                await _context.OtpEntries
                    .Where(e => e.Email == request.Email && e.Purpose == OtpPurpose.EmailVerification)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.OtpEntries.AddAsync(new OtpEntry
                {
                    Email = request.Email,
                    HashedOtp = hashedOtp,
                    Purpose = OtpPurpose.EmailVerification,
                    Expiry = DateTime.UtcNow.Add(OtpTtl),
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Send email verification OTP
                await _emailService.SendVerificationOtpEmailAsync(
                    user.Email!,
                    user.FullName,
                    otp,
                    OtpTtl,
                    cancellationToken);

                return Result.Success(new MessageResponse("Registration successful. Please check your email for the verification code."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<MessageResponse>(new Error("User.RegistrationFailed", ex.Message, 500));
            }
        }

        public async Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

            if (user.EmailConfirmed)
                return Result.Failure<AuthResponse>(UserErrors.EmailAlreadyConfirmed);

            var entry = await _context.OtpEntries
                .Where(e => e.Email == request.Email && e.Purpose == OtpPurpose.EmailVerification && e.Expiry > DateTime.UtcNow)
                .OrderByDescending(e => e.Expiry)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry == null || !BCrypt.Net.BCrypt.Verify(request.Otp, entry.HashedOtp))
                return Result.Failure<AuthResponse>(OtpErrors.InvalidOtp);

            // One-time use: remove entry
            _context.OtpEntries.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);

            // Confirm email
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Automatically log in user after verification
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

        public async Task<Result<MessageResponse>> ResendVerificationOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Success(new MessageResponse("If the email is registered, a new verification code has been sent."));

            if (user.EmailConfirmed)
                return Result.Failure<MessageResponse>(UserErrors.EmailAlreadyConfirmed);

            var otp = GenerateOtp();
            var hashedOtp = BCrypt.Net.BCrypt.HashPassword(otp);

            await _context.OtpEntries
                .Where(e => e.Email == request.Email && e.Purpose == OtpPurpose.EmailVerification)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.OtpEntries.AddAsync(new OtpEntry
            {
                Email = request.Email,
                HashedOtp = hashedOtp,
                Purpose = OtpPurpose.EmailVerification,
                Expiry = DateTime.UtcNow.Add(OtpTtl),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await _emailService.SendVerificationOtpEmailAsync(
                user.Email!,
                user.FullName,
                otp,
                OtpTtl,
                cancellationToken);

            return Result.Success(new MessageResponse("A new verification code has been sent to your email."));
        }

        public async Task<Result<MessageResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                var otp = GenerateOtp();
                var hashedOtp = BCrypt.Net.BCrypt.HashPassword(otp);

                await _context.OtpEntries
                    .Where(e => e.Email == request.Email && e.Purpose == OtpPurpose.PasswordReset)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.OtpEntries.AddAsync(new OtpEntry
                {
                    Email = request.Email,
                    HashedOtp = hashedOtp,
                    Purpose = OtpPurpose.PasswordReset,
                    Expiry = DateTime.UtcNow.Add(OtpTtl),
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                await _emailService.SendPasswordResetOtpEmailAsync(
                    user.Email!,
                    user.FullName,
                    otp,
                    OtpTtl,
                    cancellationToken);
            }

            // Always return success to prevent email enumeration
            return Result.Success(new MessageResponse("If your email is registered, a password reset code has been sent."));
        }

        public async Task<Result<VerifyOtpResponse>> VerifyPasswordResetOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<VerifyOtpResponse>(UserErrors.UserNotFound);

            var entry = await _context.OtpEntries
                .Where(e => e.Email == request.Email && e.Purpose == OtpPurpose.PasswordReset && e.Expiry > DateTime.UtcNow)
                .OrderByDescending(e => e.Expiry)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry == null || !BCrypt.Net.BCrypt.Verify(request.Otp, entry.HashedOtp))
                return Result.Failure<VerifyOtpResponse>(OtpErrors.InvalidOtp);

            // One-time use: remove entry
            _context.OtpEntries.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);

            // Generate official ASP.NET Core Identity Password Reset Token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            return Result.Success(new VerifyOtpResponse(resetToken));
        }

        public async Task<Result<MessageResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<MessageResponse>(UserErrors.UserNotFound);

            var result = await _userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.First();
                return Result.Failure<MessageResponse>(new Error(firstError.Code, firstError.Description, 400));
            }

            return Result.Success(new MessageResponse("Password has been reset successfully. You can now log in with your new password."));
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var userId = _jwtProvider.ValidateToken(request.Token);
            if (string.IsNullOrEmpty(userId))
                return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

            var existingRefreshToken = user.RefreshTokens
                .FirstOrDefault(rt => rt.Token == request.RefreshToken);

            if (existingRefreshToken == null || !existingRefreshToken.IsActive)
                return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

            existingRefreshToken.RevokedOn = DateTime.UtcNow;

            var newRefreshToken = GenerateRefreshToken();
            var newRefreshTokenExpiration = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                ExpiresOn = newRefreshTokenExpiration
            });

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var (newToken, expiresIn) = _jwtProvider.GenerateToken(user, roles);

            var response = new AuthResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                roles,
                newToken,
                expiresIn,
                newRefreshToken,
                newRefreshTokenExpiration
            );

            return Result.Success(response);
        }

        public async Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
        {
            var userId = _jwtProvider.ValidateToken(token);
            if (string.IsNullOrEmpty(userId))
                return Result.Failure(UserErrors.InvalidRefreshToken);

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return Result.Failure(UserErrors.UserNotFound);

            var existingRefreshToken = user.RefreshTokens
                .FirstOrDefault(rt => rt.Token == refreshToken);

            if (existingRefreshToken == null || !existingRefreshToken.IsActive)
                return Result.Failure(UserErrors.InvalidRefreshToken);

            existingRefreshToken.RevokedOn = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Result.Success();
        }

        private static string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
