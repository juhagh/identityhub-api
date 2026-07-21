using IdentityHub.Application.Common.Results;

namespace IdentityHub.Application.Common.Errors;

public static class UserErrors
{

    public static readonly Error InvalidCredentials =
        new("Users.InvalidCredentials", "The supplied credentials are invalid.");

    public static readonly Error UserNotFound =
        new("Users.UserNotFound", "User not found.");
    
    public static readonly Error EmailNotConfigured =
        new("Users.EmailNotConfigured", "The user does not have an email address.");
}