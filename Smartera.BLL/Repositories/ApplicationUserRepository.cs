using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.AuthenticationDto;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Errors;
using Smartera.BLL.Interfaces;
using Smartera.DAL.Data;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Repositories
{
    public class ApplicationUserRepository : BaseRepository<ApplicationUser>, IApplicationUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationUserRepository(AppDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure<UserProfileResponse>(UserErrors.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);

            var profile = new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.PhoneNumber,
                user.Address,
                user.ImageUrl,
                roles,
                user.RegistrationDate
            );

            return Result.Success(profile);
        }

        public async Task<Result<UserProfileResponse>> UpdateProfileAsync(
            string userId,
            string fullName,
            string? phoneNumber,
            string? address,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure<UserProfileResponse>(UserErrors.UserNotFound);

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.Address = address;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.First();
                return Result.Failure<UserProfileResponse>(new Error(firstError.Code, firstError.Description, 400));
            }

            return await GetProfileAsync(userId, cancellationToken);
        }

        public async Task<Result<PaginatedList<UserProfileResponse>>> GetAllUsersAsync(
            RequestFilter request,
            CancellationToken cancellationToken = default)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var term = $"%{request.SearchValue.Trim().ToLower()}%";
                query = query.Where(u =>
                    EF.Functions.Like(u.FullName.ToLower(), term) ||
                    EF.Functions.Like(u.Email!.ToLower(), term) ||
                    EF.Functions.Like(u.PhoneNumber != null ? u.PhoneNumber.ToLower() : "", term));
            }

            var count = await query.CountAsync(cancellationToken);
            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var mappedList = new List<UserProfileResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                mappedList.Add(new UserProfileResponse(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address,
                    user.ImageUrl,
                    roles,
                    user.RegistrationDate
                ));
            }

            var paginated = new PaginatedList<UserProfileResponse>(mappedList, count, request.PageNumber, request.PageSize);
            return Result.Success(paginated);
        }
    }
}
