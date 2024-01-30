using MongoDB.Driver;

namespace Floom.Repository;

public interface IRepositoryFactory
{
    IRepository<T> Create<T>(string collectionName) where T : DatabaseEntity;
}

public class RepositoryFactory : IRepositoryFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private IMongoClient _mongoClient;

    public RepositoryFactory(IMongoClient mongoClient,
        ILoggerFactory loggerFactory)
    {
        _mongoClient = mongoClient;
        _loggerFactory = loggerFactory;
    }

    public IRepository<T> Create<T>(string collectionName) where T : DatabaseEntity
    {
        var logger = _loggerFactory.CreateLogger($"Repository.{typeof(T).Name}");

        return new Repository<T>(_mongoClient, collectionName, logger);
    }
}