using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}