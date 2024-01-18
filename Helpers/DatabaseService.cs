using Azure;
using Floom.Models;
using MongoDB.Driver;

public interface IDatabaseService
{
    IMongoDatabase Database { get; }
    IMongoCollection<Pipeline> Pipelines { get; }
    IMongoCollection<Data> Data { get; }
    IMongoCollection<Model> Models { get; }
    IMongoCollection<VectorStore> VectorStores { get; }
    IMongoCollection<Embeddings> Embeddings { get; }
    IMongoCollection<Prompt> Prompts { get; }
    IMongoCollection<Floom.Models.Response> Responses { get; }
    IMongoCollection<Floom.Models.File> Files { get; }
    IMongoCollection<AuditRow> Audit { get; }
    IMongoCollection<LogRow> Log { get; }
    IMongoCollection<ApiKey> ApiKeys { get; }
}

public class DatabaseService : IDatabaseService
{
    public DatabaseService(IMongoClient mongoClient, string databaseName)
    {
        Database = mongoClient.GetDatabase(databaseName);
        Pipelines = Database.GetCollection<Pipeline>("pipelines");
        Data = Database.GetCollection<Data>("data");
        Models = Database.GetCollection<Model>("models");
        VectorStores = Database.GetCollection<VectorStore>("vector-stores");
        Embeddings = Database.GetCollection<Embeddings>("embeddings");
        Prompts = Database.GetCollection<Prompt>("prompts");
        Responses = Database.GetCollection<Floom.Models.Response>("responses");
        Files = Database.GetCollection<Floom.Models.File>("files");
        Audit = Database.GetCollection<AuditRow>("audit");
        Log = Database.GetCollection<LogRow>("log");
        ApiKeys = Database.GetCollection<ApiKey>("api-keys");
    }

    public IMongoDatabase Database { get; }
    public IMongoCollection<Pipeline> Pipelines { get; }
    public IMongoCollection<Data> Data { get; }
    public IMongoCollection<Model> Models { get; }
    public IMongoCollection<VectorStore> VectorStores { get; }
    public IMongoCollection<Embeddings> Embeddings { get; }
    public IMongoCollection<Prompt> Prompts { get; }
    public IMongoCollection<Floom.Models.Response> Responses { get; }
    public IMongoCollection<Floom.Models.File> Files { get; }
    public IMongoCollection<AuditRow> Audit { get; }
    public IMongoCollection<LogRow> Log { get; }
    public IMongoCollection<ApiKey> ApiKeys { get; }
}
