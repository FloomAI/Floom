using System.Reflection;
using Floom.Entities;
using Floom.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Floom.Repository;

public interface IRepository<T> where T : DatabaseEntity
{
    Task Insert(T entity);
    Task Update(T entity);
    Task UpsertDto(MainDto dto);
    Task UpsertEntity(T DatabaseEntity, string uid);
    Task DeleteByName(string id);
    Task<T?> Get(string id, string uniqueKey = "id");
    Task<IEnumerable<T>> GetAll();
    Task<IEnumerable<T>> GetAll(string id, string uniqueKey = "id");
}

public class Repository<T> : IRepository<T> where T : DatabaseEntity
{
    private readonly IMongoClient _client;
    protected IMongoCollection<T> _collection;
    private readonly ILogger _logger;

    public Repository(IMongoClient mongoClient, string collectionName, ILogger logger)
    {
        var database = mongoClient.GetDatabase("Floom");
        _collection = database.GetCollection<T>(collectionName);
        _logger = logger;
    }

    public Task Insert(T entity)
    {
        entity.createdAt = DateTime.UtcNow;
        entity.createdBy = HttpContextHelper.GetApiKeyFromHttpContext() ?? "";
        return _collection.InsertOneAsync(entity);
    }

    public async Task Update(T entity)
    {
        var filter = Builders<T>.Filter.Eq("_id", ((dynamic)entity).Id);
        await _collection.ReplaceOneAsync(filter, entity);
    }

    public async Task UpsertDto(MainDto dto)
    {
        var updateDefinition = Builders<T>.Update;
        var updates = new List<UpdateDefinition<T>>();

        var dtoDictionary = dto.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(dto));

        var propertiesToSkip = new List<string> { "id", "kind" };

        foreach (var property in dtoDictionary)
        {
            if (propertiesToSkip.Contains(property.Key))
            {
                continue;
            }

            if (property.Value != null)
            {
                updates.Add(updateDefinition.Set(property.Key, property.Value));
            }
        }

        var filter = Builders<T>.Filter.Eq("name", dto.id);

        await _collection.UpdateOneAsync(filter, updateDefinition.Combine(updates));
    }

    public async Task UpsertEntity(T databaseEntity, string uid)
    {
        var filter = Builders<T>.Filter.Eq("package", uid);
        
        var existingItem = await _collection.Find(filter).FirstOrDefaultAsync();

        if (existingItem != null)
        {
            databaseEntity.Id = existingItem.Id;
            var updateFilter = Builders<T>.Filter.Eq("_id", ((dynamic)existingItem).Id);
            await _collection.ReplaceOneAsync(updateFilter, databaseEntity);
        }
        else
        {
            await Insert(databaseEntity);
        }
    }

    public async Task DeleteByName(string name)
    {
        var filter = Builders<T>.Filter.Eq("name", name);

        await _collection.DeleteOneAsync(filter);
    }

    public async Task<T?> Get(string id, string uniqueKey = "_id")
    {
        FilterDefinition<T> filter;
        if (uniqueKey.Equals("_id"))
        {
            filter = Builders<T>.Filter.Eq(uniqueKey, new ObjectId(id));
        }
        else
        {
            filter = Builders<T>.Filter.Eq(uniqueKey, id);
        }

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }
    
    public async Task<IEnumerable<T>> GetAll(string id, string uniqueKey = "id")
    {
        FilterDefinition<T> filter;
        if (uniqueKey.Equals("_id"))
        {
            filter = Builders<T>.Filter.Eq(uniqueKey, new ObjectId(id));
        }
        else
        {
            filter = Builders<T>.Filter.Eq(uniqueKey, id);
        }

        return await _collection.Find(filter).ToListAsync();
    }
}