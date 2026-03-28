namespace LoreForge.Core.Primitives;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException(
                "Invalid state: success result cannot have an error."
            );

        if (!isSuccess && error is null)
            throw new InvalidOperationException(
                "Invalid state: failure result must have an error."
            );

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);

    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException("Cannot access value of a failed result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
