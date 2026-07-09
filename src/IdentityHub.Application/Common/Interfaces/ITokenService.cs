namespace IdentityHub.Application.Common.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(Guid userId, string email, IReadOnlyList<string> roles);
    string GenerateRefreshTokenValue();
}