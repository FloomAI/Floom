using System.Security.Cryptography;
using System.Text;

namespace Floom.Utils;

public static class StringUtils
{
    public static string GetShortHash(string input)
    {
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }

            // Return the first 8 characters of the hash
            return sb.ToString().Substring(0, 8);
        }
    }

}