using IdentityHub.Application.Common.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdentityHub.API.Endpoints;

public static class ResultExtensions
{
    /// Formats a failed Result as 400 with the standard validation-problem shape.
    /// Status is fixed by the ValidationProblem contract; use ToProblem for anything else.
    public static ValidationProblem ToValidationProblem(this Result result)
    {
        EnsureFailure(result);
        var errors = result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        return TypedResults.ValidationProblem(errors);
    }

    /// Formats a failed Result as a ProblemDetails response with the status the
    /// endpoint's contract demands (e.g. 401 for failed login).
    public static ProblemHttpResult ToProblem(this Result result, int statusCode)
    {
        EnsureFailure(result);
        return TypedResults.Problem(
            detail: result.Errors[0].Description,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["errorCodes"] = result.Errors.Select(e => e.Code).ToArray()
            });
    }

    private static void EnsureFailure(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot map a successful result to a problem response.");
    }
}