using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.AuthenticationDto;
using Smartera.BLL.Dtos.Common;
using Smartera.DAL.Entities;

namespace Smartera.BLL.Interfaces
{
    public interface IApplicationUserRepository : IBaseRepository<ApplicationUser>
    {
        Task<Result<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
        Task<Result<UserProfileResponse>> UpdateProfileAsync(string userId, string fullName, string? phoneNumber, string? address, CancellationToken cancellationToken = default);
        Task<Result<PaginatedList<UserProfileResponse>>> GetAllUsersAsync(RequestFilter request, CancellationToken cancellationToken = default);
    }
}
