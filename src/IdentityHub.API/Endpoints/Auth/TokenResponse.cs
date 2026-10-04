using IdentityHub.Application.Common.Models;

namespace IdentityHub.API.Endpoints.Auth;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    long AccessTokenExpiresInSeconds,
    long RefreshTokenExpiresInSeconds)
{
    public string TokenType => "Bearer";

    public static TokenResponse From(AuthenticationResult r)
    {
        return new TokenResponse(
            AccessToken: r.AccessToken,
            RefreshToken: r.RefreshToken,
            AccessTokenExpiresInSeconds: r.AccessTokenExpiresInSeconds,
            RefreshTokenExpiresInSeconds: r.RefreshTokenExpiresInSeconds);
    }
}