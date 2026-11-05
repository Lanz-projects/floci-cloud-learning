using System.Globalization;
using Amazon.DynamoDBv2.Model;
using FlociDynamoDbDemo.Services;

namespace FlociDynamoDbDemo.Menus;

internal sealed class OrderMenu
{
    private const string DemoCustomerId = "demo-customer-001";
    private readonly OrderService _orderService;

    public OrderMenu(OrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            ShowMenu();
            Console.Write("Choose an option: ");

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await CreateOrderAsync();
                    break;
                case "2":
                    await GetOrderAsync();
                    break;
                case "3":
                    await QueryOrdersAsync();
                    break;
                case "4":
                    await UpdateOrderStatusAsync();
                    break;
                case "5":
                    await DeleteOrderAsync();
                    break;
                case "6":
                    await RunSampleWorkflowAsync();
                    break;
                case "0":
                    Console.WriteLine("Goodbye.");
                    return;
                default:
                    Console.WriteLine("Choose one of the listed options.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine("=== Floci DynamoDB Orders ===");
        Console.WriteLine("1. Create an order");
        Console.WriteLine("2. Get an order by its keys");
        Console.WriteLine("3. List orders for a customer");
        Console.WriteLine("4. Update an order status");
        Console.WriteLine("5. Delete an order");
        Console.WriteLine("6. Run a sample workflow");
        Console.WriteLine("0. Exit");
    }

    private async Task CreateOrderAsync()
    {
        var customerId = ReadRequiredValue("Customer ID: ");
        var orderId = ReadRequiredValue("Order ID: ");
        var status = ReadRequiredValue("Status: ");
        var total = ReadOrderTotal();

        await _orderService.CreateOrderAsync(customerId, orderId, status, total);
        Console.WriteLine($"Saved order '{orderId}'.");
    }

    private async Task GetOrderAsync()
    {
        var (customerId, orderId) = ReadOrderKey();
        var order = await _orderService.GetOrderAsync(customerId, orderId);

        if (order is null)
        {
            Console.WriteLine("Order not found.");
            return;
        }

        DisplayOrder(order);
    }

    private async Task QueryOrdersAsync()
    {
        var customerId = ReadRequiredValue("Customer ID: ");
        await DisplayCustomerOrdersAsync(customerId);
    }

    private async Task UpdateOrderStatusAsync()
    {
        var (customerId, orderId) = ReadOrderKey();
        var existingOrder = await _orderService.GetOrderAsync(customerId, orderId);

        if (existingOrder is null)
        {
            Console.WriteLine("Order not found; nothing was updated.");
            return;
        }

        var newStatus = ReadRequiredValue("New status: ");
        await _orderService.UpdateOrderStatusAsync(customerId, orderId, newStatus);
        Console.WriteLine($"Updated order '{orderId}' to '{newStatus}'.");
    }

    private async Task DeleteOrderAsync()
    {
        var (customerId, orderId) = ReadOrderKey();
        var existingOrder = await _orderService.GetOrderAsync(customerId, orderId);

        if (existingOrder is null)
        {
            Console.WriteLine("Order not found; nothing was deleted.");
            return;
        }

        await _orderService.DeleteOrderAsync(customerId, orderId);
        Console.WriteLine($"Deleted order '{orderId}'.");
    }

    private async Task RunSampleWorkflowAsync()
    {
        Console.WriteLine($"Seeding sample orders for {DemoCustomerId}...");

        await _orderService.CreateOrderAsync(
            DemoCustomerId,
            "demo-order-1001",
            "Pending",
            24.50m);
        await _orderService.CreateOrderAsync(
            DemoCustomerId,
            "demo-order-1002",
            "Processing",
            49.95m);
        await _orderService.CreateOrderAsync(
            DemoCustomerId,
            "demo-order-1003",
            "Pending",
            12.75m);

        Console.WriteLine("Orders before the status change:");
        await DisplayCustomerOrdersAsync(DemoCustomerId);

        await _orderService.UpdateOrderStatusAsync(
            DemoCustomerId,
            "demo-order-1002",
            "Shipped");

        Console.WriteLine("Updated demo-order-1002 to Shipped.");
        Console.WriteLine("Orders after the status change:");
        await DisplayCustomerOrdersAsync(DemoCustomerId);

        Console.WriteLine(
            $"These sample orders remain in the table. To view them later, choose option 3 and enter {DemoCustomerId}.");
    }

    private async Task DisplayCustomerOrdersAsync(string customerId)
    {
        var orders = await _orderService.GetOrdersByCustomerAsync(customerId);

        if (orders.Count == 0)
        {
            Console.WriteLine("No orders found for that customer.");
            return;
        }

        Console.WriteLine($"Found {orders.Count} order(s):");

        foreach (var order in orders)
        {
            DisplayOrder(order);
        }
    }

    private static (string CustomerId, string OrderId) ReadOrderKey()
    {
        var customerId = ReadRequiredValue("Customer ID: ");
        var orderId = ReadRequiredValue("Order ID: ");
        return (customerId, orderId);
    }

    private static string ReadRequiredValue(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.WriteLine("A value is required.");
        }
    }

    private static decimal ReadOrderTotal()
    {
        while (true)
        {
            var input = ReadRequiredValue("Total (for example, 29.99): ");

            if (decimal.TryParse(
                    input,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var total) && total >= 0)
            {
                return total;
            }

            Console.WriteLine(
                "Enter a non-negative amount using a period as the decimal separator.");
        }
    }

    private static void DisplayOrder(Dictionary<string, AttributeValue> order)
    {
        Console.WriteLine($"Customer: {order["CustomerId"].S}");
        Console.WriteLine($"Order ID: {order["OrderId"].S}");
        Console.WriteLine($"Status: {order["Status"].S}");
        Console.WriteLine($"Total: {order["Total"].N}");
        Console.WriteLine();
    }
}
