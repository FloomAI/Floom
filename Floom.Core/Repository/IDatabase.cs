using System.Linq.Expressions;

namespace Floom.Repository;

public interface IDatabase<T> where T : DatabaseEntity
{
    Task Create(T entity);
    Task<T?> Read(string id, string uniqueKey);
    public Task<T?> ReadByCondition(Expression<Func<T, bool>> condition);
    public Task<T?> ReadByAttributes(Dictionary<string, object> attributes);
    Task<IEnumerable<T>> ReadAll();
    Task<IEnumerable<T>> ReadAll(string id, string uniqueKey);
    Task<IEnumerable<T>> ReadAllByCondition(Expression<Func<T, bool>> condition);
    Task Upsert(T entity, string uid, string column);
    Task Delete(string id, string uniqueKey);
}