using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.Token == token, ct);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken ct = default)
    {
        return await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId &&
                            // Must mirror RefreshToken.IsActive
                            token.RevokedAt == null &&
                            token.ExpiresAt > utcNow)
            .ToListAsync(ct);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}