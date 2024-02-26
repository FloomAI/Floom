using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace Floom.Config;

public abstract class DynamoDbConfiguration
{
    public static IAmazonDynamoDB CreateDynamoDbClient()
    {
        var config = new AmazonDynamoDBConfig
        {
            // Point to the local DynamoDB instance
            ServiceURL = "http://localhost:8000",
        };

        var credentials = new BasicAWSCredentials("dummy", "dummy");

        // Create a client with the local configuration
        return new AmazonDynamoDBClient(credentials, config);
    }

    public static IAmazonDynamoDB CreateCloudDynamoDbClient()
    {
        // Specify the AWS region of your DynamoDB instance
        var region = Amazon.RegionEndpoint.EUNorth1;
        return new AmazonDynamoDBClient(new EnvironmentVariablesAWSCredentials(), region);
    }

}