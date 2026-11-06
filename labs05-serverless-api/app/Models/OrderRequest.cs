using System.Text.Json.Serialization;

namespace FlociOrdersFunction;

public sealed class OrderRequest
{
  [JsonPropertyName("customerId")]
  public string CustomerId { get; init; } = "";

  [JsonPropertyName("orderId")]
  public string OrderId { get; init; } = "";

  [JsonPropertyName("status")]
  public string Status { get; init; } = "";

  [JsonPropertyName("total")]
  public decimal Total { get; init; }
}
