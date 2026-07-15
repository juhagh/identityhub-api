namespace IdentityHub.Application.Features.Login;

// LoginResult returns AccessToken, RefreshToken and their lifetime so that client can act proactively prior to
// token expiry. This way the code also does not rely on client to calculate the expiry times.
public sealed record LoginResult(string AccessToken, string RefreshToken, long AccessTokenExpiresIn, long RefreshTokenExpiresIn);