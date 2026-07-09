using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Results;
using IdentityHub.Domain.Constants;

namespace IdentityHub.Application.Features.Register;

public sealed class RegisterUserUseCase
{
    private readonly IIdentityService _identityService;
    
    public RegisterUserUseCase(IIdentityService identityService)
    {
        _identityService = identityService;
    }
    
    public async Task<Result<Guid>> HandleAsync(string email, string password)
    {
        var user = await _identityService.CreateUserAsync(email, password);
        if (user.IsFailure)
            return user;
        
        var rolesResult = await _identityService.AddToRoleAsync(user.Value, UserRoles.User);
        if (rolesResult.IsFailure)
        {
            // Compensating action: a user without their default role would be a broken
            // account (roles drive authorization), so registration is all-or-nothing.
            // UserManager operations can't share a transaction through the port, hence
            // compensation over atomicity. If the delete itself fails we accept the
            // orphan - the seeder guarantees roles exist, so this path is near-unreachable.
            await _identityService.DeleteUserAsync(user.Value);
            return Result<Guid>.Failure(rolesResult.Errors);
        }
            

        return Result<Guid>.Success(user.Value);
    }
}