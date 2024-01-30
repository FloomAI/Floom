using System.Text.Json;
using System.Text.Json.Serialization;
using Floom.Audit;
using Floom.Config;
using Floom.Data;
using Floom.Embeddings.Ollama;
using Floom.Embeddings.OpenAi;
using Floom.Events;
using Floom.Logs;
using Floom.Misc;
using Floom.Model.LLama;
using Floom.Model.LLamaCpp;
using Floom.Model.OpenAi;
using Floom.Pipeline;
using Floom.Pipeline.Prompt;
using Floom.Pipeline.StageHandler.Model;
using Floom.Plugin;
using Floom.Repository;
using Floom.Utils;
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

builder.Services.AddSingleton<IMongoClient>(new MongoClient(MongoConfiguration.ConnectionString()));
builder.Services.AddSingleton<EventsManager>();

// Adding services to DI container
builder.Services.AddScoped<IPipelinesService, PipelinesService>();
builder.Services.AddTransient(provider => new Lazy<IPipelinesService>(provider.GetRequiredService<IPipelinesService>));

builder.Services.AddSingleton<IPluginLoader, PluginLoader>();
builder.Services.AddSingleton<IPluginContextCreator, PluginContextCreator>();

builder.Services.AddSingleton<IPluginManifestLoader, PluginManifestLoader>();
builder.Services.AddTransient(provider => new Lazy<IPluginManifestLoader>(provider.GetRequiredService<IPluginManifestLoader>));

builder.Services.AddScoped<IPipelineCommitter, PipelineCommitter>();
builder.Services.AddTransient(provider => new Lazy<IPipelineCommitter>(provider.GetRequiredService<IPipelineCommitter>()));

builder.Services.AddScoped<IPipelineExecutor, PipelineExecutor>();
builder.Services.AddTransient(provider => new Lazy<IPipelineExecutor>(provider.GetRequiredService<IPipelineExecutor>()));

builder.Services.AddScoped<IModelStageHandler, ModelStageHandler>();
builder.Services.AddScoped<IPromptStageHandler, PromptStageHandler>();

builder.Services.AddSingleton<FloomAssetsRepository>();
builder.Services.AddSingleton<FloomAuditService>();

builder.Services.AddTransient<IRepositoryFactory, RepositoryFactory>();

builder.Services.AddScoped<LLamaCppClient>();

// Embeddings Factory
builder.Services.AddScoped<OpenAiEmbeddings>();
builder.Services.AddScoped<OllamaEmbeddings>();

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