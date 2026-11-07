using System.Globalization;
using OrderApp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string dataDirectory = Path.Combine(AppContext.BaseDirectory, "sample-data");
string dataPath = Path.Combine(dataDirectory, "products.json");

if (args.Length == 0 || (args.Length == 1 && args[0] == "demo"))
{
    return RunDemo(dataPath, dataDirectory);
}

if (args.Length == 2 && args[0] == "load")
{
    return PrintResult("Load catalog", CatalogFile.Load(args[1]),
        catalog => $"Loaded {catalog.Products.Count} products.") ? 0 : 1;
}

if ((args.Length == 3 || args.Length == 4) && args[0] == "order"
    && int.TryParse(args[1], out int productId)
    && int.TryParse(args[2], out int quantity))
{
    var result = CatalogFile.Load(args.Length == 4 ? args[3] : dataPath)
        .Bind(catalog => OrderService.PlaceOrder(catalog, productId, quantity))
        .Map(checkout => checkout.Order);

    return PrintResult("Order", result, FormatOrder) ? 0 : 1;
}

Console.WriteLine("Usage: demo | load <path> | order <productId> <quantity> [path]");
return 2;

static int RunDemo(string path, string dataDirectory)
{
    var loaded = CatalogFile.Load(path);
    if (loaded is Result<Catalog>.Failure)
    {
        PrintResult("Startup", loaded, catalog => $"Loaded {catalog.Products.Count} products.");
        return 1;
    }

    if (loaded is not Result<Catalog>.Success success)
        return 1;

    var app = new OrderApplication(success.Value);
    PrintResult("Normal order", app.Submit(1, 2), FormatOrder);
    ShowOption("Stock after successful order", app.CurrentCatalog.FindStock(1));

    PrintResult("Invalid quantity", app.Submit(1, 0), FormatOrder);
    PrintResult("Inactive product", app.Submit(2, 1), FormatOrder);
    PrintResult("Insufficient stock", app.Submit(1, 6), FormatOrder);

    Console.WriteLine(" Task 3: Handle a Missing Item ");

    ShowProductLookup(app.CurrentCatalog, 1);
    ShowProductLookup(app.CurrentCatalog, 999);
    ShowProductLookup(app.CurrentCatalog, 3);
    PrintResult("Required missing product", app.Submit(999, 1), FormatOrder);
    ShowOption("Present zero stock", app.CurrentCatalog.FindStock(3));
    ShowOption("Absent stock", app.CurrentCatalog.FindStock(999));
    ShowOption("Present false active status", app.CurrentCatalog.FindActiveStatus(2));

    var before = app.CurrentCatalog;
    var temporaryDirectory = Directory.CreateTempSubdirectory("orderapp-demo-");
    try
    {
        PrintResult("Missing file", app.Reload(Path.Combine(temporaryDirectory.FullName, "missing.json")),
            count => $"Loaded {count} products.");
    }
    finally
    {
        temporaryDirectory.Delete(true);
    }

    Console.WriteLine($"State preserved after missing file: {ReferenceEquals(before, app.CurrentCatalog)}");
    PrintResult("Malformed file", app.Reload(Path.Combine(dataDirectory, "malformed.json")),
        count => $"Loaded {count} products.");
    Console.WriteLine($"State preserved after malformed file: {ReferenceEquals(before, app.CurrentCatalog)}");
    ShowOption("Final keyboard stock", app.CurrentCatalog.FindStock(1));
    return 0;
}

static string FormatOrder(Order order) =>
    $"{order.ProductName}, quantity: {order.Quantity}, total: {order.Total:F2} KZT";

static bool PrintResult<T>(string scenario, Result<T> result, Func<T, string> format) where T : notnull
{
    Console.WriteLine($"--- {scenario} ---");
    switch (result)
    {
        case Result<T>.Success success:
            Console.WriteLine($"SUCCESS: {format(success.Value)}");
            return true;
        case Result<T>.Failure failure:
            Console.WriteLine($"ERROR [{failure.Error.Code}]: {failure.Error.Message}");
            return false;
        default:
            throw new InvalidOperationException("Unknown result case.");
    }
}

static void ShowOption<T>(string label, Option<T> option) where T : notnull
{
    Console.WriteLine(option switch
    {
        Option<T>.Some some => $"{label}: present ({some.Value})",
        Option<T>.None => $"{label}: absent",
        _ => throw new InvalidOperationException("Unknown option case.")
    });
}

static void ShowProductLookup(Catalog catalog, int productId)
{
    Option<Product> result = catalog.Find(productId);

    switch (result)
    {
        case Option<Product>.Some found:
            Product product = found.Value;

            Console.WriteLine(
                $"Product found: {product.Name}. Stock: {product.Stock}."
            );

            if (product.Stock == 0)
            {
                Console.WriteLine(
                    "The product exists, but its stock is zero."
                );
            }

            break;

        case Option<Product>.None:
            Console.WriteLine(
                $"Product with ID {productId} was not found."
            );
            break;
    }
}
