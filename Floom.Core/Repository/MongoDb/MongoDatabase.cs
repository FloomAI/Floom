using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Floom.Repository;

public class MongoDatabase<T> : IDatabase<T> where T : DatabaseEntity
{
    protected IMongoCollection<T> _collection;
    
    public MongoDatabase(IMongoClient mongoClient, string collectionName)
    {
        var database = mongoClient.GetDatabase("Floom");
        _collection = database.GetCollection<T>(collectionName);
    }
    
    public Task Create(T entity)
    {
        if(string.IsNullOrEmpty(entity.Id))
        {
            entity.Id = ObjectId.GenerateNewId().ToString();
        }
        return _collection.InsertOneAsync(entity);
    }

    public async Task<T?> Read(string value, string uniqueKey = "_id")
    {
        var filter = Builders<T>.Filter.Eq(uniqueKey, value);
        
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
    
    public async Task<T?> ReadByCondition(Expression<Func<T, bool>> condition)
    {
        return await _collection.Find(condition).FirstOrDefaultAsync();
    }

    public async Task<T?> ReadByAttributes(Dictionary<string, object> attributes)
    {
        var filters = new List<FilterDefinition<T>>();

        foreach (var attribute in attributes)
        {
            var filter = Builders<T>.Filter.Eq(attribute.Key, attribute.Value);
            filters.Add(filter);
        }

        var combinedFilter = Builders<T>.Filter.And(filters);

        return await _collection.Find(combinedFilter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> ReadAll()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<T>> ReadAll(string id, string uniqueKey = "_id")
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
    
    public async Task Upsert(T entity, string uid, string column)
    {
        var filter = Builders<T>.Filter.Eq(column, uid);
        
        var existingItem = await _collection.Find(filter).FirstOrDefaultAsync();

        if (existingItem != null)
        {
            entity.Id = existingItem.Id;
            var updateFilter = Builders<T>.Filter.Eq("_id", ((dynamic)existingItem).Id);
            await _collection.ReplaceOneAsync(updateFilter, entity);
        }
        else
        {
            await Create(entity);
        }
    }

    public async Task Delete(string value, string uniqueKey = "name")
    {
        var filter = Builders<T>.Filter.Eq(uniqueKey, value);

        await _collection.DeleteOneAsync(filter);
    }
}