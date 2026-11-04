using OrderApp;

var tests = new (string Name, Action Run)[]
{
    ("Valid order contains the correct data and total", () =>
    {
        var order = RequireSuccess(OrderService.CreateOrder(Keyboard(), 2));
        Equal(new Order(1, "Keyboard", 2, 30000m), order);
    }),
    ("Zero quantity returns a useful error", () =>
    {
        var error = RequireFailure(OrderService.CreateOrder(Keyboard(), 0));
        Equal("INVALID_QUANTITY", error.Code);
        Equal("Quantity must be greater than zero. Received: 0.", error.Message);
    }),
    ("Negative quantity returns a useful error", () =>
    {
        var error = RequireFailure(OrderService.CreateOrder(Keyboard(), -2));
        Equal("INVALID_QUANTITY", error.Code);
        Equal("Quantity must be greater than zero. Received: -2.", error.Message);
    }),
    ("Inactive product returns a useful error", () =>
    {
        var product = Keyboard() with { IsActive = false };
        var error = RequireFailure(OrderService.CreateOrder(product, 1));
        Equal("PRODUCT_INACTIVE", error.Code);
        Equal("Product 'Keyboard' is not available for sale.", error.Message);
    }),
    ("Insufficient stock returns the requested and available quantities", () =>
    {
        var error = RequireFailure(OrderService.CreateOrder(Keyboard(), 6));
        Equal("INSUFFICIENT_STOCK", error.Code);
        Equal("Product 'Keyboard': requested 6, available 5.", error.Message);
    }),
    ("Ordering exactly the available stock succeeds", () =>
    {
        var order = RequireSuccess(OrderService.CreateOrder(Keyboard(), 5));
        Equal(new Order(1, "Keyboard", 5, 75000m), order);
    }),
    ("Rejected order preserves the original product", () =>
    {
        var product = Keyboard();
        var snapshot = product with { };
        RequireFailure(OrderService.CreateOrder(product, 6));
        Equal(snapshot, product);
    })
};

int failures = 0;

foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL: {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"Tests: {tests.Length}, passed: {tests.Length - failures}, failed: {failures}");
return failures == 0 ? 0 : 1;

static Product Keyboard() => new(1, "Keyboard", 15000m, 5, true);

static Order RequireSuccess(Result<Order> result) => result switch
{
    Result<Order>.Success success => success.Value,
    Result<Order>.Failure failure => throw new Exception(
        $"Expected success, received {failure.Error.Code}."
    ),
    _ => throw new Exception("Unexpected result case.")
};

static AppError RequireFailure(Result<Order> result) => result switch
{
    Result<Order>.Failure failure => failure.Error,
    _ => throw new Exception("Expected failure, received success.")
};

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new Exception($"Expected '{expected}', received '{actual}'.");
    }
}
