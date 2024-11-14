using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using Floom.Assets;
using Floom.Auth;
using Floom.Repository;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Floom.Functions;

public interface IFunctionsService
{
    Task<string> DeployFunctionAsync(string filePath, string userId);
    Task<string> RunFunctionAsync(string userId, string functionName, string userPrompt, Dictionary<string, string>? parameters);
    Task<List<BaseFunctionDto>> ListFunctionsAsync(string userId);
    Task<BaseFunctionDto> GetFunctionByNameAsync(string userId, string functionName);
    Task<List<BaseFunctionDto>> ListPublicFeaturedFunctionsAsync();
    Task<List<BaseFunctionDto>> SearchPublicFunctionsAsync(string query);
    Task AddRolesToFunctionAsync(string functionName, string functionUserId, string userId);
    Task RemoveRolesToFunctionAsync(string functionName, string functionUserId, string userId);
}

public class FunctionsService : IFunctionsService
{
    private readonly FloomAssetsRepository _floomAssetsRepository;
    private readonly IRepository<FunctionEntity> _repository;

    private readonly IRepository<UserEntity> _userRepository;

    public FunctionsService(FloomAssetsRepository floomAssetsRepository, IRepositoryFactory repositoryFactory, HttpClient httpClient)
    {
        _floomAssetsRepository = floomAssetsRepository;
        _repository = repositoryFactory.Create<FunctionEntity>();
        _userRepository = repositoryFactory.Create<UserEntity>();
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

        if (user is not { emailAddress: "nadavnuni1@gmail.com" })
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

    public async Task RemoveRolesToFunctionAsync(string functionName, string functionUserId, string userId)
    {
        // Get the user by ID
        var user = await _userRepository.Get(userId, "_id");

        if (user is not { emailAddress: "nadavnuni1@gmail.com" })
        {

            throw new UnauthorizedAccessException("You are not authorized to perform this action.");
        }
        
        // Find the function by name
        var functionEntity = await FindFunctionByNameAndUserIdAsync(functionName, functionUserId);
        if (functionEntity == null)
        {
            throw new Exception("Function not found.");
        }
        
        // Remove the roles "Public" and "Featured" from the function
        if (functionEntity.roles == null)
        {
            return;
        }

        var rolesList = functionEntity.roles.ToList();
        if (rolesList.Contains(Roles.Public)) rolesList.Remove(Roles.Public);
        if (rolesList.Contains(Roles.Featured)) rolesList.Remove(Roles.Featured);
        functionEntity.roles = rolesList.ToArray();
        
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
            var existingFunction = await _repository.FindByCondition(f => f.name == normalizedFunctionName);
            // if existing function of the same name exists and belongs to the same user, update it
            if (existingFunction != null && existingFunction.userId == userId)
            {
                // Update existing function
                existingFunction.runtimeLanguage = manifest.runtime.language;
                existingFunction.runtimeFramework = manifest.runtime.framework;
                existingFunction.promptUrl = promptFileUrl;
                existingFunction.dataUrl = dataFileUrl;

                // Handle translated fields
                existingFunction.description = new TranslatedField
                {
                    en = manifest.description
                };
                
                if (manifest.parameters == null)
                {
                    existingFunction.parameters = new List<Parameter>();
                }
                else
                {
                    List<Parameter> parameters = manifest.parameters?.Select(dto => new Parameter
                    {
                        name = dto.name,
                        description = new TranslatedField
                        {
                            en = dto.description
                        },
                        required = dto.required,
                        defaultValue = dto.defaultValue
                    }).ToList() ?? new List<Parameter>();

                    existingFunction.parameters = parameters;
                }

                await _repository.UpsertEntity(existingFunction, existingFunction.Id, "Id");
                return existingFunction.name;
            }
            else
            {
                // Save new function entity
                
                var functionEntity = new FunctionEntity
                {
                    name = normalizedFunctionName,
                    runtimeLanguage = manifest.runtime.language,
                    runtimeFramework = manifest.runtime.framework,
                    promptUrl = promptFileUrl,
                    dataUrl = dataFileUrl,
                    userId = userId,
                    description = new TranslatedField
                    {
                        en = manifest.description
                    }
                };

                if (manifest.parameters == null)
                {
                    functionEntity.parameters = new List<Parameter>();
                }
                else
                {
                     List<Parameter> parameters = manifest.parameters?.Select(dto => new Parameter
                    {
                        name = dto.name,
                        description = new TranslatedField
                        {
                            en = dto.description
                        },
                        required = dto.required,
                        defaultValue = dto.defaultValue
                    }).ToList() ?? new List<Parameter>();
                    functionEntity.parameters = parameters;
                }

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

    public async Task<string> RunFunctionAsync(string? userId, string functionName, string userPrompt, Dictionary<string, string>? parameters)
    {
        if (string.IsNullOrEmpty(functionName))
        {
            throw new ArgumentNullException(nameof(functionName));
        }

        if (string.IsNullOrEmpty(userPrompt))
        {
            throw new ArgumentNullException(nameof(userPrompt));
        }
        
        var normalizedFunctionName = FunctionsUtils.NormalizeFunctionName(functionName);
        var functionEntity = await _repository.FindByCondition(f => f.name == normalizedFunctionName);

        if (functionEntity == null)
        {
            throw new Exception("Function not found.");
        }

        // Check if the function is public or if the user is the owner of the function
        if (functionEntity.IsPublic() || (userId != null && userId == functionEntity.userId))
        {
            return await RunFunctionInternal(functionEntity, userPrompt, parameters);
        }

        throw new Exception("User does not have access to this function.");
    }
    
    private async Task<string> RunFunctionInternal(FunctionEntity functionEntity, string userPrompt, Dictionary<string, string>? parameters)
    {
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
            var requestTemp = new HttpRequestMessage(HttpMethod.Post, "https://roy1dayo5i.execute-api.us-east-1.amazonaws.com/prompt");
            var form = new MultipartFormDataContent();

            // Add the file
            var fileContent = new StreamContent(new FileStream(promptFilePath, FileMode.Open, FileAccess.Read));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "file", "prompt.py");

            // convert parameterDtos to dictionary and set as variables
            // Add the config
            // parameters is a dictionary of key-value pairs, could be null, in case of null do not add to the config
            var config = new
            {
                input = userPrompt,
                variables = parameters,
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
    
    public async Task<List<BaseFunctionDto>> ListFunctionsAsync(string userId)
    {
        var functions = await _repository.ListByConditionAsync(f => f.userId == userId);

        var result = new List<BaseFunctionDto>();

        foreach (var function in functions)
        {
            // Fetch the user details for each function's userId
            var user = await _userRepository.Get(function.userId, "_id");

            var authorName = !string.IsNullOrEmpty(user?.nickname) ? user.nickname : user?.username;

            result.Add(new BaseFunctionDto
            {
                name = function.name ?? string.Empty, // Set to empty string if null
                description = function.description, // Ensure only English description is used
                runtimeLanguage = function.runtimeLanguage ?? string.Empty, // Set to empty string if null
                runtimeFramework = function.runtimeFramework ?? string.Empty, // Set to empty string if null
                author = string.IsNullOrEmpty(authorName) ? null : authorName, // Set to null if empty
                version = function.version ?? string.Empty, // Set to empty string if null
                rating = function.rating ?? 0.0, // Set to 0 if null (assuming rating is a numeric type)
                downloads = function.downloads ?? new List<int>(), // Set to empty list if null
                parameters = function.parameters?.Select(p => new ParameterDto
                {
                    name = p.name ?? string.Empty, // Set to empty string if null
                    description = p.description, // Fetch only the English description
                    required = p.required, // Boolean field
                    defaultValue = p.defaultValue switch
                    {
                        string str => str, // Keep as string if it's a string
                        IEnumerable<object> array => array.ToArray(), // Convert to array if it's IEnumerable
                        _ => null // Otherwise, set to null
                    }
                }).ToList() ?? new List<ParameterDto>() // Initialize as empty list if parameters is null
            });
        }

        return result;
    }

    public async Task<BaseFunctionDto> GetFunctionByNameAsync(string? userId, string functionName)
    {
       // if userId is null, then function must have public role
       // otherwise, function must have the same userId
         
         var normalizedFunctionName = FunctionsUtils.NormalizeFunctionName(functionName);
         var function = await _repository.FindByCondition(f => f.name == normalizedFunctionName && f.roles.Contains(Roles.Public));

         if(function == null)
         {
             return null;
         }
         
         var result = new BaseFunctionDto()
         {
             name = function.name,
             description = function.description,
             runtimeLanguage = function.runtimeLanguage ?? string.Empty,
             runtimeFramework = function.runtimeFramework ?? string.Empty,
             version = function.version ?? string.Empty,
             rating = function.rating ?? 0.0,
             downloads = function.downloads ?? new List<int>(),
             parameters = function.parameters?.Select(p => new ParameterDto
             {
                 name = p.name ?? string.Empty,
                 description = p.description,
                 required = p.required,
                 defaultValue = p.defaultValue switch
                 {
                     string str => str,
                     IEnumerable<object> array => array.ToArray(),
                     _ => null
                 }
             }).ToList() ?? new List<ParameterDto>()
         };
         
         return result;
    }


    public async Task<List<BaseFunctionDto>> ListPublicFeaturedFunctionsAsync()
    {
        var publicFeaturedFunctions = await _repository.ListByConditionAsync(
            f => f.roles != null && f.roles.Contains(Roles.Public) && f.roles.Contains(Roles.Featured));

        var result = new List<BaseFunctionDto>();

        foreach (var function in publicFeaturedFunctions)
        {
            var user = await _userRepository.Get(function.userId, "_id");
            if (user == null)
            {
                continue;
            }
            
            var authorName = !string.IsNullOrEmpty(user.nickname) ? user.nickname : user.username;
            
            result.Add(new BaseFunctionDto
            {
                name = function.name,
                title = new TranslatedField
                {
                    en = function.title?.en ?? string.Empty,
                    fr = function.title?.fr ?? string.Empty,
                    es = function.title?.es ?? string.Empty
                },
                description = new TranslatedField
                {
                    en = function.description?.en ?? string.Empty,
                    fr = function.description?.fr ?? string.Empty,
                    es = function.description?.es ?? string.Empty
                },
                promptPlaceholder = new TranslatedField()
                {
                    en = function.promptPlaceholder?.en ?? string.Empty,
                    fr = function.promptPlaceholder?.fr ?? string.Empty,
                    es = function.promptPlaceholder?.es ?? string.Empty
                },
                runtimeLanguage = function.runtimeLanguage ?? string.Empty,
                runtimeFramework = function.runtimeFramework ?? string.Empty,
                author = string.IsNullOrEmpty(authorName) ? null : authorName,
                version = function.version ?? string.Empty,
                rating = function.rating ?? 0f,
                downloads = function.downloads ?? new List<int>(),
                parameters = function.parameters?.Select(p => new ParameterDto
                {
                    name = p.name ?? string.Empty,
                    description = new TranslatedField
                    {
                        en = p.description?.en ?? string.Empty,
                        fr = p.description?.fr ?? string.Empty,
                        es = p.description?.es ?? string.Empty
                    },
                    required = p.required,
                    defaultValue = p.defaultValue is string
                        ? p.defaultValue
                        : p.defaultValue is IEnumerable<object> array
                            ? array.ToArray()
                            : null
                }).ToList() ?? new List<ParameterDto>()
            });
        }

        return result;
    }
    
    public async Task<List<BaseFunctionDto>> SearchPublicFunctionsAsync(string query)
    {
        if (query == null || query.Length < 5)
        {
            return new List<BaseFunctionDto>();
        }
        
        // Search for functions with the "Public" role
        // Search for function that their description or name contains the query
        
        var regexQuery = new BsonRegularExpression(query, "i");  // 'i' makes the regex case-insensitive

        // Create filters for both the title and description fields
        var titleFilter = Builders<FunctionEntity>.Filter.Regex("title.en", regexQuery);
        var descriptionFilter = Builders<FunctionEntity>.Filter.Regex("description.en", regexQuery);
        var nameFilter = Builders<FunctionEntity>.Filter.Regex("name", regexQuery);

        // Create a role filter to match "Public" roles
        var roleFilter = Builders<FunctionEntity>.Filter.Eq("roles", Roles.Public);

        // Combine the filters using OR (|) for title and description and AND (&) for roles
        var finalFilter = Builders<FunctionEntity>.Filter.And(
            roleFilter,
            Builders<FunctionEntity>.Filter.Or(nameFilter, titleFilter, descriptionFilter)
        );

        // Fetch the results using the combined filter
        var searchResults = await _repository.ListByFilterAsync(finalFilter);

        var result = new List<BaseFunctionDto>();

        foreach (var function in searchResults)
        {
            var user = await _userRepository.Get(function.userId, "_id");
            if (user == null)
            {
                continue;
            }
            
            var authorName = !string.IsNullOrEmpty(user.nickname) ? user.nickname : user.username;
            
            result.Add(new BaseFunctionDto
            {
                name = function.name,
                author = authorName,
                rating = function.rating ?? 0f,
                title = new TranslatedField
                {
                    en = function.title?.en ?? string.Empty,
                    fr = function.title?.fr ?? string.Empty,
                    es = function.title?.es ?? string.Empty
                },
                description = new TranslatedField
                {
                    en = function.description?.en ?? string.Empty,
                    fr = function.description?.fr ?? string.Empty,
                    es = function.description?.es ?? string.Empty
                }
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

    public List<ManifestParameter>? parameters { get; set; }
}

public class ManifestParameter
{
    public string name { get; set; }
    public string description { get; set; }
    public bool required { get; set; }
    public object? defaultValue { get; set; }
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