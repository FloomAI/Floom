using Floom.Misc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using YamlDotNet.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;
using FluentValidation;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Floom.Helpers;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

#region Auto Fluent Validation

//builder.Services.AddFluentValidationAutoValidation();

#endregion

#region Swagger

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddMvc();

#endregion

//Add YAML
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(
        0,
        new YamlInputFormatter(
            new DeserializerBuilder().IgnoreUnmatchedProperties().Build()
            )
        );
});

//Add JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddSingleton<ISerializer>(new SerializerBuilder().Build());
builder.Services.AddSingleton<IDeserializer>(new DeserializerBuilder().Build());

#region Versioning

builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver"));
});

builder.Services.AddVersionedApiExplorer(
    options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

#endregion

#region MongoDB Setup

// Read MongoDB connection string from environment variables
var mongoUser = Environment.GetEnvironmentVariable("DB_USER");
var mongoPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var mongoAddress = Environment.GetEnvironmentVariable("DB_ADDRESS");
var connectionString = "";

if (string.IsNullOrEmpty(mongoUser) && string.IsNullOrEmpty(mongoPassword) && string.IsNullOrEmpty(mongoAddress))
{
    mongoUser = "root";
    mongoPassword = "MyFloom";
    mongoAddress = "mongo:27017";
    connectionString = $"mongodb://{mongoUser}:{mongoPassword}@{mongoAddress}";
}
else
{
    connectionString = $"mongodb+srv://{mongoUser}:{mongoPassword}@{mongoAddress}";
}

// Build the MongoDB connection string

// Register the connection string in the configuration system
builder.Services.Configure<Floom.Misc.ConnectionStringSettings>(options =>
{
    options.MongoDB = connectionString;
});

// Register the IMongoDatabase instance for use in ApiKeyAuthorizationAttribute
builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));

//builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
//{
//    var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
//    var settings = mongoClient.Settings;
//    var databaseName = "Floom"; // Replace with your actual database name
//    return mongoClient.GetDatabase(databaseName);
//});

builder.Services.AddScoped<IDatabaseService>(provider =>
{
    var mongoClient = provider.GetRequiredService<IMongoClient>();
    var settings = mongoClient.Settings;
    var databaseName = "Floom"; // Replace with your actual database name
    return new DatabaseService(mongoClient, databaseName);
});

#endregion

#region Check Default MongoDB + VDB (if provided) connectivity

#endregion

//Disable nullable validation
builder.Services.AddControllers(
    options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true
);

//Add Dynamic Helpers
builder.Services.AddScoped<IDynamicHelpersService, DynamicHelpersService>();

var app = builder.Build();

//#region Generate Database

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var client = services.GetRequiredService<IMongoClient>(); // Resolving IMongoDatabase directly
//    //var database = services.GetRequiredService<IDatabaseService>();

//    var dbInitializer = new DBInitializer(client);
//    dbInitializer.Initialize("Floom");
//}

//#endregion

#region Generate Initial Api Key

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    //var database = services.GetRequiredService<IMongoDatabase>(); // Resolving IMongoDatabase directly
    var database = services.GetRequiredService<IDatabaseService>(); 

    var apiKeyInitializer = new ApiKeyInitializer(database);
    apiKeyInitializer.Initialize();
}

#endregion

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

//app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run("http://*:80"); //port inside docker
