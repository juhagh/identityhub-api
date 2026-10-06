using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Features.Logout;
using IdentityHub.Domain.Entities;
using NSubstitute;

namespace IdentityHub.Application.Tests;

public sealed class LogoutUserUseCaseTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly LogoutUserUseCase _sut;
    private readonly TimeProvider _timeProvider;

    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 10, 05, 15, 0, 0, TimeSpan.Zero);
    
    public LogoutUserUseCaseTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _timeProvider = Substitute.For<TimeProvider>();
        
        _timeProvider
            .GetUtcNow()
            .Returns(FixedUtcNow);
        
        _sut = new LogoutUserUseCase(_refreshTokenRepository, _timeProvider);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenAlreadyRevoked_ShouldReturnSuccessWithoutSaving()
    {
        var userId = Guid.NewGuid();
        var utcNow = FixedUtcNow.UtcDateTime;
        var sessionStartedAt = utcNow.AddDays(-2);
        var issuedAt = utcNow.AddDays(-1);
        var revokedAt = utcNow.AddHours(-1);
        var revokedRefreshTokenValue = "old-refresh-token";
        
        var revokedRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: revokedRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: sessionStartedAt,
            issuedAt: issuedAt);
        
        revokedRefreshToken.Revoke(
            revokedAt: revokedAt, 
            replacedByToken: null);
        
        _refreshTokenRepository
            .GetByTokenAsync(
                revokedRefreshTokenValue,
                Arg.Any<CancellationToken>())
            .Returns(revokedRefreshToken);

        await _sut.HandleAsync(revokedRefreshTokenValue, CancellationToken.None);
        
        await _refreshTokenRepository
            .Received(1)
            .GetByTokenAsync(
                revokedRefreshTokenValue,
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTokenIsActive_ShouldRevokeTokenAndSaveChanges()
    {
        var userId = Guid.NewGuid();
        var utcNow = FixedUtcNow.UtcDateTime;
        var sessionStartedAt = utcNow.AddDays(-2);
        var issuedAt = utcNow.AddDays(-1);
        var refreshTokenValue = "refresh-token-value";
        
        var refreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: refreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: sessionStartedAt,
            issuedAt: issuedAt);
        
        _refreshTokenRepository
            .GetByTokenAsync(
                refreshTokenValue,
                Arg.Any<CancellationToken>())
            .Returns(refreshToken);
        
        await _sut.HandleAsync(refreshTokenValue, CancellationToken.None);
        Assert.True(refreshToken.IsRevoked);
        Assert.Equal(utcNow, refreshToken.RevokedAt);
        
        await _refreshTokenRepository
            .Received(1)
            .GetByTokenAsync(
                refreshTokenValue,
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTokenDoesNotExist_ShouldReturnSuccess()
    {
        var refreshTokenValue = "refresh-token-value";
        await _sut.HandleAsync(refreshTokenValue, CancellationToken.None);
        
        await _refreshTokenRepository
            .Received(1)
            .GetByTokenAsync(
                refreshTokenValue,
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}