using IdentityHub.Application.Common.Results;

namespace IdentityHub.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<Guid>> CreateUserAsync(string email, string password);
    Task<Result<Guid>> ValidateCredentialsAsync(string email, string password);
    Task<Result> AddToRoleAsync(Guid userId, string role);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId);
}