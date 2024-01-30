using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Floom.Misc
{
    public class ApiKeyInitializer
    {
        private readonly IDatabaseService _db;

        public ApiKeyInitializer(IDatabaseService database)
        {
            _db = database;
        }

        public void Initialize()
        {
            var existingApiKey = _db.ApiKeys.Find(a => true).FirstOrDefault();

            if (existingApiKey == null)
            {
                var random = new Random();
                var apiKey = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 32)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                _db.ApiKeys.InsertOne(new ApiKey { Key = apiKey });

                Console.WriteLine($"Generated API Key: {apiKey}");
            }
        }
    }
}
