using FlociDynamoDbDemo;
using FlociDynamoDbDemo.Menus;
using FlociDynamoDbDemo.Services;

internal static class Program
{
  private static async Task Main()
  {
    var tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME");

    if (string.IsNullOrWhiteSpace(tableName))
    {
      Console.Error.WriteLine("DYNAMODB_TABLE_NAME is not set.");
      return;
    }

    using var client = FlociDynamoDbClientFactory.CreateClient();
    var orderService = new OrderService(client, tableName);
    var orderMenu = new OrderMenu(orderService);

    await orderMenu.RunAsync();
  }
}
