namespace OrderApp;

public sealed record AppError(string Code, string Message);

public abstract record Result<T> where T : notnull
{
    private Result() { }

    public Result<U> Map<U>(Func<T, U> transform) where U : notnull => this switch
    {
        Success success => new Result<U>.Success(transform(success.Value)),
        Failure failure => new Result<U>.Failure(failure.Error),
        _ => throw new InvalidOperationException("Unknown result case.")
    };

    public Result<U> Bind<U>(Func<T, Result<U>> next) where U : notnull => this switch
    {
        Success success => next(success.Value),
        Failure failure => new Result<U>.Failure(failure.Error),
        _ => throw new InvalidOperationException("Unknown result case.")
    };

    public sealed record Success : Result<T>
    {
        public T Value { get; }

        public Success(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed record Failure : Result<T>
    {
        public AppError Error { get; }

        public Failure(AppError error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }
    }
}
