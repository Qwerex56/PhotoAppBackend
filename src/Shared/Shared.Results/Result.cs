namespace Shared.Results;

/// <summary>
/// Represents the result of an operation that can fail with an error or succeed with a value.
/// </summary>
public sealed record Result<T>
{
    private Result(T? value, bool isSuccess, Error? error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets the value if the result represents success.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the result represents failure.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) 
        => new(value, true, null);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T> Failure(Error error) 
        => new(default, false, error);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) 
        => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result<T>(Error error) 
        => Failure(error);

    /// <summary>
    /// Applies a transformation function to the value if successful.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (IsFailure)
            return Result<TNew>.Failure(Error!);

        return Result<TNew>.Success(mapper(Value!));
    }

    /// <summary>
    /// Applies a function that returns a Result if successful.
    /// </summary>
    public Result<TNew> Then<TNew>(Func<T, Result<TNew>> next)
    {
        if (IsFailure)
            return Result<TNew>.Failure(Error!);

        return next(Value!);
    }

    /// <summary>
    /// Executes an action if successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
            action(Value!);

        return this;
    }

    /// <summary>
    /// Executes an action if failed.
    /// </summary>
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(Error!);

        return this;
    }

    /// <summary>
    /// Gets the value or throws an exception if failed.
    /// </summary>
    public T GetValueOrThrow()
    {
        if (IsFailure)
            throw new InvalidOperationException($"Result failed with error: {Error!.Code} - {Error.Description}");

        return Value!;
    }

    /// <summary>
    /// Gets the value or a default value if failed.
    /// </summary>
    public T? GetValueOrDefault(T? defaultValue = default)
        => IsSuccess ? Value : defaultValue;
}

/// <summary>
/// Represents the result of an operation that can fail with an error but has no return value.
/// </summary>
public sealed record Result
{
    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the result represents failure.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() 
        => new(true, null);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error) 
        => new(false, error);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result(Error error) 
        => Failure(error);

    /// <summary>
    /// Executes an action if successful.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
            action();

        return this;
    }

    /// <summary>
    /// Executes an action if failed.
    /// </summary>
    public Result OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(Error!);

        return this;
    }

    /// <summary>
    /// Converts to a Result{T} carrying the error or a default success value.
    /// </summary>
    public Result<T> ToResult<T>(T value)
    {
        if (IsFailure)
            return Result<T>.Failure(Error!);

        return Result<T>.Success(value);
    }

    /// <summary>
    /// Throws an exception if the result failed.
    /// </summary>
    public void ThrowIfFailed()
    {
        if (IsFailure)
            throw new InvalidOperationException($"Result failed with error: {Error!.Code} - {Error.Description}");
    }
}
