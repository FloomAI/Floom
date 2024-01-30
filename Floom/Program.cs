using System.Text.Json;
using System.Text.Json.Serialization;
using Floom.Audit;
using Floom.Auth;
using Floom.Config;
using Floom.Context.Embeddings;
using Floom.Context.VectorStores;
using Floom.Data;
using Floom.Embeddings;
using Floom.Embeddings.Ollama;
using Floom.Embeddings.OpenAi;
using Floom.Events;
using Floom.LLMs;
using Floom.LLMs.LLama;
using Floom.LLMs.Ollama;
using Floom.LLMs.OpenAi;
using Floom.Logs;
using Floom.Misc;
using Floom.Model.OpenAi;
using Floom.Pipeline;
using Floom.Pipeline.Model;
using Floom.Pipeline.Prompt;
using Floom.Plugin;
using Floom.Repository;
using Floom.Services;
using Floom.Utils;
using Floom.VectorStores;
using Floom.VectorStores.Engines;
using Floom.Vendors;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using YamlDotNet.Serialization;

var builder = WebApplication.CreateBuilder(args);

#region Swagger

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddMvc();
builder.Services.AddMvc().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = c =>
    {
        var errors = c.ModelState.Values
            .Where(v => v.Errors.Count > 0)
            .SelectMany(v => v.Errors)
            .Select(v => v.ErrorMessage)
            .ToArray();

        return new BadRequestObjectResult(new
        {
            Message = errors
        });
    };
});

#endregion

//Add YAML
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(
        0,
        new YamlInputFormatter()
    );
});

builder.Services.AddRouting(options => { options.LowercaseUrls = true; });

//Add JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new PluginConfigurationJsonConverter());
    });

//Disable nullable validation
builder.Services.AddControllers(
    options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true
);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddSingleton(new SerializerBuilder().Build());
builder.Services.AddSingleton(new DeserializerBuilder().Build());

#region Versioning

builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver"));
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddVersionedApiExplorer(
    options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

#endregion

#region MongoDB Setup

// Register the IMongoDatabase instance for use in ApiKeyAuthorizationAttribute
builder.Services.AddSingleton<IMongoClient>(new MongoClient(Configuration.MongoDb.ConnectionString()));


// Adding services to DI container
builder.Services.AddScoped<IModelsService, ModelsService>();
builder.Services.AddTransient(provider => new Lazy<IModelsService>(provider.GetRequiredService<IModelsService>));
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddTransient(provider => new Lazy<IDataService>(provider.GetRequiredService<IDataService>));
builder.Services.AddScoped<IEmbeddingsService, EmbeddingsService>();
builder.Services.AddTransient(provider =>
    new Lazy<IEmbeddingsService>(provider.GetRequiredService<IEmbeddingsService>));
builder.Services.AddScoped<IVectorStoresService, VectorStoresService>();
builder.Services.AddTransient(provider =>
    new Lazy<IVectorStoresService>(provider.GetRequiredService<IVectorStoresService>));
builder.Services.AddScoped<IPromptsService, PromptsService>();
builder.Services.AddTransient(provider => new Lazy<IPromptsService>(provider.GetRequiredService<IPromptsService>));
builder.Services.AddScoped<IResponsesService, ResponsesService>();
builder.Services.AddTransient(provider => new Lazy<IResponsesService>(provider.GetRequiredService<IResponsesService>));
builder.Services.AddScoped<IPipelinesService, PipelinesService>();
builder.Services.AddTransient(provider => new Lazy<IPipelinesService>(provider.GetRequiredService<IPipelinesService>));

#endregion

// Adding use cases to DI container
builder.Services.AddScoped<IDataApplyUseCase, DataApplyUseCase>();
builder.Services.AddTransient(provider => new Lazy<IDataApplyUseCase>(provider.GetRequiredService<IDataApplyUseCase>));

builder.Services.AddScoped<IApplyPipelineUseCase, ApplyPipelineUseCase>();
builder.Services.AddTransient(provider =>
    new Lazy<IApplyPipelineUseCase>(provider.GetRequiredService<IApplyPipelineUseCase>()));

builder.Services.AddScoped<IRunPipelineUseCase, RunPipelineUseCase>();
builder.Services.AddTransient(provider =>
    new Lazy<IRunPipelineUseCase>(provider.GetRequiredService<IRunPipelineUseCase>()));

builder.Services.AddScoped<IGetPipelineUseCase, GetPipelineUseCase>();

builder.Services.AddSingleton<EventsManager>();
builder.Services.AddSingleton<IPluginLoader, PluginLoader>();

builder.Services.AddSingleton<IPluginManifestLoader, PluginManifestLoader>();
builder.Services.AddTransient(provider => new Lazy<IPluginManifestLoader>(provider.GetRequiredService<IPluginManifestLoader>));

builder.Services.AddScoped<IModelStageHandler, ModelStageHandler>();
builder.Services.AddScoped<IPromptStageHandler, PromptStageHandler>();

builder.Services.AddScoped<IPipelineCommitter, PipelineCommitter>();
builder.Services.AddTransient(provider => new Lazy<IPipelineCommitter>(provider.GetRequiredService<IPipelineCommitter>()));

builder.Services.AddScoped<IPipelineExecutor, PipelineExecutor>();
builder.Services.AddTransient(provider => new Lazy<IPipelineExecutor>(provider.GetRequiredService<IPipelineExecutor>()));

builder.Services.AddSingleton<IPluginContextCreator, PluginContextCreator>();

builder.Services.AddSingleton<FloomAssetsRepository>();
builder.Services.AddSingleton<FloomAuditService>();

//Add Dynamic Helpers
// builder.Services.AddScoped<IDynamicHelpersService, DynamicHelpersService>();

// builder.Services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddTransient<IRepositoryFactory, RepositoryFactory>();

// LLM Factory
builder.Services.AddScoped<ILLMFactory, LLMFactory>();
builder.Services.AddScoped(provider => new Lazy<ILLMFactory>(provider.GetRequiredService<ILLMFactory>));
builder.Services.AddScoped<OpenAiLLM>();
builder.Services.AddScoped<OpenAiClient>();
builder.Services.AddScoped<OllamaLLM>();
builder.Services.AddScoped<OllamaClient>();
builder.Services.AddScoped<LLamaLLM>();

// Embeddings Factory
builder.Services.AddScoped<OpenAiEmbeddings>();
builder.Services.AddScoped<OllamaEmbeddings>();

// Vector Stores Factory
builder.Services.AddScoped<Milvus>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.MapControllers();

var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
});

FloomLoggerFactory.Configure(loggerFactory);

app.Lifetime.ApplicationStarted.Register(() =>
{
    var repositoryFactory = app.Services.GetRequiredService<IRepositoryFactory>();
    
    // Generate Database
    var client = app.Services.GetRequiredService<IMongoClient>();
    var dbInitializer = new DbInitializer(client);
    dbInitializer.Initialize("Floom");

    // Generate Initial Api Key
    // var apiKeyInitializer = new ApiKeyInitializer(repositoryFactory);
    // apiKeyInitializer.Initialize();
    
    var pluginManifestLoader = app.Services.GetRequiredService<IPluginManifestLoader>();
    pluginManifestLoader.LoadAndUpdateManifestsAsync();
    
    // Get an instance of EventsManager
    var eventsManager = app.Services.GetService<EventsManager>();

    // Call the OnFloomStarts method
    eventsManager?.OnFloomStarts();
        
    // Generate Floom Assets Repository
    FloomAssetsRepository.Instance.Initialize(repositoryFactory);
    
    // Generate Floom Audit Service
    FloomAuditService.Instance.Initialize(repositoryFactory);
});

app.Run("http://*:80"); //port inside docker