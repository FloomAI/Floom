
namespace Floom.Repository;

public class DatabaseEntity
{
    public string Id { get; set; } = string.Empty;
    public DateTime createdAt { get; set; }
        
    public Dictionary<string, object> createdBy { get; set; }

    public DatabaseEntity()
    {
        // Initialize with an empty dictionary to avoid null reference exceptions
        createdBy = new Dictionary<string, object>();
    }
        
    // Method to add a key-value pair to the createdBy dictionary
    public void AddCreatedByApiKey(object value)
    {
        var key = "apiKey";
        createdBy[key] = value;
    }
        
    public void AddCreatedByOwner(object value)
    {
        var key = "owner";
        createdBy[key] = value;
    }
}