using IdentityHub.Application.Common.Results;

namespace IdentityHub.Application.Common.Errors;

public static class TokenErrors
{
    // Generic error for all failure cases to prevent probing stolen tokens 
    // and to not leak any state or implementation details. 
    public static readonly Error InvalidRefreshToken =
        new("Tokens.InvalidRefreshToken", "Invalid refresh token.");
}