using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Floom.Data;

namespace Floom.Utils;

public static class FileUtils
{
    public static async Task<byte[]> ConvertIFormFileToByteArrayAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return new byte[0];

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    public static async Task<string> CalculateChecksumAsync(IFormFile file)
    {
        using (var stream = file.OpenReadStream())
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = await sha256.ComputeHashAsync(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
    
    public static async Task<byte[]> ReadFileAsync(FloomAsset floomAsset)
    {
        await using var fileStream = new FileStream(floomAsset.StoredPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return fileBytes;
    }
    
    public static async Task<byte[]> ReadFileCloudAsync(FloomAsset floomAsset)
    {
        var s3Client = new AmazonS3Client();
        var bucketName = Environment.GetEnvironmentVariable("FLOOM_S3_BUCKET") ?? "empty_bucket";
        var fileKey = floomAsset.StoredName;

        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = fileKey
        };

        using (var response = await s3Client.GetObjectAsync(request))
        {
            using (var responseStream = response.ResponseStream)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await responseStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}