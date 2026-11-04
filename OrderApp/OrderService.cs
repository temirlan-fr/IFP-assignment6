namespace OrderApp;

public static class OrderService
{
    public static Result<Order> CreateOrder(Product product, int quantity)
    {
        if (quantity <= 0)
        {
            return new Result<Order>.Failure(new AppError(
                "INVALID_QUANTITY",
                $"Quantity must be greater than zero. Received: {quantity}."
            ));
        }

        if (!product.IsActive)
        {
            return new Result<Order>.Failure(new AppError(
                "PRODUCT_INACTIVE",
                $"Product '{product.Name}' is not available for sale."
            ));
        }

        if (quantity > product.Stock)
        {
            return new Result<Order>.Failure(new AppError(
                "INSUFFICIENT_STOCK",
                $"Product '{product.Name}': requested {quantity}, available {product.Stock}."
            ));
        }

        var order = new Order(
            product.Id,
            product.Name,
            quantity,
            product.Price * quantity
        );

        return new Result<Order>.Success(order);
    }
}
