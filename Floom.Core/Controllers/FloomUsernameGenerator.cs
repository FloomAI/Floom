using System.Security.Cryptography;
using System.Text;

namespace Floom.Controllers;

public class FloomUsernameGenerator
{
    private static readonly string[] prefixes = {
        "Floomi",
        "Floomer",
        "Floomist", 
        "Floomify", 
        "FloomX",
        "FloomNova", 
        "FloomByte", 
        "FloomNest",
        "FloomSpark",
        "FloomQuest",
        "FloomZen", 
        "FloomVerse",
        "FloomEcho"
    };

    public static string GenerateTemporaryUsername()
    {
        // Dynamic component based on a hash of the current timestamp and a random element
        var random = new Random();
        var timestamp = DateTime.UtcNow.Ticks.ToString();
        var randomComponent = random.Next(1000, 9999).ToString();
        var uniqueComponent = timestamp + randomComponent;

        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(uniqueComponent));
            var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

            // Take the first 6 characters to keep it short
            var shortHash = hash.Substring(0, 6);

            return $"{shortHash}";
        }
    }
    
    public static string GenerateTemporaryNickname()
    {
        // Randomly select a prefix
        var random = new Random();
        string prefix = prefixes[random.Next(prefixes.Length)];
        return prefix;
    }

}