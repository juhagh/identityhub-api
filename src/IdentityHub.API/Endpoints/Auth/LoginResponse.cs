namespace IdentityHub.API.Endpoints.Auth;

public sealed record LoginResponse(string AccessToken, string RefreshToken, long AccessTokenExpiresIn, long RefreshTokenExpiresIn);