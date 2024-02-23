using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Assets
{
    [BsonIgnoreExtraElements]
    public class AssetEntity : DatabaseEntity
    {
        public string? originalName { get; set; }
        public string? assetId { get; set; }
        public string? storedName { get; set; }
        public string? storedPath { get; set; }
        public string? extension { get; set; }
        public long size { get; set; } //bytes
        public string? checksum { get; set; } // Add this line
    }
}