using Floom.Repository;

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
                var apiKey = ApiKeyUtils.GenerateApiKey();

                _repository.Insert(new ApiKeyEntity { Key = apiKey });

                Console.WriteLine($"Generated API Key: {apiKey}");
            }
        }
    }
}