using Amazon.DynamoDBv2.Model;

namespace Floom.Repository.DynamoDB;

public class DynamoDbMapper
{
    public static T ConvertToEntity<T>(Dictionary<string, AttributeValue> item) where T : new()
    {
        T entity = new T();
        foreach (var prop in typeof(T).GetProperties())
        {
            if (item.ContainsKey(prop.Name) && prop.CanWrite)
            {
                AttributeValue value = item[prop.Name];
                prop.SetValue(entity, ConvertAttributeValue(value, prop.PropertyType));
            }
        }
        return entity;
    }

    private static object ConvertAttributeValue(AttributeValue value, Type targetType)
    {
        // Handle null case
        if (value == null) return null;

        // Convert based on targetType
        if (targetType == typeof(string))
        {
            return value.S;
        }
        else if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return value.NULL ? default(int?) : int.Parse(value.N);
        }
        else if (targetType == typeof(long) || targetType == typeof(long?))
        {
            return value.NULL ? default(long?) : long.Parse(value.N);
        }
        else if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return value.NULL ? default(bool?) : value.BOOL;
        }
        else if (typeof(IEnumerable<string>).IsAssignableFrom(targetType) && value.L != null)
        {
            return value.L.Select(av => av.S).ToList();
        }
        else if (typeof(IDictionary<string, string>).IsAssignableFrom(targetType) && value.M != null)
        {
            return value.M.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.S);
        }
        else
        {
            throw new NotSupportedException($"Type {targetType.Name} is not supported");
        }
    }
}