using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace FlociDynamoDbDemo;

internal static class FlociDynamoDbClientFactory
{
  public static IAmazonDynamoDB CreateClient()
  {
    var credentials = new BasicAWSCredentials("test", "test");

    var config = new AmazonDynamoDBConfig
    {
      ServiceURL = "http://localhost:4566",
      AuthenticationRegion = "us-east-1"
    };

    return new AmazonDynamoDBClient(credentials, config);
  }
}