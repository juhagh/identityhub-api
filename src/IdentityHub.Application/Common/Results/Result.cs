namespace IdentityHub.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    protected Result(bool success, List<Error> errors)
    {
        if (success && errors.Count != 0)
            throw new InvalidOperationException("A successful result cannot contain errors.");
         
        if (!success && errors.Count == 0)
            throw new InvalidOperationException("A failed result must contain at least one error.");
        
        IsSuccess = success;
        Errors = errors;
    }

    public static Result Success() => new Result(true, new List<Error>());
    public static Result Failure(params IReadOnlyList<Error> errors) => new Result(false, errors.ToList());
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");
    
    private Result(T value) : base(true, new List<Error>())
    {
        _value = value;
    }

    private Result(List<Error> errors) : base(false, errors)
    {
    }
    
    public static Result<T> Success(T value) => new Result<T>(value);
    public new static Result<T> Failure(params IReadOnlyList<Error> errors) => new Result<T>(errors.ToList());
}