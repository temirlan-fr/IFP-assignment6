namespace OrderApp;

public sealed record AppError(string Code, string Message);

public abstract record Result<T> where T : notnull
{
    private Result() { }

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
