using MongoDB.Bson;

namespace Floom.Entities.Assets
{
    public class AssetModel : BaseModel
    {
        public string? originalName { get; set; }
        public string? assetId { get; set; }
        public string? storedName { get; set; }
        public string? storedPath { get; set; }
        public string? extension { get; set; }
        public long size { get; set; } //bytes
        
        public static AssetModel FromEntity(AssetEntity assetEntity)
        {
            return new AssetModel
            {
                Id = assetEntity.Id == ObjectId.Empty ? null : assetEntity.Id.ToString(),
                originalName = assetEntity.originalName,
                assetId = assetEntity.assetId,
                storedName = assetEntity.storedName,
                storedPath = assetEntity.storedPath,
                extension = assetEntity.extension,
                size = assetEntity.size
            };
        }
    
        public AssetEntity ToEntity()
        {
            return new AssetEntity
            {
                Id = Id != null ? ObjectId.Parse(Id) : ObjectId.Empty,
                name = name,
                originalName = originalName,
                assetId = assetId,
                storedName = storedName,
                storedPath = storedPath,
                extension = extension,
                size = size
            };
        }
    }
}