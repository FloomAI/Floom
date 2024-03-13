namespace Floom.Plugins.Prompt.Context.VectorStores;

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
    
    public static VectorStoreConfiguration? GetEnvVarVectorStoreConfiguration()
    {
        var vectorStoreConfiguration = new VectorStoreConfiguration
        {
            Vendor = System.Environment.GetEnvironmentVariable("FLOOM_VDB_VENDOR") ?? string.Empty,
            Endpoint = System.Environment.GetEnvironmentVariable("FLOOM_VDB_ENDPOINT") ?? string.Empty,
            Port = System.Environment.GetEnvironmentVariable("FLOOM_VDB_PORT") ?? string.Empty,
            Username = System.Environment.GetEnvironmentVariable("FLOOM_VDB_USERNAME") ?? string.Empty,
            Password = System.Environment.GetEnvironmentVariable("FLOOM_VDB_PASSWORD") ?? string.Empty,
            ApiKey = System.Environment.GetEnvironmentVariable("FLOOM_VDB_APIKEY") ?? string.Empty,
            Environment = System.Environment.GetEnvironmentVariable("FLOOM_VDB_ENVIRONMENT") ?? string.Empty,
        };
        
        // VDB_VENDOR Env Var is required to use a Vector Store
        if (vectorStoreConfiguration.Vendor == string.Empty)
            return null;
        
        return vectorStoreConfiguration;
    }
}