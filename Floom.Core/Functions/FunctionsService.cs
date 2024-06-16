using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Floom.Assets;
using Floom.Repository;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public interface IFunctionsService
{
    Task<string> DeployFunctionAsync(string filePath, string userId);
    Task<string> RunFunctionAsync(string userId, string functionName, string userPrompt);
}

public class FunctionsService : IFunctionsService
{
    private readonly FloomAssetsRepository _floomAssetsRepository;
    private readonly IRepository<FunctionEntity> _repository;
    private readonly HttpClient _httpClient;

    public FunctionsService(FloomAssetsRepository floomAssetsRepository, IRepositoryFactory repositoryFactory, HttpClient httpClient)
    {
        _floomAssetsRepository = floomAssetsRepository;
        _repository = repositoryFactory.Create<FunctionEntity>();
        _httpClient = httpClient;
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
                //await _repository.Update(existingFunction);
                await _repository.UpsertEntity(existingFunction, existingFunction.Id, "Id");
                return existingFunction.Id;
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
                    userId = userId
                };
                await _repository.Insert(functionEntity);
                return functionEntity.Id;
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
        // 1. Get the Function entity by userId and functionName
        var normalizedFunctionName = FunctionsUtils.NormalizeFunctionName(functionName);
        var functionEntity = await _repository.FindByCondition(f => f.name == normalizedFunctionName && f.userId == userId);

        if (functionEntity == null)
        {
            throw new Exception("Function not found.");
        }

        // 2. Get the promptUrl file
        var promptFileUrl = functionEntity.promptUrl;

        // 3. Download the prompt.py file
        var promptFileStream = await _floomAssetsRepository.DownloadFileFromS3Async(promptFileUrl);
        var promptFilePath = Path.GetTempFileName();
        using (var fileStream = new FileStream(promptFilePath, FileMode.Create, FileAccess.Write))
        {
            await promptFileStream.CopyToAsync(fileStream);
        }

        try
        {
            // 4. Prepare the request content
            var requestContent = new MultipartFormDataContent();
            var config = new LambdaRunConfig
            {
                Input = userPrompt,
                Variables = new Dictionary<string, string> { { "language", "hebrew" } },
                ConfigSettings = new Dictionary<string, string>(),
                Env = new Dictionary<string, string> { { "OPENAI_API_KEY", "REMOVED-1kYJtfEErUPPgpo11MdfT3BlbkFJ5x8f10AGqx7MpZda0ezP" } }
            };

            var configJson = JsonSerializer.Serialize(config);
            requestContent.Add(new StreamContent(new FileStream(promptFilePath, FileMode.Open)), "file", "prompt.py");
            requestContent.Add(new StringContent(configJson, Encoding.UTF8, "application/json"), "config");

            // 5. Execute the HTTP request to run the lambda function
            using (var response = await _httpClient.PostAsync("https://i4yijg36th.execute-api.us-east-1.amazonaws.com/prompt", requestContent))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
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