using MongoDB.Bson;

namespace Floom.Utils;

public static class MongoUtils
{
    public static BsonDocument  ConvertToBsonDocument(Dictionary<string, object> configuration)
    {
        var bsonDocument = new BsonDocument();
        foreach (var kvp in configuration)
        {
            BsonValue bsonValue;

            // Check the type of the value and convert accordingly
            switch (kvp.Value)
            {
                case string stringValue:
                    bsonValue = new BsonString(stringValue);
                    break;
                case IEnumerable<string> stringEnumerable:
                    bsonValue = new BsonArray(stringEnumerable.Select(s => new BsonString(s)));
                    break;
                case Dictionary<string, object> subDict:
                    bsonValue = ConvertToBsonDocument(subDict); // Recursive call for nested dictionaries
                    break;
                // Add more cases as needed for other types
                default:
                    bsonValue = BsonValue.Create(kvp.Value); // Fallback for simple types or null values
                    break;
            }

            bsonDocument.Add(kvp.Key, bsonValue);
        }

        return bsonDocument;
    }

    public static Dictionary<string, object> ConvertBsonDocumentToDictionary(BsonDocument bsonDocument)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var element in bsonDocument)
        {
            object value = ConvertBsonValue(element.Value);
            dictionary.Add(element.Name, value);
        }

        return dictionary;
    }

    private static  object ConvertBsonValue(BsonValue bsonValue)
    {
        switch (bsonValue.BsonType)
        {
            case BsonType.Document:
                return ConvertBsonDocumentToDictionary(bsonValue.AsBsonDocument);
            case BsonType.Array:
                return bsonValue.AsBsonArray.Select(ConvertBsonValue).ToList();
            case BsonType.String:
                return bsonValue.AsString;
            case BsonType.Int32:
                return bsonValue.AsInt32;
            case BsonType.Int64:
                return bsonValue.AsInt64;
            case BsonType.Boolean:
                return bsonValue.AsBoolean;
            case BsonType.Double:
                return bsonValue.AsDouble;
            case BsonType.DateTime:
                return bsonValue.ToUniversalTime();
            // Add more cases as necessary for other BsonTypes you expect to handle
            default:
                return BsonTypeMapper.MapToDotNetValue(bsonValue); // Fallback for types not explicitly handled
        }
    }

}