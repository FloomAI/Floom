using Floom.Auth;

namespace Floom.Utils;

public static class HttpContextHelper
{
    public static String? GetApiKeyFromHttpContext()
    {
        var httpContext = new HttpContextAccessor().HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        return (httpContext.Items.TryGetValue(ApiKeyAuthorizationAttribute.ApiKey,
                    out var apiKeyDetailsObj) &&
                apiKeyDetailsObj is ApiKeyEntity apiKeyDocument)
            ? apiKeyDocument.key
            : null;
    }
}