using System.Numerics;

namespace OrderApp;

public static class OrderService
{
    public static Result<Checkout> PlaceOrder(Catalog catalog, int productId, int quantity)
    {
        return catalog.Find(productId)
            .ToResult(new AppError("PRODUCT_NOT_FOUND", $"Product ID {productId} was not found."))
            .Bind(product => CreateOrder(product, quantity))
            .Map(order => new Checkout(
                order,
                new Catalog(catalog.Products.Select(product =>
                    product.Id == order.ProductId
                        ? product with { Stock = product.Stock - order.Quantity }
                        : product))
            ));
    }

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

        if (TotalExceedsDecimal(product.Price, quantity))
        {
            return new Result<Order>.Failure(new AppError(
                "TOTAL_TOO_LARGE",
                $"Order total for product '{product.Name}' exceeds the supported amount."
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

    private static bool TotalExceedsDecimal(decimal price, int quantity)
    {
        // Compare the exact scaled integers before decimal multiplication can overflow.
        int[] bits = decimal.GetBits(price);
        var units = (new BigInteger((uint)bits[2]) << 64)
            + (new BigInteger((uint)bits[1]) << 32) + (uint)bits[0];
        int scale = (bits[3] >> 16) & 0xff;
        return units * quantity > new BigInteger(decimal.MaxValue) * BigInteger.Pow(10, scale);
    }
}
