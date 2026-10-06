using System.Security.Claims;
using IdentityHub.Application.Features.Login;
using IdentityHub.Application.Features.Logout;
using IdentityHub.Application.Features.LogoutAll;
using IdentityHub.Application.Features.RefreshTokens;
using IdentityHub.Application.Features.Register;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityHub.API.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapPost("/logout-all", LogoutAllAsync).RequireAuthorization();
        return app;
    }

    private static async Task<Results<Created<RegisterResponse>, ValidationProblem>> RegisterAsync(
        RegisterRequest request, RegisterUserUseCase useCase)
    {
        var result = await useCase.HandleAsync(request.Email, request.Password);
        if (result.IsFailure)
            return result.ToValidationProblem();

        return TypedResults.Created((string?)null, new RegisterResponse(result.Value));
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request, LoginUserUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.LoginAsync(request.Email, request.Password, ct);
        if (result.IsFailure)
            return result.ToProblem(StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(TokenResponse.From(result.Value));
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshRequest request, RefreshTokenUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.HandleAsync(request.RefreshToken, ct);
        if (result.IsFailure)
            return result.ToProblem(StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(TokenResponse.From(result.Value));
    }
    
    private static async Task<NoContent> LogoutAsync(
        LogoutRequest request, LogoutUserUseCase useCase, CancellationToken ct)
    {
        await useCase.HandleAsync(request.RefreshToken, ct);

        return TypedResults.NoContent();
    }
    
    private static async Task<NoContent> LogoutAllAsync(
        ClaimsPrincipal user, LogoutAllSessionsUseCase useCase, CancellationToken ct)
    {
        var userIdClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        
        if (!Guid.TryParse(userIdClaim, out Guid userId))
            throw new InvalidOperationException("Authenticated user has no valid subject claim.");
        
        await useCase.HandleAsync(userId, ct);

        return TypedResults.NoContent();
    }
}