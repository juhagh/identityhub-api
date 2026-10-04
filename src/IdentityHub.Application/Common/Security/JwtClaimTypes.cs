namespace IdentityHub.Application.Common.Security;

public static class JwtClaimTypes
{
    // Implemented as a public class because
    // the claim type is part of authentication contract, not an implementation detail
    public const string Role = "role";
}