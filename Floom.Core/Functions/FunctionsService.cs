using System.IO.Compression;
using System.Text;
using Floom.Assets;
using Floom.Repository;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;
using System.Net.Http.Headers;

public interface IFunctionsService
{
    Task<string> DeployFunctionAsync(string filePath, string userId);
    Task<string> RunFunctionAsync(string userId, string functionName, string userPrompt);

    Task<List<dynamic>> ListFunctionsAsync(string userId);
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
                existingFunction.description = manifest.description;
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
                env = new { OPENAI_API_KEY = "REMOVED-1kYJtfEErUPPgpo11MdfT3BlbkFJ5x8f10AGqx7MpZda0ezP" }
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

    public async Task<List<dynamic>> ListFunctionsAsync(string userId)
    {
        var functions = await _repository.ListByConditionAsync(f => f.userId == userId);
        var result = functions.Select(f => new { f.name, f.description, f.runtimeLanguage, f.runtimeFramework }).ToList<dynamic>();
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