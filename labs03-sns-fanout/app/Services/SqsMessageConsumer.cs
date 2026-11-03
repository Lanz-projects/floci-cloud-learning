using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace FlociSnsDemo.Services;

public sealed class SqsMessageConsumer
{
    private readonly IAmazonSQS _sqsClient;

    public SqsMessageConsumer(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
    }

    public async Task ReceiveAndDeleteOneAsync(
        string queueUrl,
        string queueName,
        CancellationToken cancellationToken = default)
    {
        var response = await _sqsClient.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 2
            },
            cancellationToken
        );

        if (response.Messages is null || response.Messages.Count == 0)
        {
            Console.WriteLine($"No message available in the {queueName} queue.");
            return;
        }

        var receivedMessage = response.Messages[0];
        var messageBody = ExtractSnsMessage(receivedMessage.Body);

        Console.WriteLine($"{queueName} received:");
        Console.WriteLine(messageBody);

        await _sqsClient.DeleteMessageAsync(
            queueUrl,
            receivedMessage.ReceiptHandle,
            cancellationToken
        );

        Console.WriteLine($"Deleted the message from the {queueName} queue.");
    }

    private static string ExtractSnsMessage(string body)
    {
        try
        {
            using var notification = JsonDocument.Parse(body);

            if (notification.RootElement.TryGetProperty("Message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // A direct SQS message may not have an SNS notification envelope.
        }

        return body;
    }
}
