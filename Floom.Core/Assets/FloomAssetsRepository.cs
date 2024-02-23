using Floom.Base;
using Floom.Data;
using Floom.Logs;
using Floom.Repository;
using Floom.Utils;

namespace Floom.Assets;

public class FloomAssetsRepository : FloomSingletonBase<FloomAssetsRepository>
{
    private readonly ILogger _logger;
    private IRepository<AssetEntity> _repository;
    private const string SubDirectory = "floom_user_files";
    private readonly string _filesDirectory =  Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, SubDirectory);
    private static readonly object LockObject = new();
    private static bool _isInitialized;
    
    // Private constructor to prevent instance creation outside the class.
    public FloomAssetsRepository()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public override void Initialize(IRepositoryFactory repositoryFactory)
    {
        lock (LockObject)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("FloomAssetsRepository is already Initialized.");
            }

            _repository = repositoryFactory.Create<AssetEntity>("assets");
            _isInitialized = true;
        }
    }
    
    public async Task<string?> CreateAsset(IFormFile file)
    {
        try
        {
            var checksum = await FileUtils.CalculateChecksumAsync(file);
            var existingAssets = await _repository.FindByCondition(
                a => a.originalName == file.FileName && a.checksum == checksum);

            var existingAsset = existingAssets.FirstOrDefault(); // Assuming you're okay with taking the first match

            if (existingAsset != null)
            {
                _logger.LogInformation($"File already exists: {existingAsset.assetId}");
                return existingAsset.assetId; // Return existing asset ID
            }

            var assetId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(file.FileName);

            if (!Directory.Exists(_filesDirectory))
            {
                Directory.CreateDirectory(_filesDirectory);
            }

            var storedFile = $"{assetId}{fileExtension}";
            var filePath = Path.Combine(_filesDirectory, storedFile);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileDocument = new AssetEntity
            {
                assetId = assetId.ToString(),
                originalName = file.FileName,
                storedName = storedFile,
                storedPath = filePath,
                extension = fileExtension,
                size = file.Length,
                checksum = checksum
            };

            await _repository.Insert(fileDocument);

            return assetId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating file.");
            return null;
        }
    }
    
    public async Task<FloomAsset?> GetAssetById(string assetId)
    {
        var assetEntity = await _repository.Get(assetId, "assetId");
        return FloomAsset.FromEntity(assetEntity);
    }
}