using System.Security.Authentication;
using MongoDB.Driver;

namespace Floom.Config;

public static class MongoConfiguration
{
    private static readonly string User = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
    private static readonly string Password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "MyFloom";
    private static readonly string Address = Environment.GetEnvironmentVariable("DB_ADDRESS") ?? "localhost:4060";
    
    public static string ConnectionString()
    {
        return $"mongodb://{User}:{Password}@{Address}";
    }

    public static string CloudConnectionString()
    {
        if(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") != null)
            return Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

        return null;
    }
    
    public static MongoClient CreateMongoClient()
    {
        var template = "mongodb://{0}:{1}@{2}/?tls=true&tlsCAFile=global-bundle.pem&replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false";
        var username = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var clusterEndpoint = Environment.GetEnvironmentVariable("DB_ADDRESS");
        var connectionString = String.Format(template, username, password, clusterEndpoint);
        var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12,
        };
        settings.UseTls = true;
        var client = new MongoClient(settings);
        return client;
    }
}