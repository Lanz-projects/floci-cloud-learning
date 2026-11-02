using Amazon.SQS;
using Amazon.SQS.Model;

public class SqsMessageService
{
    private readonly IAmazonSQS _client;

    public SqsMessageService(IAmazonSQS client)
    {
      _client = client;
    }

    public async Task<SendMessageResponse> SendMessageAsync(
      string queueUrl,
      string messageBody)
    {
      var request = new SendMessageRequest
      {
        QueueUrl = queueUrl,
        MessageBody = messageBody
      };
      return await _client.SendMessageAsync(request);
    }

    public async Task<List<Message>> ReceiveMessagesAsync(string queueUrl)
    {
      var request = new ReceiveMessageRequest
      {
        QueueUrl = queueUrl,
        MaxNumberOfMessages = 1,
        WaitTimeSeconds = 2
      };
      var response = await _client.ReceiveMessageAsync(request);
      return response.Messages ?? new List<Message>();
    }

    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle)
    {
      var request = new DeleteMessageRequest
      {
        QueueUrl = queueUrl,
        ReceiptHandle = receiptHandle
      };
      await _client.DeleteMessageAsync(request);
    }
}