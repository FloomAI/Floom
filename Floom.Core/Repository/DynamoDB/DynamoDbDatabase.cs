using System.Linq.Expressions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Floom.Utils;

namespace Floom.Repository.DynamoDB;

public class DynamoDbDatabase<T> : IDatabase<T> where T : DatabaseEntity, new()
{
    private readonly DynamoDBContext _context;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    
    public DynamoDbDatabase(IAmazonDynamoDB client, string tableName)
    {
        _dynamoDbClient = client;
        var contextConfig = new DynamoDBContextConfig
        {
            IgnoreNullValues = true
        };
        _context = new DynamoDBContext(client, contextConfig);
        _tableName = tableName;
    }
        
    public async Task Create(T entity)
    {
        // Check if the entity's ID is not set or empty, then assign a new GUID
        if (string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = Guid.NewGuid().ToString();
            entity.createdAt = DateTime.UtcNow;
        }
        entity.AddCreatedByApiKey(HttpContextHelper.GetApiKeyFromHttpContext());

        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };
        
        await _context.SaveAsync(entity, config);
    }

    public async Task<T?> Read(string value, string property = "Id")
    {
        if (property == "Id")
        {
            var request = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = value } } // Assuming the Id is of type String
                }
            };

            var response = await _dynamoDbClient.GetItemAsync(request);
            if (response.Item == null || !response.IsItemSet)
            {
                return null;
            }

            // Assuming you have a method to convert a Dictionary<string, AttributeValue> to your entity type T
            return DynamoDbMapper.ConvertToEntity<T>(response.Item);
        }
        else
        {
            // Query using GSI
            var indexName = $"{property}-index";
            var queryRequest = new QueryRequest
            {
                TableName = _tableName,
                IndexName = indexName,
                KeyConditionExpression = $"{property} = :v",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":v", new AttributeValue { S = value } }
                }
            };

            var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);
            if (queryResponse.Items.Count == 0)
            {
                return null;
            }

            // Assuming you have a method to convert a Dictionary<string, AttributeValue> to your entity type T
            return DynamoDbMapper.ConvertToEntity<T>(queryResponse.Items[0]);
        }
    }

    
    public async Task<T?> ReadByCondition(Expression<Func<T, bool>> condition)
    {
        var search = _context.ScanAsync<T>(new List<ScanCondition>());
        var allItems = await search.GetRemainingAsync();
        return allItems.AsQueryable().Where(condition).FirstOrDefault();
    }

    public Task<T?> ReadByAttributes(Dictionary<string, object> attributes)
    {
        var request = new QueryRequest
        {
            TableName = "Assets",
            IndexName = "ChecksumIndex", // Specify the GSI name
            KeyConditionExpression = "Checksum = :v_Checksum",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                // {":v_Checksum", new AttributeValue { S = checksum }}
            },
            ProjectionExpression = "Id" // Retrieve only the 'Id' attribute
        };

        var response =  _context.QueryAsync<T>(request);
        return Task.FromResult(response.GetRemainingAsync().Result.FirstOrDefault());
    }

    public async Task<IEnumerable<T>> ReadAll()
    {
        var search = _context.ScanAsync<T>(new List<ScanCondition>());
        return await search.GetRemainingAsync();
    }

    public async Task<IEnumerable<T>> ReadAll(string id, string uniqueKey = "Id")
    {
        // This method is not directly supported as DynamoDB does not support fetching all based on a non-primary key without a full scan
        return uniqueKey == "Id" ? new List<T> { await _context.LoadAsync<T>(id) } : await ReadAllByCondition(item => typeof(T).GetProperty(uniqueKey).GetValue(item, null).ToString() == id);
    }

    private async Task<IEnumerable<T>> ReadAllByCondition(Expression<Func<T, bool>> condition)
    {
        var search = _context.ScanAsync<T>(new List<ScanCondition>());
        var allItems = await search.GetRemainingAsync();
        return allItems.AsQueryable().Where(condition).ToList();
    }

    public async Task Upsert(T entity, string uid, string column)
    {
        // DynamoDB SaveAsync acts as upsert
        await Create(entity);
    }

    public async Task Delete(string id, string uniqueKey = "Id")
    {
        if (uniqueKey == "Id")
        {
            await _context.DeleteAsync<T>(id);
        }
        else
        {
            // Deleting by a condition other than the Id requires fetching the item first then deleting it by Id
            var itemToDelete = await ReadByCondition(item => typeof(T).GetProperty(uniqueKey).GetValue(item, null).ToString() == id);
            if (itemToDelete != null)
            {
                await _context.DeleteAsync(itemToDelete);
            }
        }
    }
}