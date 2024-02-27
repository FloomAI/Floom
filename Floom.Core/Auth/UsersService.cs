using Floom.Controllers;
using Floom.Repository;

namespace Floom.Auth;

public class RegisterUserResponse
{
    public string ApiKey { get; set; }
    public string Username { get; set; }
    public string Nickname { get; set; }
}

public interface IUsersService
{
    Task<RegisterUserResponse> RegisterGuestUserAsync();
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

    public async Task<RegisterUserResponse> RegisterGuestUserAsync()
    {
        var user = new UserEntity
        {
            validated = false,
            type = "guest",
            username = FloomUsernameGenerator.GenerateTemporaryUsername(),
            nickname = FloomUsernameGenerator.GenerateTemporaryNickname()
        };
        await _userRepository.Insert(user);

        var apiKey = new ApiKeyEntity
        {
            userId = user.Id,
            key =  ApiKeyUtils.GenerateApiKey()
        };

        await _apiKeyRepository.Insert(apiKey);

        return new RegisterUserResponse
        {
            ApiKey = apiKey.key,
            Username = user.username,
            Nickname = user.nickname
        };
    }
}