using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace FlociDynamoDbDemo.Services;

internal sealed class OrderService
{
  private readonly IAmazonDynamoDB _client;
  private readonly string _tableName;

  public OrderService(IAmazonDynamoDB client, string tableName)
  {
    _client = client;
    _tableName = tableName;
  }

  public async Task CreateOrderAsync(
      string customerId,
      string orderId,
      string status,
      decimal total)
  {
    var request = new PutItemRequest
    {
      TableName = _tableName,
      Item = new Dictionary<string, AttributeValue>
      {
        ["CustomerId"] = new AttributeValue { S = customerId },
        ["OrderId"] = new AttributeValue { S = orderId },
        ["Status"] = new AttributeValue { S = status },
        ["Total"] = new AttributeValue
        {
          N = total.ToString(CultureInfo.InvariantCulture)
        }
      }
    };

    await _client.PutItemAsync(request);
  }

  public async Task<Dictionary<string, AttributeValue>?> GetOrderAsync(
    string customerId,
    string orderId)
  {
    var request = new GetItemRequest
    {
      TableName = _tableName,
      Key = new Dictionary<string, AttributeValue>
      {
        ["CustomerId"] = new AttributeValue { S = customerId },
        ["OrderId"] = new AttributeValue { S = orderId }
      }
    };

    var response = await _client.GetItemAsync(request);

    if (response.Item is null || response.Item.Count == 0)
    {
      return null;
    }

    return response.Item;
  }

  public async Task<List<Dictionary<string, AttributeValue>>> GetOrdersByCustomerAsync(
    string customerId)
  {
    var request = new QueryRequest
    {
      TableName = _tableName,
      KeyConditionExpression = "CustomerId = :customerId",
      ExpressionAttributeValues = new Dictionary<string, AttributeValue>
      {
        [":customerId"] = new AttributeValue { S = customerId }
      }
    };

    var response = await _client.QueryAsync(request);

    return response.Items ?? [];
  }

  public async Task UpdateOrderStatusAsync(
    string customerId,
    string orderId,
    string status)
  {
    var request = new UpdateItemRequest
    {
      TableName = _tableName,
      Key = new Dictionary<string, AttributeValue>
      {
        ["CustomerId"] = new AttributeValue { S = customerId },
        ["OrderId"] = new AttributeValue { S = orderId }
      },
      UpdateExpression = "SET #status = :newStatus",
      ExpressionAttributeNames = new Dictionary<string, string>
      {
        ["#status"] = "Status"
      },
      ExpressionAttributeValues = new Dictionary<string, AttributeValue>
      {
        [":newStatus"] = new AttributeValue { S = status }
      }
    };

    await _client.UpdateItemAsync(request);
  }
  public async Task DeleteOrderAsync(string customerId, string orderId)
  {
    var request = new DeleteItemRequest
    {
      TableName = _tableName,
      Key = new Dictionary<string, AttributeValue>
      {
        ["CustomerId"] = new AttributeValue { S = customerId },
        ["OrderId"] = new AttributeValue { S = orderId }
      }
    };

    await _client.DeleteItemAsync(request);
  }

}
