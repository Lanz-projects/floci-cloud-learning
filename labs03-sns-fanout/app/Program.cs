using FlociSnsDemo;
using FlociSnsDemo.Models;
using FlociSnsDemo.Services;

var topicArn = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");
var billingQueueUrl = Environment.GetEnvironmentVariable("BILLING_QUEUE_URL");
var shippingQueueUrl = Environment.GetEnvironmentVariable("SHIPPING_QUEUE_URL");

if (string.IsNullOrWhiteSpace(topicArn) ||
    string.IsNullOrWhiteSpace(billingQueueUrl) ||
    string.IsNullOrWhiteSpace(shippingQueueUrl))
{
    Console.WriteLine("Set SNS_TOPIC_ARN, BILLING_QUEUE_URL, and SHIPPING_QUEUE_URL in PowerShell first.");
    return;
}

using var snsClient = FlociClientFactory.CreateSnsClient();
using var sqsClient = FlociClientFactory.CreateSqsClient();

var publisher = new SnsPublisher(snsClient, topicArn);
var consumer = new SqsMessageConsumer(sqsClient);

await RunMenuAsync(publisher, consumer, billingQueueUrl, shippingQueueUrl);

static async Task RunMenuAsync(
    SnsPublisher publisher,
    SqsMessageConsumer consumer,
    string billingQueueUrl,
    string shippingQueueUrl)
{
    while (true)
    {
        DisplayMenu();
        Console.Write("Choose an option: ");

        switch (Console.ReadLine())
        {
            case "1":
                await PublishOrderAsync(publisher);
                break;
            case "2":
                await consumer.ReceiveAndDeleteOneAsync(billingQueueUrl, "Billing");
                break;
            case "3":
                await consumer.ReceiveAndDeleteOneAsync(shippingQueueUrl, "Shipping");
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Choose 1, 2, 3, or 0.");
                break;
        }
    }
}

static void DisplayMenu()
{
    Console.WriteLine();
    Console.WriteLine("=== Floci SNS/SQS Demo ===");
    Console.WriteLine("1. Publish an order event");
    Console.WriteLine("2. Receive one message from Billing");
    Console.WriteLine("3. Receive one message from Shipping");
    Console.WriteLine("0. Exit");
}

static async Task PublishOrderAsync(SnsPublisher publisher)
{
    Console.Write("Enter an order ID: ");

    if (!int.TryParse(Console.ReadLine(), out var orderId) || orderId <= 0)
    {
        Console.WriteLine("Enter a positive whole-number order ID.");
        return;
    }

    var orderEvent = new OrderPlacedEvent(
        EventType: "OrderPlaced",
        OrderId: orderId,
        CustomerId: "customer-123",
        Total: 49.95m,
        OccurredAtUtc: DateTime.UtcNow
    );

    var messageId = await publisher.PublishOrderAsync(orderEvent);
    Console.WriteLine($"Published order event. SNS message ID: {messageId}");
}
