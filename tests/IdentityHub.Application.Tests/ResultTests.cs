using IdentityHub.Application.Common.Results;

namespace IdentityHub.Application.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_Should_Create_Result_With_No_Errors()
    {
        var result = Result.Success();
        
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_Should_Create_Result_With_One_Error()
    {
        var error = new Error("400", "ErrorDescription");
        
        var result = Result.Failure(error);
        
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]

    public void Failure_Should_Create_Result_With_Multiple_Errors()
    {
        var firstError = new Error("400", "First error");
        var secondError = new Error("401", "Second error");

        var result = Result.Failure(firstError, secondError);
        
        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(firstError, result.Errors);
        Assert.Contains(secondError, result.Errors);
    }
    
    [Fact]
    public void Success_With_Value_Should_Create_Result_With_Value_And_No_Errors()
    {
        var result = Result<string>.Success("SuccessMessage");
        
        Assert.True(result.IsSuccess);
        Assert.Equal("SuccessMessage", result.Value);
        Assert.Empty(result.Errors);
    }
    
    [Fact]
    public void Failure_With_Value_ShouldThrow()
    {
        var error = new Error("400", "ErrorDescription");
        var result = Result<string>.Failure(error);
        
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_With_No_Errors_Should_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure());
    }
}