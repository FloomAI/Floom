using Floom.Base;
using Floom.Repository;

namespace Floom.Assets;

[CollectionName("assets")]
public class AssetEntity : DatabaseEntity
{
    public string? originalName { get; set; }
    public string? storedName { get; set; }
    public string? storedPath { get; set; }
    public string? extension { get; set; }
    public long size { get; set; } //bytes
    public string? checksum { get; set; } // Add this line
}