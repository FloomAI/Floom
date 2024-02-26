using System.Reflection;
using Floom.Base;
using Floom.Config;
using MongoDB.Driver;

namespace Floom.Repository;

public interface IRepositoryFactory
{
    IRepository<T> Create<T>(string collectionName= null) where T : DatabaseEntity, new();
}

public class RepositoryFactory : IRepositoryFactory
{
    public IRepository<T> Create<T>(string collectionName = null) where T : DatabaseEntity, new()
    {
        // If collectionName is not provided, try to get it from the CollectionNameAttribute
        if (string.IsNullOrEmpty(collectionName))
        {
            var attribute = typeof(T).GetCustomAttribute<CollectionNameAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException($"The collection name for {typeof(T).Name} is not specified.");
            }
            collectionName = attribute.Name;
        }
        
        var mongoDatabase = new MongoDatabase<T>(MongoConfiguration.CreateMongoClient(), collectionName);
        return new Repository<T>(mongoDatabase);
        

        /*
         if(databaseType.Equals("dynamodb"))
        {
            var dynamoDbClient = DynamoDbConfiguration.CreateCloudDynamoDbClient();
            var dynamoDbDatabase = new DynamoDbDatabase<T>(dynamoDbClient, collectionName);
            return new Repository<T>(dynamoDbDatabase);
        }*/

        return null;
    }
}