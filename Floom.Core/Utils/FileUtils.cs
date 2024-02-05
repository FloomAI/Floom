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

}