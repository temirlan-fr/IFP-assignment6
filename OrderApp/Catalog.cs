namespace OrderApp;

public sealed class Catalog
{
    public IReadOnlyList<Product> Products { get; }

    public Catalog(IEnumerable<Product> products)
    {
        Products = Array.AsReadOnly(products.ToArray());
    }

    public Option<Product> Find(int id)
    {
        foreach (var product in Products)
        {
            if (product.Id == id)
                return new Option<Product>.Some(product);
        }

        return new Option<Product>.None();
    }

    public Option<int> FindStock(int id) => Find(id) switch
    {
        Option<Product>.Some some => new Option<int>.Some(some.Value.Stock),
        _ => new Option<int>.None()
    };

    public Option<bool> FindActiveStatus(int id) => Find(id) switch
    {
        Option<Product>.Some some => new Option<bool>.Some(some.Value.IsActive),
        _ => new Option<bool>.None()
    };
}
