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
        _userRepository = repositoryFactory.Create<UserEntity>("users");
        _apiKeyRepository = repositoryFactory.Create<ApiKeyEntity>("api-keys");
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
            UserId = user.Id.ToString(),
            Key =  ApiKeyUtils.GenerateApiKey()
        };

        await _apiKeyRepository.Insert(apiKey);

        return apiKey;
    }
}