using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FlociSnsDemo.Models;

namespace FlociSnsDemo.Services;

public sealed class SnsPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _topicArn;

    public SnsPublisher(IAmazonSimpleNotificationService snsClient, string topicArn)
    {
        _snsClient = snsClient;
        _topicArn = topicArn;
    }

    public async Task<string> PublishOrderAsync(
        OrderPlacedEvent orderEvent,
        CancellationToken cancellationToken = default)
    {
        var response = await _snsClient.PublishAsync(
            new PublishRequest
            {
                TopicArn = _topicArn,
                Message = JsonSerializer.Serialize(orderEvent)
            },
            cancellationToken
        );

        return response.MessageId ?? "(no message ID returned)";
    }
}
