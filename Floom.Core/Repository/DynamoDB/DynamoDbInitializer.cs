using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Floom.Logs;

namespace Floom.Repository.DynamoDB;

public class DynamoDbInitializer
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly ILogger _logger;

    public DynamoDbInitializer(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    // Add your table creation methods here...

    public async Task Initialize()
    {
        if (await EnsureDynamoDBConnection())
        {
            _logger.LogInformation("Successfully connected to DynamoDB.");
        }
        else
        {
            _logger.LogError("Failed to establish a connection to DynamoDB after multiple attempts.");
        }
    }
    
    private async Task<bool> EnsureDynamoDBConnection()
    {
        int attempts = 5;
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            _logger.LogInformation($"Attempting to connect to DynamoDB, attempt {attempt} of {attempts}");
            
            if (await TestConnection())
            {
                return true;
            }

            if (attempt < attempts)
            {
                _logger.LogWarning("Failed to connect to DynamoDB, retrying in 15 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        return false;
    }

    private async Task<bool> TestConnection()
    {
        try
        {
            // Listing tables as a lightweight operation to test connectivity
            var response = await _dynamoDbClient.ListTablesAsync(new ListTablesRequest
            {
                Limit = 1
            });

            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while testing the DynamoDB connection.");
            return false;
        }
    }
}