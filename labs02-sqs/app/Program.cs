using Amazon.Runtime;
using Amazon.SQS;

var credentials = new BasicAWSCredentials("test", "test");

var config = new AmazonSQSConfig
{
    ServiceURL = "http://localhost:4566",
    AuthenticationRegion = "us-east-1"
};

using var sqsClient = new AmazonSQSClient(credentials, config);

var configuredQueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");

if (string.IsNullOrWhiteSpace(configuredQueueUrl))
{
    Console.WriteLine("SQS_QUEUE_URL is not set.");
    return;
}

var queueUrl = configuredQueueUrl;
var sqsService = new SqsMessageService(sqsClient);
var isRunning = true;
string? lastReceiptHandle = null;

while (isRunning)
{
    ShowMenu();
    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1":
                await SendMessageFromPromptAsync();
                break;
            case "2":
                await ReceiveMessageAsync();
                break;
            case "3":
                await DeleteReceivedMessageAsync();
                break;
            case "0":
                ExitProgram();
                break;
            default:
                ShowInvalidOption();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Operation failed: {ex.Message}");
    }
}

void ShowMenu()
{
    Console.WriteLine();
    Console.WriteLine("Floci SQS Demo");
    Console.WriteLine("1. Send a message");
    Console.WriteLine("2. Receive a message");
    Console.WriteLine("3. Delete the received message");
    Console.WriteLine("0. Exit");
    Console.Write("Choose an option: ");
}

async Task SendMessageFromPromptAsync()
{
    Console.Write("Message body: ");
    var messageBody = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(messageBody))
    {
        Console.WriteLine("Message body cannot be empty.");
        return;
    }

    var response = await sqsService.SendMessageAsync(queueUrl, messageBody);
    Console.WriteLine($"Message sent. Message ID: {response.MessageId}");
}

async Task ReceiveMessageAsync()
{
    if (!string.IsNullOrWhiteSpace(lastReceiptHandle))
    {
        Console.WriteLine("Delete the message you already received before receiving another.");
        return;
    }

    var messages = await sqsService.ReceiveMessagesAsync(queueUrl);

    if (messages.Count == 0)
    {
        Console.WriteLine("No messages are currently available.");
        return;
    }

    var message = messages[0];
    if (string.IsNullOrWhiteSpace(message.ReceiptHandle))
    {
        Console.WriteLine("The received message did not include a receipt handle.");
        return;
    }

    lastReceiptHandle = message.ReceiptHandle;

    Console.WriteLine($"Received message ID: {message.MessageId}");
    Console.WriteLine($"Body: {message.Body}");
    Console.WriteLine("The message is temporarily hidden until you delete it or its visibility timeout expires.");
}

async Task DeleteReceivedMessageAsync()
{
    var receiptHandle = lastReceiptHandle;
    if (string.IsNullOrWhiteSpace(receiptHandle))
    {
        Console.WriteLine("Receive a message before choosing delete.");
        return;
    }

    Console.Write("Delete the received message? (y/N): ");
    var confirmation = Console.ReadLine()?.Trim();

    if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Delete cancelled.");
        return;
    }

    await sqsService.DeleteMessageAsync(queueUrl, receiptHandle);
    lastReceiptHandle = null;
    Console.WriteLine("Message deleted from the queue.");
}

void ExitProgram()
{
    isRunning = false;
}

void ShowInvalidOption()
{
    Console.WriteLine("Please choose one of the listed options.");
}
