using System.Linq.Expressions;
using Floom.Utils;

namespace Floom.Repository;

public interface IRepository<T> where T : DatabaseEntity
{
    Task Insert(T entity);
    Task UpsertEntity(T entity, string uid, string uniqueKey);
    Task Delete(string id, string uniqueKey);
    Task<T?> Get(string id, string uniqueKey);
    Task<IEnumerable<T>> GetAll();
    Task<IEnumerable<T>> GetAll(string id, string uniqueKey);
    Task<T?> FindByCondition(Expression<Func<T, bool>> condition);
    Task<T?> FindByAttributesAsync(Dictionary<string, object> attributes);
    Task<IEnumerable<T>> ListByConditionAsync(Expression<Func<T, bool>> condition);
}

public class Repository<T> : IRepository<T> where T : DatabaseEntity
{
    private readonly IDatabase<T> _database;

    public Repository(IDatabase<T> database)
    {
        _database = database;
    }

    public Task Insert(T entity)
    {
        entity.createdAt = DateTime.UtcNow;
        entity.AddCreatedByApiKey(HttpContextHelper.GetApiKeyFromHttpContext());
        return _database.Create(entity);
    }
    
    public async Task UpsertEntity(T databaseEntity, string uid, string column)
    {
        await _database.Upsert(databaseEntity, uid, column);
    }

    public async Task Delete(string name, string uniqueKey)
    {
        await _database.Delete(name, uniqueKey);
    }

    public async Task<T?> Get(string value, string key)
    {
        return await _database.Read(value, key);
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        return await _database.ReadAll();
    }
    
    public async Task<IEnumerable<T>> GetAll(string id, string uniqueKey)
    {
        return await _database.ReadAll(id, uniqueKey);
    }
    
    public async Task<T?> FindByCondition(Expression<Func<T, bool>> condition)
    {
        return await _database.ReadByCondition(condition);
    }   

    public async Task<T?> FindByAttributesAsync(Dictionary<string, object> attributes)
    {
        return await _database.ReadByAttributes(attributes);
    }

    public async Task<IEnumerable<T>> ListByConditionAsync(Expression<Func<T, bool>> condition)
    {
        return await _database.ReadAllByCondition(condition);
    }
}