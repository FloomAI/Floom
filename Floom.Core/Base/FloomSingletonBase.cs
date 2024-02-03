using Floom.Repository;

namespace Floom.Base;

public abstract class FloomSingletonBase<T> where T : FloomSingletonBase<T>, new()
{
    private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());
    
    public static T Instance => _instance.Value;

    protected FloomSingletonBase()
    {
        // Base constructor logic, if any
    }

    // Method to provide common initialization logic, if needed.
    // This can be abstract or virtual depending on your requirements.
    public abstract void Initialize(IRepositoryFactory repositoryFactory);
}
