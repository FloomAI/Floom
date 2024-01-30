using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

public class ApiKeyAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
{
    private bool _authorizeController;
    private bool _authorizeMethod;

    public ApiKeyAuthorizationAttribute(bool authorizeController = true, bool authorizeMethod = true)
    {
        _authorizeController = authorizeController;
        _authorizeMethod = authorizeMethod;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!_authorizeController && !_authorizeMethod)
        {
            // If authorization is disabled for both controller and method, skip the check.
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("Api-Key", out var apiKeyValues))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var apiKey = apiKeyValues.FirstOrDefault(); // Convert StringValues to a regular string
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var mongoClient = context.HttpContext.RequestServices.GetRequiredService<IMongoClient>();
        var database = mongoClient.GetDatabase("Floom"); // Replace with your actual database name
        var apiKeyCollection = database.GetCollection<ApiKey>("api-keys");

        var apiKeyDocument = await apiKeyCollection.Find(a => a.Key == apiKey).FirstOrDefaultAsync();
        if (apiKeyDocument == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        //You can also perform additional checks or store the key details in the request context for future use.
        //For example:
        context.HttpContext.Items["API_KEY_DETAILS"] = apiKeyDocument;

        await Task.CompletedTask;
    }
}
