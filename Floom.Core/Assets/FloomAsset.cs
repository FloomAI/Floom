using Floom.Assets;
using MongoDB.Bson;

namespace Floom.Data;

public class FloomAsset
{
    public string? Id { get; set; }
    public string? OriginalName { get; set; }
    public string? AssetId { get; set; }
    public string? StoredName { get; set; }
    public string? StoredPath { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; } //bytes
    
    public static FloomAsset FromEntity(AssetEntity assetEntity)
    {
        return new FloomAsset
        {
            Id = assetEntity.Id == ObjectId.Empty ? null : assetEntity.Id.ToString(),
            OriginalName = assetEntity.originalName,
            AssetId = assetEntity.assetId,
            StoredName = assetEntity.storedName,
            StoredPath = assetEntity.storedPath,
            Extension = assetEntity.extension,
            Size = assetEntity.size
        };
    }
}