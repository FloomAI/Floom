using Floom.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Floom.Auth;

public class ApiKeyAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string ApiKey = "API_KEY_DETAILS";
    private readonly bool _shouldEnforceAuthorization;

    public ApiKeyAuthorizationAttribute()
    {
        if(Environment.GetEnvironmentVariable("FLOOM_AUTHENTICATION") != null)
        {
            var useAuthentication = Environment.GetEnvironmentVariable("FLOOM_AUTHENTICATION");
            _shouldEnforceAuthorization = useAuthentication == "true";
        }
        else
        {
            _shouldEnforceAuthorization = false;
        }
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!_shouldEnforceAuthorization)
        {
            // If authorization is disabled skip the check.
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("Api-Key", out var apiKeyValues))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var repositoryFactory = context.HttpContext.RequestServices.GetRequiredService<IRepositoryFactory>();
        var repository = repositoryFactory.Create<ApiKeyEntity>();

        var apiKey = apiKeyValues.FirstOrDefault(); // Convert StringValues to a regular string
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var existingApiKey = await repository.Get(apiKey, "key");
        
        if (existingApiKey == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[ApiKey] = existingApiKey;
        
        await Task.CompletedTask;
    }
}