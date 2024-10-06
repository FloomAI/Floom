using Floom.Assets;
using Floom.Audit;
using Floom.Auth;
using Floom.Config;
using Floom.Functions;
using Floom.Logs;
using Floom.Repository;
using Floom.Repository.DynamoDB;
using Floom.Repository.MongoDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.Cookies;
using Floom.Server;
using Microsoft.AspNetCore.HttpOverrides;

var allowedOrigins = new[] { "https://console.floom.ai", "http://localhost:3000", "https://www.floom.ai", "https://floom.ai" };

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers options
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Only loopback proxies are allowed by default.
    // Clear that restriction because forwarders are enabled by explicit configuration.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRouting(options => { options.LowercaseUrls = true; });

// #region Versioning
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
// #endregion

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMongoClient>(MongoConfiguration.CreateMongoClient());

// Adding services to DI container
builder.Services.AddScoped<IFunctionsService, FunctionsService>();
builder.Services.AddTransient(provider => new Lazy<IFunctionsService>(provider.GetRequiredService<IFunctionsService>));
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddSingleton<FloomAssetsRepository>();
builder.Services.AddSingleton<FloomAuditService>();
builder.Services.AddTransient<IRepositoryFactory, RepositoryFactory>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});

// add localhost:3000 to the list of allowed origins, also add www.floom.ai and floom.ai

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Determine the current environment
var environment = builder.Environment;
Console.WriteLine($"Floom Is Running On Environment: {environment.EnvironmentName}");

// Configure Google authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    if (environment.IsProduction())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }}
);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
Console.WriteLine("Handling request: " + context.Request.Method + " " + context.Request.Path);

    // Handle preflight requests
    if (context.Request.Method == "OPTIONS")
    {
        Console.WriteLine("Handling preflight request (OPTIONS)");
        var origin = context.Request.Headers["Origin"].ToString();
        if (allowedOrigins.Contains(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Api-Key";
        }

        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    if (!context.Response.HasStarted)
    {
        context.Response.OnStarting(() =>
        {
            Console.WriteLine("Adding CORS headers to response");

            var origin = context.Request.Headers["Origin"].ToString();
            if (allowedOrigins.Contains(origin))
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            return Task.CompletedTask;
        });
    }

    await next();
    Console.WriteLine("Finished handling request");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(); // Add CORS middleware before authentication and authorization
//app.UseHttpsRedirection();
app.UseAuthentication(); // Add authentication middleware
app.UseAuthorization();

app.MapControllers();
app.UseMiddleware<DynamicApiRoutingMiddleware>();

var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
});

FloomLoggerFactory.Configure(loggerFactory);

app.Lifetime.ApplicationStarted.Register(FloomInitCallback);
Console.WriteLine("Starting app");
app.Run("http://*:4050"); // port inside docker

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

    // Generate Floom Assets Repository
    FloomAssetsRepository.Instance.Initialize(repositoryFactory);

    // Generate Floom Audit Service
    FloomAuditService.Instance.Initialize(repositoryFactory);
}
