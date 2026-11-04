using System.Globalization;
using OrderApp;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var keyboard = new Product(1, "Keyboard", 15000m, 5, true);
var inactiveMouse = new Product(2, "Mouse", 7000m, 10, false);

ShowResult("Normal order", OrderService.CreateOrder(keyboard, 2));
ShowResult("Invalid quantity", OrderService.CreateOrder(keyboard, 0));
ShowResult("Inactive product", OrderService.CreateOrder(inactiveMouse, 1));
ShowResult("Insufficient stock", OrderService.CreateOrder(keyboard, 6));

static void ShowResult(string scenario, Result<Order> result)
{
    Console.WriteLine($"--- {scenario} ---");

    switch (result)
    {
        case Result<Order>.Success success:
            var order = success.Value;
            Console.WriteLine(
                $"SUCCESS: {order.ProductName}, quantity: {order.Quantity}, total: {order.Total:F2} KZT"
            );
            break;

        case Result<Order>.Failure failure:
            Console.WriteLine($"ERROR [{failure.Error.Code}]: {failure.Error.Message}");
            break;
    }

    Console.WriteLine();
}
