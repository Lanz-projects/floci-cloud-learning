using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace FlociOrdersFunction;

public sealed class OrderService
{
  private readonly IAmazonDynamoDB _client;
  private readonly string _tableName;

  public OrderService(IAmazonDynamoDB client, string tableName)
  {
    _client = client;
    _tableName = tableName;
  }

  public async Task<string> CreateAsync(OrderRequest order)
  {
    if (string.IsNullOrWhiteSpace(order.CustomerId) ||
        string.IsNullOrWhiteSpace(order.OrderId) ||
        string.IsNullOrWhiteSpace(order.Status))
    {
      throw new ArgumentException("CustomerId, OrderId, and Status are required.");
    }

    if (order.Total < 0)
    {
      throw new ArgumentException("Total cannot be negative.");
    }

    await _client.PutItemAsync(new PutItemRequest
    {
      TableName = _tableName,
      Item = new Dictionary<string, AttributeValue>
      {
        ["CustomerId"] = new AttributeValue { S = order.CustomerId },
        ["OrderId"] = new AttributeValue { S = order.OrderId },
        ["Status"] = new AttributeValue { S = order.Status },
        ["Total"] = new AttributeValue { N = order.Total.ToString(CultureInfo.InvariantCulture) }
      }
    });

    return $"Saved order {order.OrderId}.";
  }

  public async Task<string?> GetAsync(string customerId, string orderId)
  {
    if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(orderId))
    {
      throw new ArgumentException("CustomerId and OrderId are required for get.");
    }

    var response = await _client.GetItemAsync(new GetItemRequest
    {
      TableName = _tableName,
      Key = new Dictionary<string, AttributeValue>
      {
        ["CustomerId"] = new AttributeValue { S = customerId },
        ["OrderId"] = new AttributeValue { S = orderId }
      }
    });

    if (response.Item is null || response.Item.Count == 0)
    {
      return null;
    }

    return $"Order {orderId}: {response.Item["Status"].S}, total {response.Item["Total"].N}.";
  }
}
