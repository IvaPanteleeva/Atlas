namespace Atlas.Application.Common;

// Same shape as Gemsoft's Result, but carries a typed Error instead of List<string>,
// so the API knows which HTTP status to return. No implicit conversions from string/bool:
// they would lose the error type.
public class Result
{
    private readonly Error? error;

    protected Result(Error? error)
        => this.error = error;

    public bool Succeeded => this.error is null;

    public Error Error
        => this.error
            ?? throw new InvalidOperationException($"{nameof(this.Error)} is not available with a successful result.");

    public static Result Success
        => new(null);

    public static Result Failure(Error error)
        => new(error);

    public static implicit operator Result(Error error)
        => Failure(error);
}

public sealed class Result<TData> : Result
{
    private readonly TData data;

    private Result(TData data, Error? error)
        : base(error)
        => this.data = data;

    public TData Data
        => this.Succeeded
            ? this.data
            : throw new InvalidOperationException(
                $"{nameof(this.Data)} is not available with a failed result. Use {nameof(this.Error)} instead.");

    public static Result<TData> SuccessWith(TData data)
        => new(data, null);

    public new static Result<TData> Failure(Error error)
        => new(default!, error);

    public static implicit operator Result<TData>(Error error)
        => Failure(error);

    public static implicit operator Result<TData>(TData data)
        => SuccessWith(data);
}
