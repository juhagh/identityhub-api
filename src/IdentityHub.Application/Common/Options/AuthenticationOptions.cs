using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Application.Common.Options;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    // Short‑lived access tokens (default 15 min)
    // to reduce blast radius if a token is leaked or intercepted.
    // Forces clients to rely on refresh‑token rotation for continued access and enables revoking access.
    [Range(1, 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    
    // Refresh tokens provide a reasonable session duration without requiring
    // frequent sign-ins. Rotation limits reuse of compromised refresh tokens.
    [Range(1, 30)]
    public int RefreshTokenLifetimeDays { get; init; } = 7;
    
    // Limits the total session lifetime, even when refresh-token rotation
    // would otherwise extend the session through a sliding expiration window.
    [Range(1, 365)]
    public int AbsoluteRefreshTokenLifetimeDays { get; init; } = 30;
}