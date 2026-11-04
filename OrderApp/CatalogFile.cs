using System.Text.Json;

namespace OrderApp;

public static class CatalogFile
{
    public static Result<Catalog> Load(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var rows = JsonSerializer.Deserialize<List<ProductInput?>>(json);

            if (rows is null)
                return InvalidData(path, "Expected a JSON array of products.");

            var products = new List<Product>();
            var ids = new HashSet<int>();

            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (row is null)
                    return InvalidData(path, $"Product at index {index} is null.");

                if (row.Id <= 0 || !ids.Add(row.Id))
                    return InvalidData(path, $"Product at index {index} needs a unique positive ID.");

                if (string.IsNullOrWhiteSpace(row.Name) || row.Price < 0 || row.Stock < 0)
                    return InvalidData(path, $"Product ID {row.Id} needs a name, nonnegative price and nonnegative stock.");

                products.Add(new Product(row.Id, row.Name, row.Price, row.Stock, row.IsActive));
            }

            return new Result<Catalog>.Success(new Catalog(products));
        }
        catch (FileNotFoundException)
        {
            return FileFailure("FILE_NOT_FOUND", path, "The data file does not exist.");
        }
        catch (DirectoryNotFoundException)
        {
            return FileFailure("FILE_NOT_FOUND", path, "The data directory does not exist.");
        }
        catch (JsonException exception)
        {
            return FileFailure("MALFORMED_JSON", path, $"Invalid JSON or product structure. {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return FileFailure("FILE_ACCESS_DENIED", path, exception.Message);
        }
        catch (IOException exception)
        {
            return FileFailure("FILE_READ_ERROR", path, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return FileFailure("INVALID_FILE_PATH", path, exception.Message);
        }
        catch (NotSupportedException exception)
        {
            return FileFailure("INVALID_FILE_PATH", path, exception.Message);
        }
    }

    private static Result<Catalog> InvalidData(string path, string detail) =>
        FileFailure("INVALID_CATALOG", path, detail);

    private static Result<Catalog> FileFailure(string code, string path, string detail) =>
        new Result<Catalog>.Failure(new AppError(code, $"Loading catalog '{path}': {detail}"));

    private sealed record ProductInput
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required decimal Price { get; init; }
        public required int Stock { get; init; }
        public required bool IsActive { get; init; }
    }
}
