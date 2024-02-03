using Floom.Repository;
using MongoDB.Driver;

namespace Floom.Auth
{
    public class ApiKeyInitializer
    {
        private readonly IRepository<ApiKeyEntity> _repository;

        public ApiKeyInitializer(IRepositoryFactory repositoryFactory)
        {
            _repository = repositoryFactory.Create<ApiKeyEntity>("api-keys");
        }

        public void Initialize()
        {
            var existingApiKey = _repository.GetAll().Result.FirstOrDefault();

            if (existingApiKey == null)
            {
                var random = new Random();
                var apiKey = new string(Enumerable
                    .Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 32)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                _repository.Insert(new ApiKeyEntity { Key = apiKey });

                Console.WriteLine($"Generated API Key: {apiKey}");
            }
        }
    }
}