namespace OrderApp;

public abstract record Option<T> where T : notnull
{
    private Option() { }

    public sealed record Some : Option<T>
    {
        public T Value { get; }

        public Some(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed record None : Option<T>;

    public Result<T> ToResult(AppError missingError) => this switch
    {
        Some some => new Result<T>.Success(some.Value),
        None => new Result<T>.Failure(missingError),
        _ => throw new InvalidOperationException("Unknown option case.")
    };
}
