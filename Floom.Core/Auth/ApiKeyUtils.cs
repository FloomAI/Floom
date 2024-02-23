namespace Floom.Auth;

public class ApiKeyUtils
{
    public static string GenerateApiKey()
    {
        var random = new Random();
        return new string(Enumerable
            .Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}