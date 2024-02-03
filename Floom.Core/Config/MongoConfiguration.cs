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
}