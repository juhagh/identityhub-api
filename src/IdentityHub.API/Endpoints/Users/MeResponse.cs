namespace IdentityHub.API.Endpoints.Users;

public sealed record MeResponse(Guid UserId, string Email, IReadOnlyList<string> Roles);