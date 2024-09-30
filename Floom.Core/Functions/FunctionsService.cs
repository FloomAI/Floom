using System.IO.Compression;
using System.Text;
using Floom.Assets;
using Floom.Repository;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Floom.Auth;

public interface IFunctionsService
{
    Task<string> DeployFunctionAsync(string filePath, string userId);
    Task<string> RunFunctionAsync(string userId, string functionName, string userPrompt);
    Task<List<FunctionDto>> ListFunctionsAsync(string userId);
    Task<List<FunctionDto>> ListPublicFeaturedFunctionsAsync();
    Task<FunctionEntity?> FindFunctionByNameAndUserIdAsync(string functionName, string functionUserId);
    Task UpdateFunctionAsync(FunctionEntity functionEntity);
    Task AddRolesToFunctionAsync(string functionName, string functionUserId, string userId);
}

public class FunctionsService : IFunctionsService
{
    private readonly FloomAssetsRepository _floomAssetsRepository;
    private readonly IRepository<FunctionEntity> _repository;

    private readonly IRepository<UserEntity> _userRepository;
    private readonly HttpClient _httpClient;

    public FunctionsService(FloomAssetsRepository floomAssetsRepository, IRepositoryFactory repositoryFactory, HttpClient httpClient)
    {
        _floomAssetsRepository = floomAssetsRepository;
        _repository = repositoryFactory.Create<FunctionEntity>();
        _userRepository = repositoryFactory.Create<UserEntity>();
        _httpClient = httpClient;
    }

    public async Task<FunctionEntity?> FindFunctionByNameAndUserIdAsync(string functionName, string functionUserId)
    {
        return await _repository.FindByCondition(f => f.name == functionName && f.userId == functionUserId);
    }

    public async Task UpdateFunctionAsync(FunctionEntity functionEntity)
    {
        await _repository.UpsertEntity(functionEntity, functionEntity.Id, "Id");
    }

    public async Task AddRolesToFunctionAsync(string functionName, string functionUserId, string userId)
    {
        // Get the user by ID
        var user = await _userRepository.Get(userId, "_id");

        if (user == null || user.emailAddress != "nadavnuni1@gmail.com")
        {

            throw new UnauthorizedAccessException("You are not authorized to perform this action.");
        }

        // Find the function by name
        var functionEntity = await FindFunctionByNameAndUserIdAsync(functionName, functionUserId);
        if (functionEntity == null)
        {
            throw new Exception("Function not found.");
        }

        // Add the roles "Public" and "Featured" to the function
        if (functionEntity.roles == null)
        {
            functionEntity.roles = new string[] { Roles.Public, Roles.Featured };
        }
        else
        {
            var rolesList = functionEntity.roles.ToList();
            if (!rolesList.Contains(Roles.Public)) rolesList.Add(Roles.Public);
            if (!rolesList.Contains(Roles.Featured)) rolesList.Add(Roles.Featured);
            functionEntity.roles = rolesList.ToArray();
        }

        // Update the function in the repository
        await UpdateFunctionAsync(functionEntity);
    }

    public async Task<string> DeployFunctionAsync(string filePath, string userId)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            // 1. Unzip the file
            Directory.CreateDirectory(tempDirectory);
            ZipFile.ExtractToDirectory(filePath, tempDirectory);

            // 2. Upload prompt.py to S3
            var promptFilePath = Path.Combine(tempDirectory, "prompt.py");
            if (!File.Exists(promptFilePath))
            {
                throw new FileNotFoundException("prompt.py is missing in the zip file.");
            }
            using var promptStream = new FileStream(promptFilePath, FileMode.Open);
            var promptFile = new FormFile(promptStream, 0, promptStream.Length, null, "prompt.py");
            var promptFileUrl = await _floomAssetsRepository.UploadPythonFileToAwsBucket(promptFile);

            // 3. Upload data.py to S3 if it exists
            string dataFileUrl = null;
            var dataFilePath = Path.Combine(tempDirectory, "data.py");
            if (File.Exists(dataFilePath))
            {
                using var dataStream = new FileStream(dataFilePath, FileMode.Open);
                var dataFile = new FormFile(dataStream, 0, dataStream.Length, null, "data.py");
                dataFileUrl = await _floomAssetsRepository.UploadPythonFileToAwsBucket(dataFile);
            }

