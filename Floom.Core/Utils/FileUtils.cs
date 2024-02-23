using System.Security.Cryptography;

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
}