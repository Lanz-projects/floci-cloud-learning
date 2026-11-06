using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FlociOrdersFunction;

public class Class1
{
  private static readonly IAmazonDynamoDB DynamoDbClient = new AmazonDynamoDBClient();

  public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(
      APIGatewayHttpApiV2ProxyRequest request,
      ILambdaContext context)
  {
    var tableName = Environment.GetEnvironmentVariable("ORDERS_TABLE_NAME");
    if (string.IsNullOrWhiteSpace(tableName))
    {
      throw new InvalidOperationException("ORDERS_TABLE_NAME is not set.");
    }

    var orders = new OrderService(DynamoDbClient, tableName);
    var method = request.RequestContext.Http.Method;

    if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(request.RawPath, "/orders", StringComparison.OrdinalIgnoreCase))
    {
      return await CreateOrderAsync(request, orders, context);
    }

    if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
        request.PathParameters is not null &&
        request.PathParameters.TryGetValue("customerId", out var customerId) &&
        request.PathParameters.TryGetValue("orderId", out var orderId))
    {
      return await GetOrderAsync(customerId, orderId, orders, context);
    }

    return CreateResponse(404, "Route not found.");
  }

  private static async Task<APIGatewayHttpApiV2ProxyResponse> CreateOrderAsync(
      APIGatewayHttpApiV2ProxyRequest request, OrderService orders, ILambdaContext context)
  {
    OrderRequest? order;
    try
    {
      order = JsonSerializer.Deserialize<OrderRequest>(request.Body ?? string.Empty);
    }
    catch (JsonException)
    {
      return CreateResponse(400, "The request body must contain valid JSON.");
    }

    if (order is null)
    {
      return CreateResponse(400, "A request body is required.");
    }

    try
    {
      var message = await orders.CreateAsync(order);
      context.Logger.LogLine(message);
      return CreateResponse(201, message);
    }
    catch (ArgumentException ex)
    {
      return CreateResponse(400, ex.Message);
    }
  }

  private static async Task<APIGatewayHttpApiV2ProxyResponse> GetOrderAsync(
      string customerId, string orderId, OrderService orders, ILambdaContext context)
  {
    var message = await orders.GetAsync(customerId, orderId);
    if (message is null)
    {
      return CreateResponse(404, $"Order {orderId} was not found.");
    }

    context.Logger.LogLine($"Read order {orderId}.");
    return CreateResponse(200, message);
  }

  private static APIGatewayHttpApiV2ProxyResponse CreateResponse(int statusCode, string message)
  {
    return new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = statusCode,
      Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
      Body = JsonSerializer.Serialize(new { message })
    };
  }
}
