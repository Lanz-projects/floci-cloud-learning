using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace FlociSnsDemo;

public static class FlociClientFactory
{
    private const string Endpoint = "http://localhost:4566";
    private const string Region = "us-east-1";

    public static AmazonSimpleNotificationServiceClient CreateSnsClient()
    {
        var config = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = Endpoint,
            AuthenticationRegion = Region
        };

        return new AmazonSimpleNotificationServiceClient(
            CreateLocalCredentials(),
            config
        );
    }

    public static AmazonSQSClient CreateSqsClient()
    {
        var config = new AmazonSQSConfig
        {
            ServiceURL = Endpoint,
            AuthenticationRegion = Region
        };

        return new AmazonSQSClient(CreateLocalCredentials(), config);
    }

    private static BasicAWSCredentials CreateLocalCredentials()
    {
        return new BasicAWSCredentials("test", "test");
    }
}
