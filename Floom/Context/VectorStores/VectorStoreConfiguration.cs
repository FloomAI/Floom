namespace Floom.Context.VectorStores;

public class VectorStoreConfiguration
{
    public string? Vendor;
    public string? Endpoint;
    public string? Port;
    public string? Username;
    public string? Password;
    public string? ApiKey;
    public string? Environment;
    
    public VectorStoreConfiguration()
    {
        
    }
    
    public VectorStoreConfiguration(object configuration)
    {
        if (configuration is not IDictionary<object, object> dict) return;
        Vendor = dict.TryGetValue("vendor", out var vendor) ? vendor as string : string.Empty;
        Endpoint = dict.TryGetValue("endpoint", out var endpoint) ? endpoint as string : string.Empty;
        Port = dict.TryGetValue("port", out var port) ? port as string : string.Empty;
        Username = dict.TryGetValue("username", out var username) ? username as string : string.Empty;
        Password = dict.TryGetValue("password", out var password) ? password as string : string.Empty;
        ApiKey = dict.TryGetValue("apikey", out var apiKey) ? apiKey as string : string.Empty;
        Environment = dict.TryGetValue("environment", out var environment) ? environment as string : string.Empty;
    }
    
    public static VectorStoreConfiguration? GetEnvVarVectorStoreConfiguration()
    {
        var vectorStoreConfiguration = new VectorStoreConfiguration
        {
            Vendor = System.Environment.GetEnvironmentVariable("VDB_VENDOR") ?? string.Empty,
            Endpoint = System.Environment.GetEnvironmentVariable("VDB_ENDPOINT") ?? string.Empty,
            Port = System.Environment.GetEnvironmentVariable("VDB_PORT") ?? string.Empty,
            Username = System.Environment.GetEnvironmentVariable("VDB_USERNAME") ?? string.Empty,
            Password = System.Environment.GetEnvironmentVariable("VDB_PASSWORD") ?? string.Empty,
            ApiKey = System.Environment.GetEnvironmentVariable("VDB_APIKEY") ?? string.Empty,
            Environment = System.Environment.GetEnvironmentVariable("VDB_ENVIRONMENT") ?? string.Empty,
        };
        
        // VDB_VENDOR Env Var is required to use a Vector Store
        if (vectorStoreConfiguration.Vendor == string.Empty)
            return null;
        
        return vectorStoreConfiguration;
    }
}