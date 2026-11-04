namespace OrderApp;

public sealed record Product(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    bool IsActive
);

public sealed record Order(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal Total
);
