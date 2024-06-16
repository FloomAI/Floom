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
    Task<RegisterUserResponse> RegisterOrLoginUserAsync(string provider, string email);
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
        return await RegisterUserAsync(user);
    }

    public async Task<RegisterUserResponse> RegisterOrLoginUserAsync(string provider, string email)
    {
        var existingUser = await _userRepository.Get(email, "emailAddress");

        if (existingUser != null && existingUser.registrationProvider == provider)
        {
            return await GenerateApiKeyForUserAsync(existingUser);
        }

        var user = new UserEntity
        {
            registrationProvider = provider,
            validated = true,
            type = "user",
            emailAddress = email,
            username = FloomUsernameGenerator.GenerateTemporaryUsername(),
            nickname = FloomUsernameGenerator.GenerateTemporaryNickname()
        };
        return await RegisterUserAsync(user);
    }

    private async Task<RegisterUserResponse> RegisterUserAsync(UserEntity user)
    {
        await _userRepository.Insert(user);
        return await GenerateApiKeyForUserAsync(user);
    }

    private async Task<RegisterUserResponse> GenerateApiKeyForUserAsync(UserEntity user)
    {
        var apiKey = new ApiKeyEntity
        {
            userId = user.Id,
            key = ApiKeyUtils.GenerateApiKey()
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