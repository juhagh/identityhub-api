namespace IdentityHub.Domain.Constants;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    
    public static readonly IReadOnlyList<string> All = [Admin, User];
}