            // 4. Parse manifest.yml
            var manifestFilePath = Path.Combine(tempDirectory, "manifest.yml");
            if (!File.Exists(manifestFilePath))
            {
                throw new FileNotFoundException("manifest.yml is missing in the zip file.");
            }

            Manifest? manifest = null;
            try
            {
                var manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var manifestDto = deserializer.Deserialize<ManifestDto>(manifestContent);
                manifest = manifestDto.manifest;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse manifest.yml", ex);
            }

            // 5. Normalize function name and check if it exists for the user
            var normalizedFunctionName = FunctionsUtils.NormalizeFunctionName(manifest.name);
            var existingFunction = await _repository.FindByCondition(f => f.name == normalizedFunctionName && f.userId == userId);
            if (existingFunction != null)
            {
                // Update existing function
                existingFunction.runtimeLanguage = manifest.runtime.language;
                existingFunction.runtimeFramework = manifest.runtime.framework;
                existingFunction.promptUrl = promptFileUrl;
                existingFunction.dataUrl = dataFileUrl;
                existingFunction.description = manifest.description;
                await _repository.UpsertEntity(existingFunction, existingFunction.Id, "Id");
                return existingFunction.Id;
            }
            else
            {
                // Save new function entity
                var functionEntity = new FunctionEntity
                {
                    name = normalizedFunctionName,
                    description = manifest.description,
                    runtimeLanguage = manifest.runtime.language,
                    runtimeFramework = manifest.runtime.framework,
                    promptUrl = promptFileUrl,
                    dataUrl = dataFileUrl,
                    userId = userId
                };
                await _repository.Insert(functionEntity);
                return functionEntity.name;
            }
        }
        finally
        {
            // Ensure the temporary directory and its contents are deleted
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }

    public async Task<string> RunFunctionAsync(string userId, string functionName, string userPrompt)
    {
        Console.WriteLine("RunFunctionAsync userId:" + userId);
        // 1. Get the Function entity by userId and functionName
        var normalizedFunctionName = FunctionsUtils.NormalizeFunctionName(functionName);
        var functionEntity = await _repository.FindByCondition(f => f.name == normalizedFunctionName && f.userId == userId);

        if (functionEntity == null)
        {
            throw new Exception("Function not found.");
        }

        Console.WriteLine("FunctionEntity: "+ functionEntity.Id + ",name: " + functionEntity.name + ", promptUrl: " + functionEntity.promptUrl);
        Console.WriteLine("UserPrompt: " + userPrompt);
        // 2. Get the promptUrl file
        var promptFileUrl = functionEntity.promptUrl;

        // 3. Download the prompt.py file
        Console.WriteLine("Downloading prompt file from S3");
        var promptFileStream = await _floomAssetsRepository.DownloadFileFromS3Async(promptFileUrl);
        Console.WriteLine("Downloaded prompt file from S3");
        var promptFilePath = Path.GetTempFileName();
        // copy file to file stream
        using (var fileStream = new FileStream(promptFilePath, FileMode.Create, FileAccess.Write))
        {
            await promptFileStream.CopyToAsync(fileStream);
        }

        try
        {
            // Prepare and send the HTTP request
            var client = new HttpClient();
            var requestTemp = new HttpRequestMessage(HttpMethod.Post, "https://i4yijg36th.execute-api.us-east-1.amazonaws.com/prompt");

            var form = new MultipartFormDataContent();

            // Add the file
            var fileContent = new StreamContent(new FileStream(promptFilePath, FileMode.Open, FileAccess.Read));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "file", "prompt.py");

            // Add the config
            var config = new
            {
                input = userPrompt,
                variables = new { language = "hebrew" },
                config = new { },
                env = new { OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") }
            };
            var configJson = JsonConvert.SerializeObject(config);
            form.Add(new StringContent(configJson, Encoding.UTF8, "application/json"), "config");

            requestTemp.Content = form;

            var response = await client.SendAsync(requestTemp);
            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }
        finally
        {
            // Clean up the temporary file
            if (File.Exists(promptFilePath))
            {
                File.Delete(promptFilePath);
            }
        }
    }

    public async Task<List<FunctionDto>> ListFunctionsAsync(string userId)
    {
        var functions = await _repository.ListByConditionAsync(f => f.userId == userId);
    
        var result = new List<FunctionDto>();

        foreach (var function in functions)
        {
            // Fetch the user details for each function's userId
            var user = await _userRepository.Get(function.userId, "_id");

            var authorName = !string.IsNullOrEmpty(user?.nickname) ? user.nickname : user?.username;

            result.Add(new FunctionDto
            {
                name = function.name ?? string.Empty, // Set to empty string if null
                description = function.description ?? string.Empty, // Set to empty string if null
                runtimeLanguage = function.runtimeLanguage ?? string.Empty, // Set to empty string if null
                runtimeFramework = function.runtimeFramework ?? string.Empty, // Set to empty string if null
                author = string.IsNullOrEmpty(authorName) ? null : authorName, // Set to null if empty
                version = function.version ?? string.Empty, // Set to empty string if null
                rating = function.rating ?? 0, // Set to 0 if null (assuming rating is a numeric type)
                downloads = function.downloads ?? new List<int>(), // Set to empty list if null
                parameters = function.parameters?.Select(p => new ParameterDto
                {
                    name = p.name ?? string.Empty, // Set to empty string if null
                    description = p.description ?? string.Empty, // Set to empty string if null
                    required = p.required, // Assuming this is a boolean, keep as is
                    defaultValue = p.defaultValue ?? string.Empty // Set to empty string if null
                }).ToList() ?? new List<ParameterDto>() // Initialize as empty list if parameters is null
            });
        }

        return result;

    }


    public async Task<List<FunctionDto>> ListPublicFeaturedFunctionsAsync()
    {
        var publicFeaturedFunctions = await _repository.ListByConditionAsync(
            f => f.roles != null && f.roles.Contains(Roles.Public) && f.roles.Contains(Roles.Featured)
        );

        // Assuming a method GetUserByIdAsync(string userId) to fetch the user entity
        var result = new List<FunctionDto>();

        foreach (var function in publicFeaturedFunctions)
        {
            Console.WriteLine("Function: " + function.Id + ", name: " + function.name + ", userId: " + function.userId);
            var user = await _userRepository.Get(function.userId, "_id");
            if (user == null)
            Console.WriteLine("User: " + user.Id + ", username: " + user.username + ", nickname: " + user.nickname);
            var authorName = !string.IsNullOrEmpty(user.nickname) ? user.nickname : user.username;

            result.Add(new FunctionDto
            {
                name = function.name ?? string.Empty, // Set to empty string if null
                description = function.description ?? string.Empty, // Set to empty string if null
                runtimeLanguage = function.runtimeLanguage ?? string.Empty, // Set to empty string if null
                runtimeFramework = function.runtimeFramework ?? string.Empty, // Set to empty string if null
                author = string.IsNullOrEmpty(authorName) ? null : authorName, // Set to null if empty
                version = function.version ?? string.Empty, // Set to empty string if null
                rating = function.rating ?? 0, // Set to 0 if null (assuming rating is a numeric type)
                downloads = function.downloads ?? new List<int>(), // Set to empty list if null
                parameters = function.parameters?.Select(p => new ParameterDto
                {
                    name = p.name ?? string.Empty, // Set to empty string if null
                    description = p.description ?? string.Empty, // Set to empty string if null
                    required = p.required, // Assuming this is a boolean, keep as is
                    defaultValue = p.defaultValue ?? string.Empty // Set to empty string if null
                }).ToList() ?? new List<ParameterDto>() // Initialize as empty list if parameters is null
            });

        }

        return result;
    }
}

public class LambdaRunConfig
{
    public string Input { get; set; }
    public Dictionary<string, string> Variables { get; set; }
    public Dictionary<string, string> ConfigSettings { get; set; }
    public Dictionary<string, string> Env { get; set; }
}



partial class ManifestDto
{
    public Manifest manifest;
}

public class Manifest
{
    public string name { get; set; }
    public string? description { get; set; }
    public Runtime runtime { get; set; }
    public Entrypoint entrypoint { get; set; }
}

public class Runtime
{
    public string language { get; set; }
    public string framework { get; set; }
}

public class Entrypoint
{
    public string prompt { get; set; }
}