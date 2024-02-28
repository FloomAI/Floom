using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Floom.Assets;
using Floom.Audit;
using Floom.Auth;
using Floom.Config;
using Floom.Events;
using Floom.Logs;
using Floom.Pipeline;
using Floom.Pipeline.StageHandler.Model;
using Floom.Pipeline.StageHandler.Prompt;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;
using Floom.Plugin.Manifest;
using Floom.Repository;
using Floom.Repository.DynamoDB;
using Floom.Repository.MongoDb;
using Floom.Server;
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
// builder.Services.AddTransient<PipelineDto.PipelineDtoValidator>();

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

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IMongoClient>(MongoConfiguration.CreateMongoClient());
builder.Services.AddSingleton<EventsManager>();

// Adding services to DI container
builder.Services.AddScoped<IPipelinesService, PipelinesService>();
builder.Services.AddTransient(provider => new Lazy<IPipelinesService>(provider.GetRequiredService<IPipelinesService>));

builder.Services.AddScoped<IUsersService, UsersService>();

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
app.UseMiddleware<DynamicApiRoutingMiddleware>();
var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
});

FloomLoggerFactory.Configure(loggerFactory);

app.Lifetime.ApplicationStarted.Register(FloomInitCallback);
Console.WriteLine("Starting app");
app.Run("http://*:4050"); //port inside docker
return;

async void FloomInitCallback()
{
    var repositoryFactory = app.Services.GetRequiredService<IRepositoryFactory>();

    // Generate Database
    var databaseType = Environment.GetEnvironmentVariable("FLOOM_DATABASE_TYPE");
    if (databaseType is "mongodb")
    {
        var client = app.Services.GetRequiredService<IMongoClient>();
        var dbInitializer = new MongoDbInitializer(client);
        await dbInitializer.Initialize("Floom");
    }
    else if (databaseType is "dynamodb")
    {
        var dynamoDbClient = DynamoDbConfiguration.CreateCloudDynamoDbClient();
        var dbInitializer = new DynamoDbInitializer(dynamoDbClient);
        await dbInitializer.Initialize();
    }

    // Generate Initial Api Key
    // var apiKeyInitializer = new ApiKeyInitializer(repositoryFactory);
    // apiKeyInitializer.Initialize();

    var pluginManifestLoader = app.Services.GetRequiredService<IPluginManifestLoader>();
    await pluginManifestLoader.LoadAndUpdateManifestsAsync();

    // Get an instance of EventsManager
    var eventsManager = app.Services.GetService<EventsManager>();

    // Call the OnFloomStarts method
    eventsManager?.OnFloomStarts();

    // Generate Floom Assets Repository
    FloomAssetsRepository.Instance.Initialize(repositoryFactory);

    // Generate Floom Audit Service
    FloomAuditService.Instance.Initialize(repositoryFactory);
}