namespace OrderApp;

public sealed class OrderApplication
{
    public Catalog CurrentCatalog { get; private set; }

    public OrderApplication(Catalog catalog)
    {
        CurrentCatalog = catalog;
    }

    public Result<int> Reload(string path)
    {
        var result = CatalogFile.Load(path);
        if (result is Result<Catalog>.Success success)
            CurrentCatalog = success.Value;

        return result.Map(catalog => catalog.Products.Count);
    }

    public Result<Order> Submit(int productId, int quantity)
    {
        var result = OrderService.PlaceOrder(CurrentCatalog, productId, quantity);
        if (result is Result<Checkout>.Success success)
            CurrentCatalog = success.Value.UpdatedCatalog;

        return result.Map(checkout => checkout.Order);
    }
}
