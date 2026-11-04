using System.Text.Json;
using OrderApp;

internal static class WorkflowTests
{
    public static IEnumerable<(string Name, Action Run)> All()
    {
        yield return ("Lookup returns a present product", () =>
            Equal("Keyboard", Some(Catalog().Find(1)).Name));

        yield return ("Lookup returns None for an absent product", () =>
            Check(Catalog().Find(999) is Option<Product>.None, "Expected None."));

        yield return ("Zero stock is present and differs from absent stock", () =>
        {
            Equal(0, Some(Catalog().FindStock(3)));
            Check(Catalog().FindStock(999) is Option<int>.None, "Expected absent stock.");
        });

        yield return ("False active status is present and differs from absence", () =>
        {
            Equal(false, Some(Catalog().FindActiveStatus(2)));
            Check(Catalog().FindActiveStatus(999) is Option<bool>.None, "Expected absent status.");
        });

        yield return ("Required missing product becomes a useful Result failure", () =>
        {
            var error = Failure(OrderService.PlaceOrder(Catalog(), 999, 1));
            Equal("PRODUCT_NOT_FOUND", error.Code);
            Check(error.Message.Contains("999"), "Error must identify the product.");
        });

        yield return ("Map transforms the successful value", () =>
        {
            int received = 0;
            var result = new Result<int>.Success(4).Map(value =>
            {
                received = value;
                return value * 3;
            });
            Equal(4, received);
            Equal(12, Success(result));
        });

        yield return ("Bind receives the value and preserves the returned Result", () =>
        {
            int received = 0;
            Result<string> next = new Result<string>.Success("continued");
            var result = new Result<int>.Success(4).Bind(value =>
            {
                received = value;
                return next;
            });
            Equal(4, received);
            Check(ReferenceEquals(next, result), "Bind must return the next Result.");
        });

        yield return ("Bind preserves a failure returned by the next operation", () =>
        {
            Result<int> next = new Result<int>.Failure(new AppError("REJECTED", "Rejected request."));
            var result = new Result<int>.Success(4).Bind(_ => next);
            Check(ReferenceEquals(next, result), "Expected the exact returned failure.");
        });

        yield return ("Failed Map and Bind preserve the error and skip both callbacks", () =>
        {
            var error = new AppError("STOP", "Stop this chain.");
            Result<int> start = new Result<int>.Failure(error);
            int calls = 0;
            var result = start.Map(value => { calls++; return value + 1; })
                .Bind(value => { calls++; return (Result<string>)new Result<string>.Success(value.ToString()); });
            Equal(0, calls);
            Check(ReferenceEquals(error, Failure(result)), "The original error must be preserved.");
        });

        yield return ("Missing lookup skips validation and calculation", () =>
        {
            int calls = 0;
            var result = Catalog().Find(999)
                .ToResult(new AppError("PRODUCT_NOT_FOUND", "Product 999 is absent."))
                .Bind(product => { calls++; return OrderService.CreateOrder(product, 1); })
                .Map(order => { calls++; return order.Total; });
            Equal("PRODUCT_NOT_FOUND", Failure(result).Code);
            Equal(0, calls);
        });

        yield return ("Integrated chain returns the right order and new stock", () =>
        {
            var original = Catalog();
            var checkout = Success(OrderService.PlaceOrder(original, 1, 2));
            Equal(new Order(1, "Keyboard", 2, 30000m), checkout.Order);
            Equal(3, Some(checkout.UpdatedCatalog.FindStock(1)));
            Equal(5, Some(original.FindStock(1)));
            Equal(Some(original.Find(2)), Some(checkout.UpdatedCatalog.Find(2)));
        });

        yield return ("Application commits success and preserves state on all domain failures", () =>
        {
            var app = new OrderApplication(Catalog());
            Success(app.Submit(1, 2));
            var snapshot = app.CurrentCatalog;
            Equal("INVALID_QUANTITY", Failure(app.Submit(1, 0)).Code);
            Equal("PRODUCT_INACTIVE", Failure(app.Submit(2, 1)).Code);
            Equal("INSUFFICIENT_STOCK", Failure(app.Submit(1, 6)).Code);
            Equal("PRODUCT_NOT_FOUND", Failure(app.Submit(999, 1)).Code);
            Check(ReferenceEquals(snapshot, app.CurrentCatalog), "Failures changed the catalog.");
            Equal(3, Some(app.CurrentCatalog.FindStock(1)));
        });

        yield return ("Exhausting stock succeeds and the next order is rejected", () =>
        {
            var app = new OrderApplication(Catalog());
            Success(app.Submit(1, 5));
            Equal(0, Some(app.CurrentCatalog.FindStock(1)));
            Equal("INSUFFICIENT_STOCK", Failure(app.Submit(1, 1)).Code);
            Equal(0, Some(app.CurrentCatalog.FindStock(1)));
        });

        yield return ("Free product has a legitimate zero total", () =>
        {
            var product = new Product(4, "Gift", 0m, 1, true);
            Equal(0m, Success(OrderService.CreateOrder(product, 1)).Total);
        });

        yield return ("Excessive total becomes a domain failure without stock changes", () =>
        {
            var catalog = new Catalog(new[] { new Product(4, "Expensive", decimal.MaxValue, 2, true) });
            var app = new OrderApplication(catalog);
            Equal("TOTAL_TOO_LARGE", Failure(app.Submit(4, 2)).Code);
            Check(ReferenceEquals(catalog, app.CurrentCatalog), "Overflow changed the catalog.");
        });

        yield return ("File loading composes with the full order chain", () =>
        {
            using var files = new TemporaryFiles();
            string path = files.Write("products.json", JsonSerializer.Serialize(Catalog().Products));
            var result = CatalogFile.Load(path)
                .Bind(catalog => OrderService.PlaceOrder(catalog, 1, 2))
                .Map(checkout => checkout.Order.Total);
            Equal(30000m, Success(result));
        });

        yield return ("Rounded decimal division cannot hide an overflowing total", () =>
        {
            var product = new Product(4, "Boundary", decimal.MaxValue / 2m, 2, true);
            Equal("TOTAL_TOO_LARGE", Failure(OrderService.CreateOrder(product, 2)).Code);
            var ordinary = new Product(5, "Fractional", 0.25m, 3, true);
            Equal(0.75m, Success(OrderService.CreateOrder(ordinary, 3)).Total);
        });

        yield return ("Missing file reports the path and preserves state", () =>
        {
            using var files = new TemporaryFiles();
            var app = new OrderApplication(Catalog());
            var snapshot = app.CurrentCatalog;
            string path = Path.Combine(files.DirectoryPath, "missing.json");
            var error = Failure(app.Reload(path));
            Equal("FILE_NOT_FOUND", error.Code);
            Check(error.Message.Contains(path), "Error must identify the file.");
            Check(ReferenceEquals(snapshot, app.CurrentCatalog), "Missing file changed state.");
            Check(!File.Exists(path), "Loader must not fabricate a file.");
        });

        yield return ("Missing directory becomes a clear file failure", () =>
        {
            using var files = new TemporaryFiles();
            var path = Path.Combine(files.DirectoryPath, "absent", "products.json");
            Equal("FILE_NOT_FOUND", Failure(CatalogFile.Load(path)).Code);
        });

        yield return ("Malformed JSON after a valid row cannot partially update state", () =>
        {
            using var files = new TemporaryFiles();
            string json = "[{\"Id\":8,\"Name\":\"Valid\",\"Price\":1,\"Stock\":4,\"IsActive\":true},{bad}]";
            string path = files.Write("malformed.json", json);
            var app = new OrderApplication(Catalog());
            var snapshot = app.CurrentCatalog;
            var error = Failure(app.Reload(path));
            Equal("MALFORMED_JSON", error.Code);
            Check(error.Message.Contains(path), "Error must identify the file.");
            Check(ReferenceEquals(snapshot, app.CurrentCatalog), "Malformed file changed state.");
            Equal(json, File.ReadAllText(path));
        });

        yield return ("Invalid row after a valid row cannot partially update state", () =>
        {
            using var files = new TemporaryFiles();
            var rows = new[] { new Product(8, "Valid", 1m, 4, true), new Product(9, "Invalid", 1m, -1, true) };
            string json = JsonSerializer.Serialize(rows);
            string path = files.Write("invalid.json", json);
            var app = new OrderApplication(Catalog());
            var snapshot = app.CurrentCatalog;
            Equal("INVALID_CATALOG", Failure(app.Reload(path)).Code);
            Check(ReferenceEquals(snapshot, app.CurrentCatalog), "Invalid catalog changed state.");
            Equal(json, File.ReadAllText(path));
        });

        yield return ("Duplicate product IDs are rejected", () =>
        {
            using var files = new TemporaryFiles();
            var product = new Product(1, "Duplicate", 1m, 1, true);
            string path = files.Write("duplicates.json", JsonSerializer.Serialize(new[] { product, product }));
            Equal("INVALID_CATALOG", Failure(CatalogFile.Load(path)).Code);
        });

        yield return ("Missing required fields are rejected instead of defaulted", () =>
        {
            using var files = new TemporaryFiles();
            string path = files.Write("incomplete.json", "[{\"Id\":1,\"Name\":\"Incomplete\"}]");
            Equal("MALFORMED_JSON", Failure(CatalogFile.Load(path)).Code);
        });

        yield return ("Null catalog and null rows are rejected", () =>
        {
            using var files = new TemporaryFiles();
            Equal("INVALID_CATALOG", Failure(CatalogFile.Load(files.Write("null.json", "null"))).Code);
            Equal("INVALID_CATALOG", Failure(CatalogFile.Load(files.Write("null-row.json", "[null]"))).Code);
        });

        yield return ("Empty catalog is valid and lookup explicitly returns None", () =>
        {
            using var files = new TemporaryFiles();
            var catalog = Success(CatalogFile.Load(files.Write("empty.json", "[]")));
            Equal(0, catalog.Products.Count);
            Check(catalog.Find(1) is Option<Product>.None, "Empty catalog fabricated a record.");
        });

        yield return ("Successful reload replaces the entire catalog", () =>
        {
            using var files = new TemporaryFiles();
            var replacement = new[] { new Product(8, "Replacement", 0m, 0, false) };
            string path = files.Write("replacement.json", JsonSerializer.Serialize(replacement));
            var app = new OrderApplication(Catalog());
            Equal(1, Success(app.Reload(path)));
            Equal(replacement[0], Some(app.CurrentCatalog.Find(8)));
            Check(app.CurrentCatalog.Find(1) is Option<Product>.None, "Old product survived reload.");
        });
    }

