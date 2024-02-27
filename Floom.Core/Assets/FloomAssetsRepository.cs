using Amazon.S3;
using Amazon.S3.Transfer;
using Floom.Base;
using Floom.Data;
using Floom.Logs;
using Floom.Repository;
using Floom.Utils;
using MongoDB.Bson;

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

            _repository = repositoryFactory.Create<AssetEntity>();
            _isInitialized = true;
        }
    }
    
    public async Task<string?> CreateAsset(IFormFile file)
    {
        var floomEnvironment = Environment.GetEnvironmentVariable("FLOOM_ENVIRONMENT");
        if (floomEnvironment == "local")
        {
            return await CreateAssetLocally(file);
        }
        else if (floomEnvironment == "cloud")
        {
            return await CreateAssetInAwsBucket(file);
        }
        else
        {
            _logger.LogError("Invalid FLOOM_ENVIRONMENT value.");
            return null;
        }
    }
    
    public async Task<string?> CreateAssetLocally(IFormFile file)
    {
        try
        {
            var checksum = await FileUtils.CalculateChecksumAsync(file);
            
            var existingAsset = await _repository.FindByCondition(a => a.originalName == file.FileName && a.checksum == checksum);
            
            if (existingAsset != null)
            {
                _logger.LogInformation($"File already exists: {existingAsset.Id}");
                return existingAsset.Id; // Return existing asset ID
            }

            var assetId = ObjectId.GenerateNewId().ToString();
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
                Id = assetId,
                originalName = file.FileName,
                storedName = storedFile,
                storedPath = filePath,
                extension = fileExtension,
                size = file.Length,
                checksum = checksum
            };

            await _repository.Insert(fileDocument);

            return assetId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating file.");
            return null;
        }
    }
    
    public async Task<string?> CreateAssetInAwsBucket(IFormFile file)
    {
        try
        {
            var checksum = await FileUtils.CalculateChecksumAsync(file);

            var existingAsset = await _repository.FindByCondition(a => a.originalName == file.FileName && a.checksum == checksum);

            if (existingAsset != null)
            {
                _logger.LogInformation($"File already exists: {existingAsset.Id}");
                return existingAsset.Id; // Return existing asset ID
            }

            var assetId = ObjectId.GenerateNewId().ToString();
            var fileExtension = Path.GetExtension(file.FileName);
            var storedFile = $"{assetId}{fileExtension}";

            // Specify your bucket name
            var bucketName = Environment.GetEnvironmentVariable("FLOOM_S3_BUCKET") ?? "empty_bucket";

            // Create a client
            using (var client = new AmazonS3Client())
            {
                using (var newMemoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(newMemoryStream);

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = newMemoryStream,
                        Key = storedFile,
                        BucketName = bucketName,
                        CannedACL = S3CannedACL.Private
                    };

                    var transferUtility = new TransferUtility(client);
                    await transferUtility.UploadAsync(uploadRequest);
                }
            }

            var fileDocument = new AssetEntity
            {
                Id = assetId,
                originalName = file.FileName,
                storedName = storedFile,
                storedPath = $"s3://{bucketName}/{storedFile}",
                extension = fileExtension,
                size = file.Length,
                checksum = checksum
            };

            await _repository.Insert(fileDocument);

            return assetId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating file.");
            return null;
        }
    }
    
    public async Task<FloomAsset?> GetAssetById(string assetId)
    {
        var assetEntity = await _repository.Get(assetId, "_id");
        return FloomAsset.FromEntity(assetEntity);
    }
}