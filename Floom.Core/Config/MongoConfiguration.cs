using System.Security.Authentication;
using MongoDB.Driver;

namespace Floom.Config;

public static class MongoConfiguration
{
    public static MongoClient CreateMongoClient()
    {
        var template = "mongodb://{0}:{1}@{2}";
        var username = Environment.GetEnvironmentVariable("FLOOM_DB_USER");
        var password = Environment.GetEnvironmentVariable("FLOOM_DB_PASSWORD");
        var clusterEndpoint = Environment.GetEnvironmentVariable("FLOOM_DB_ADDRESS");
        var connectionString = string.Format(template, username, password, clusterEndpoint);
        var floomEnvironment = Environment.GetEnvironmentVariable("FLOOM_ENVIRONMENT");
        MongoClientSettings? settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
        var client = new MongoClient(settings);
        return client;
    }
}