    private static Catalog Catalog() => new(new[]
    {
        new Product(1, "Keyboard", 15000m, 5, true),
        new Product(2, "Mouse", 7000m, 10, false),
        new Product(3, "USB Cable", 1000m, 0, true)
    });

    private static T Success<T>(Result<T> result) where T : notnull => result switch
    {
        Result<T>.Success success => success.Value,
        Result<T>.Failure failure => throw new Exception($"Expected success: {failure.Error}"),
        _ => throw new Exception("Unexpected Result case.")
    };

    private static AppError Failure<T>(Result<T> result) where T : notnull => result switch
    {
        Result<T>.Failure failure => failure.Error,
        _ => throw new Exception("Expected failure.")
    };

    private static T Some<T>(Option<T> option) where T : notnull => option switch
    {
        Option<T>.Some some => some.Value,
        _ => throw new Exception("Expected a present value.")
    };

    private static void Equal<T>(T expected, T actual) =>
        Check(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{expected}', received '{actual}'.");

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private sealed class TemporaryFiles : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("orderapp-tests-");
        public string DirectoryPath => directory.FullName;

        public string Write(string name, string contents)
        {
            string path = Path.Combine(DirectoryPath, name);
            File.WriteAllText(path, contents);
            return path;
        }

        public void Dispose() => directory.Delete(true);
    }
}
