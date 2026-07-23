namespace IdentityHub.API.Endpoints.Auth;

public sealed record LoginRequest(string Email, string Password);