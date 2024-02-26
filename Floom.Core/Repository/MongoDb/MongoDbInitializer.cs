using Floom.Logs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Floom.Repository.MongoDb;

public class MongoDbInitializer
{
    private readonly IMongoClient _client;
    private readonly ILogger _logger;
        
    public MongoDbInitializer(IMongoClient client)
    {
        _client = client;
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    public async Task Initialize(string database)
    {
        if (await EnsureMongoDBConnection())
        {
            var dbList = _client.ListDatabases().ToList().Select(db => db["name"].AsString);
            if (!dbList.Contains(database))
            {
                _logger.LogWarning("Database {Database} does not exist. Creating it...", database);
                var db = _client.GetDatabase(database);
                var collection = db.GetCollection<dynamic>("DummyCollection");

                collection.InsertOne(new { DummyField = "DummyValue" });
                collection.DeleteOne(Builders<dynamic>.Filter.Eq("DummyField", "DummyValue"));
                db.DropCollection("DummyCollection");

                _logger.LogInformation("Database {Database} created.", database);
            }
            else
            {
                _logger.LogInformation("Database {Database} already exists.", database);
            }
        }
        else
        {
            _logger.LogError("Failed to establish a connection to MongoDB after multiple attempts.");
        }
    }

    private async Task<bool> EnsureMongoDBConnection()
    {
        int attempts = 5;
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            _logger.LogInformation("Attempting to connect to MongoDB, attempt {Attempt} of {Attempts}", attempt, attempts);
            if (await TestConnection())
            {
                _logger.LogInformation("Successfully connected to MongoDB.");
                return true;
            }

            if (attempt < attempts)
            {
                _logger.LogWarning("Failed to connect to MongoDB, retrying in 15 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        return false;
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            var database = _client.GetDatabase("admin");
            var command = new BsonDocument("ping", 1);
            await database.RunCommandAsync<BsonDocument>(command);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while testing the MongoDB connection.");
            return false;
        }
    }
}