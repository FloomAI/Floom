using Floom.Controllers;
using Floom.Repository;

namespace Floom.Auth;

public interface IUsersService
{
    Task<ApiKeyEntity> RegisterGuestUserAsync();
}

public class UsersService : IUsersService
{
    private readonly IRepository<UserEntity> _userRepository;
    private readonly IRepository<ApiKeyEntity> _apiKeyRepository;

    public UsersService(IRepositoryFactory repositoryFactory)
    {
        _userRepository = repositoryFactory.Create<UserEntity>();
        _apiKeyRepository = repositoryFactory.Create<ApiKeyEntity>();
    }

    public async Task<ApiKeyEntity> RegisterGuestUserAsync()
    {
        var user = new UserEntity
        {
            validated = false,
            type = "guest",
            username = FloomUsernameGenerator.GenerateTemporaryUsername()
        };
        await _userRepository.Insert(user);

        var apiKey = new ApiKeyEntity
        {
            userId = user.Id.ToString(),
            key =  ApiKeyUtils.GenerateApiKey()
        };

        await _apiKeyRepository.Insert(apiKey);

        return apiKey;
    }
}