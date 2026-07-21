using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Options;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.Features.RefreshTokens;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IdentityHub.Application.Tests;

public sealed class RefreshTokenUseCaseTests
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly RefreshTokenUseCase _sut;
    
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
    
    public RefreshTokenUseCaseTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _tokenService = Substitute.For<ITokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var timeProvider = Substitute.For<TimeProvider>();
        
        timeProvider
            .GetUtcNow()
            .Returns(FixedUtcNow);
        
        var options = Options.Create(new AuthenticationOptions
        {
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7,
            AbsoluteRefreshTokenLifetimeDays = 30
        });

        _sut = new RefreshTokenUseCase(
            _identityService,
            _tokenService,
            _refreshTokenRepository,
            timeProvider,
            options
            );
    }
    
    [Fact]
    public async Task HandleAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var utcNow = FixedUtcNow.UtcDateTime;
        
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        
        var oldRefreshTokenValue = "old-refresh-token";
        var newRefreshTokenValue = "new-refresh-token";
        var newAccessToken = "new-access-token";
        
        IReadOnlyList<string> roles = ["User"];

        var sessionStartedAt = utcNow.AddDays(-1);
        
        var oldRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: oldRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: sessionStartedAt,
            issuedAt: utcNow);

        _refreshTokenRepository
            .GetByTokenAsync(
                oldRefreshTokenValue,
                Arg.Any<CancellationToken>())
            .Returns(oldRefreshToken);

        _identityService
            .GetEmailAsync(userId)
            .Returns(Result<string>.Success(email));

        _identityService
            .GetRolesAsync(userId)
            .Returns(roles);

        _tokenService
            .CreateAccessToken(userId, email, roles)
            .Returns(newAccessToken);

        _tokenService
            .GenerateRefreshTokenValue()
            .Returns(newRefreshTokenValue);
        
        RefreshToken? addedRefreshToken = null;

        _refreshTokenRepository
            .AddAsync(
                Arg.Do<RefreshToken>(token => addedRefreshToken = token),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _refreshTokenRepository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await _sut.HandleAsync(
            oldRefreshTokenValue,
            CancellationToken.None);

        // Assert: returned result
        Assert.True(result.IsSuccess);
        Assert.Equal(newAccessToken, result.Value.AccessToken);
        Assert.Equal(newRefreshTokenValue, result.Value.RefreshToken);
        Assert.Equal(
            (long)TimeSpan.FromDays(7).TotalSeconds,
            result.Value.RefreshTokenExpiresIn);
        Assert.Equal(
            (long)TimeSpan.FromMinutes(15).TotalSeconds,
            result.Value.AccessTokenExpiresIn);
        
        // Assert: old token was rotated
        Assert.True(oldRefreshToken.IsRevoked);
        Assert.Equal(
            newRefreshTokenValue,
            oldRefreshToken.ReplacedByToken);
        
        // Assert: new token passed to repository
        Assert.NotNull(addedRefreshToken);
        Assert.Equal(userId, addedRefreshToken.UserId);
        Assert.Equal(newRefreshTokenValue, addedRefreshToken.Token);
        Assert.Equal(sessionStartedAt, addedRefreshToken.SessionStartedAt);
        Assert.True(addedRefreshToken.IsActiveAt(utcNow));
        
        // Assert: repository operations occurred
        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Any<RefreshToken>(),
            Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());   
    }
    
    [Fact]
    public async Task HandleAsync_ShouldRevokeAllActiveTokens_WhenReuseIsDetected()
    {
        // Arrange
        var utcNow = FixedUtcNow.UtcDateTime;
        
        var revokedAt = utcNow;
        
        var userId = Guid.NewGuid();
        
        var revokedRefreshTokenValue = "revoked-refresh-token-value";
        var firstActiveRefreshTokenValue = "first-active-refresh-token-value";
        var secondActiveRefreshTokenValue = "second-active-refresh-token-value";
    
        var revokedRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: revokedRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: utcNow.AddDays(-2),
            issuedAt: utcNow);
        
        revokedRefreshToken.Revoke(
            revokedAt: revokedAt, 
            replacedByToken: firstActiveRefreshTokenValue);
        
        var firstRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: firstActiveRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: utcNow.AddDays(-2),
            issuedAt: utcNow);
        
        var secondRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: secondActiveRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: utcNow.AddDays(-1),
            issuedAt: utcNow);
    
        IReadOnlyList<RefreshToken> activeRefreshTokens = 
        [
            firstRefreshToken, 
            secondRefreshToken
        ];
        
        _refreshTokenRepository
            .GetByTokenAsync(
                revokedRefreshTokenValue,
                Arg.Any<CancellationToken>())
            .Returns(revokedRefreshToken);
    
        _refreshTokenRepository
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>())
            .Returns(activeRefreshTokens);
        
        // Act
        var result = await _sut.HandleAsync(
            revokedRefreshTokenValue,
            CancellationToken.None);
    
        // Assert: returned result
        Assert.True(result.IsFailure);
    
        var error = Assert.Single(result.Errors);
        Assert.Equal(TokenErrors.InvalidRefreshToken, error);
        
        Assert.All(
            activeRefreshTokens, 
            token => Assert.True(token.IsRevoked));
        
        await _refreshTokenRepository
            .Received(1)
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .DidNotReceive()
            .AddAsync(
                Arg.Any<RefreshToken>(),
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .Received(1)
            .SaveChangesAsync(
            Arg.Any<CancellationToken>());
        
        await _identityService
            .DidNotReceive()
            .GetEmailAsync(Arg.Any<Guid>());
    
        _tokenService
            .DidNotReceive()
            .GenerateRefreshTokenValue();
    }
    
    [Fact]
    public async Task HandleAsync_ShouldFailWithGenericError_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var utcNow = FixedUtcNow.UtcDateTime;
        
        var userId = Guid.NewGuid();
        
        var expiredRefreshTokenValue = "expired-refresh-token-value";
    
        var expiredRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: expiredRefreshTokenValue,
            expiresAt: utcNow.AddMinutes(-1),
            sessionStartedAt: utcNow.AddDays(-10),
            issuedAt: utcNow.AddDays(-1));
    
        _refreshTokenRepository
            .GetByTokenAsync(
                expiredRefreshTokenValue,
                Arg.Any<CancellationToken>())
            .Returns(expiredRefreshToken);
        
        // Act
        var result = await _sut.HandleAsync(
            expiredRefreshTokenValue,
            CancellationToken.None);
    
        // Assert
        Assert.True(result.IsFailure);
    
        var error = Assert.Single(result.Errors);
        Assert.Equal(TokenErrors.InvalidRefreshToken, error);
        
        await _refreshTokenRepository
            .DidNotReceive()
            .AddAsync(
                Arg.Any<RefreshToken>(),
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .DidNotReceive()
            .SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldFailWithGenericError_WhenRefreshTokenIsUnknown()
    {
        // Arrange
        const string unknownRefreshTokenValue = "unknown-refresh-token-value";

        // Act
        var result = await _sut.HandleAsync(
            unknownRefreshTokenValue,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);

        var error = Assert.Single(result.Errors);
        Assert.Equal(TokenErrors.InvalidRefreshToken, error);

        await _refreshTokenRepository
            .Received(1)
            .GetByTokenAsync(
                unknownRefreshTokenValue,
                Arg.Any<CancellationToken>());

        await _refreshTokenRepository
            .DidNotReceive()
            .AddAsync(
                Arg.Any<RefreshToken>(),
                Arg.Any<CancellationToken>());

        await _refreshTokenRepository
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());

        await _identityService
            .DidNotReceive()
            .GetEmailAsync(Arg.Any<Guid>());

        _tokenService
            .DidNotReceive()
            .GenerateRefreshTokenValue();
    }

    [Fact]
    public async Task HandleAsync_ShouldFailWithGenericError_WhenAbsoluteExpiryIsReached()
    {
        // Arrange
        var utcNow = FixedUtcNow.UtcDateTime;
        var userId = Guid.NewGuid();
        
        var oldRefreshTokenValue = "old-refresh-token";
        
        var oldRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: oldRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: utcNow.AddDays(-30),
            issuedAt: utcNow);

        _refreshTokenRepository
            .GetByTokenAsync(
                oldRefreshTokenValue,
                Arg.Any<CancellationToken>())
            .Returns(oldRefreshToken);
        
        // Act
        var result = await _sut.HandleAsync(
            oldRefreshTokenValue,
            CancellationToken.None);
        
        // Assert
        Assert.True(result.IsFailure);
        
        var error = Assert.Single(result.Errors);
        Assert.Equal(TokenErrors.InvalidRefreshToken, error);
        
        await _refreshTokenRepository
            .Received(1)
            .GetByTokenAsync(
                oldRefreshTokenValue,
                Arg.Any<CancellationToken>());

        await _identityService
            .DidNotReceive()
            .GetEmailAsync(Arg.Any<Guid>());
        
        await _identityService
            .DidNotReceive()
            .GetRolesAsync(Arg.Any<Guid>());

        _tokenService
            .DidNotReceive()
            .CreateAccessToken(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>());

        _tokenService
            .DidNotReceive()
            .GenerateRefreshTokenValue();
        
        await _refreshTokenRepository
            .DidNotReceive()
            .AddAsync(
                Arg.Any<RefreshToken>(),
                Arg.Any<CancellationToken>());

        await _refreshTokenRepository
            .DidNotReceive()
            .SaveChangesAsync(
                Arg.Any<CancellationToken>());
    }
}