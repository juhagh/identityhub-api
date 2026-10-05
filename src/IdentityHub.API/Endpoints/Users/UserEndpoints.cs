using System.Security.Claims;
using IdentityHub.API.Common.Extensions;
using IdentityHub.Application.Common.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdentityHub.API.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users");
        
        group.MapGet("/me", GetMe)
            .RequireAuthorization();
        
        return app;
    }

    private static Ok<MeResponse> GetMe(ClaimsPrincipal user)
    {
        var userId = user.GetRequiredUserId();
        var email = user.GetRequiredEmail();
        var roles = user
            .FindAll(JwtClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();

        return TypedResults.Ok(new MeResponse(
            UserId: userId,
            Email: email,
            Roles: roles));
    }
}