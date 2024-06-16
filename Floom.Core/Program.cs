using System.Text.Json;
using Floom.Assets;
using Floom.Audit;
using Floom.Auth;
using Floom.Config;
using Floom.Logs;
using Floom.Repository;
using Floom.Repository.DynamoDB;
using Floom.Repository.MongoDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Floom.Server;

var builder = WebApplication.CreateBuilder(args);


// #region Swagger

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
// builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
// builder.Services.AddMvc();
// builder.Services.AddMvc().ConfigureApiBehaviorOptions(options =>
// {
//     options.InvalidModelStateResponseFactory = c =>
//     {
//         var errors = c.ModelState.Values
//             .Where(v => v.Errors.Count > 0)
//             .SelectMany(v => v.Errors)
//             .Select(v => v.ErrorMessage)
//             .ToArray();

//         return new BadRequestObjectResult(new
//         {
//             Message = errors
//         });
//     };
// });

// #endregion

//Add YAML
// builder.Services.AddControllers(options =>
// {
//     options.InputFormatters.Insert(
//         0,
//         new YamlInputFormatter()
//     );
// });

builder.Services.AddRouting(options => { options.LowercaseUrls = true; });

// //Add JSON
// builder.Services.AddControllers()
//     .AddJsonOptions(options =>
//     {
//         options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
//         options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
//         options.JsonSerializerOptions.Converters.Add(new PluginConfigurationJsonConverter());
//     });

// //Disable nullable validation
// builder.Services.AddControllers(
//     options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true
// );

// builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// builder.Services.AddTransient<PipelineDto.PipelineDtoValidator>();

// builder.Services.AddFluentValidationAutoValidation();
// builder.Services.AddSingleton(new SerializerBuilder().Build());
// builder.Services.AddSingleton(new DeserializerBuilder().Build());

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

// builder.Services.AddVersionedApiExplorer(
//     options =>
//     {
//         options.GroupNameFormat = "'v'VVV";
//         options.SubstituteApiVersionInUrl = true;
//     });

// #endregion

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IMongoClient>(MongoConfiguration.CreateMongoClient());

// // Adding services to DI container
builder.Services.AddScoped<IFunctionsService, FunctionsService>();
builder.Services.AddTransient(provider => new Lazy<IFunctionsService>(provider.GetRequiredService<IFunctionsService>));

builder.Services.AddScoped<IUsersService, UsersService>();


builder.Services.AddSingleton<FloomAssetsRepository>();
builder.Services.AddSingleton<FloomAuditService>();

builder.Services.AddTransient<IRepositoryFactory, RepositoryFactory>();

// new
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Configure CORS to allow requests from http://localhost:3000
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});


// Configure Google authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    IConfigurationSection googleAuthSection = builder.Configuration.GetSection("GoogleAuth");
    options.ClientId = googleAuthSection["ClientId"];
    options.ClientSecret = googleAuthSection["ClientSecret"];
})
.AddOAuth("GitHub", options =>
{
    IConfigurationSection githubAuthSection = builder.Configuration.GetSection("GitHubAuth");
    options.ClientId = githubAuthSection["ClientId"];
    options.ClientSecret = githubAuthSection["ClientSecret"];
    options.CallbackPath = new PathString("/signin-github");
    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
    options.UserInformationEndpoint = "https://api.github.com/user";

    options.Scope.Add("user:email");

    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

    options.SaveTokens = true;

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            context.RunClaimActions(user.RootElement);
            
            // Fetch the email address
            if (context.Principal != null && context.Principal.Identity is ClaimsIdentity identity)
            {
                var emailClaim = identity.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                    emailRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    emailRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

                    var emailResponse = await context.Backchannel.SendAsync(emailRequest, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                    emailResponse.EnsureSuccessStatusCode();

                    var emails = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync());
                    var email = emails.RootElement.EnumerateArray().FirstOrDefault(e => e.GetProperty("primary").GetBoolean()).GetProperty("email").GetString();

                    if (email != null)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, email));
                    }
                }
            }
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors(); // Add CORS middleware
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

    // Generate Floom Assets Repository
    FloomAssetsRepository.Instance.Initialize(repositoryFactory);

    // Generate Floom Audit Service
    FloomAuditService.Instance.Initialize(repositoryFactory);
}