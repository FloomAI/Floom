using Floom.Auth;

namespace Floom.Utils;

public static class HttpContextHelper
{
    public static string GetApiKeyFromHttpContext()
    {
        var httpContext = new HttpContextAccessor().HttpContext;
        
        if (httpContext == null)
        {
            return "";
        }

        var apiKeyEntity = httpContext.Items[ApiKeyAuthorizationAttribute.ApiKey];
        
        if(apiKeyEntity != null)
        {
            return ((ApiKeyEntity)apiKeyEntity).key;
        }

        return "";
    }
}