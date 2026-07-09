using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Infrastructure.Identity;

internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
            return Result<Guid>.Success(user.Id);
        
        return Result<Guid>.Failure(ToErrors(result));
    }

    public async Task<Result<Guid>> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result<Guid>.Failure(UserErrors.InvalidCredentials);
        
        var passwordResult = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordResult)
            return Result<Guid>.Failure(UserErrors.InvalidCredentials);

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result> AddToRoleAsync(Guid userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure(UserErrors.UserNotFound);

        var result = await _userManager.AddToRoleAsync(user, role);
        
        if (result.Succeeded)
            return Result.Success();
        
        return Result.Failure(ToErrors(result));
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Array.Empty<string>();
        
        var roleList = await _userManager.GetRolesAsync(user);

        return roleList.AsReadOnly();
    }

    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result.Failure(UserErrors.UserNotFound);

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
            return Result.Success();
        
        return Result.Failure(ToErrors(result));
    }
    
    private static List<Error> ToErrors(IdentityResult result) =>
        result.Errors.Select(e => new Error(e.Code, e.Description)).ToList();
}