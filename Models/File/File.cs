using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    [BsonIgnoreExtraElements]
    public class File : ObjectEntity
    {
        public string originalName { get; set; }
        public string fileId { get; set; }
        public string storedName { get; set; }
        public string storedPath { get; set; }
        public string extension { get; set; }
        public long size { get; set; } //bytes

    }
}