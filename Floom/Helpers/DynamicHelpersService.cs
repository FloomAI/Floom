// DynamicHelpersService.cs

using Floom.Models;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Floom.Helpers
{
    public interface IDynamicHelpersService
    {
        DynamicHelpers DynamicHelpers { get; }
        Task AuditAsync(
            AuditAction action, 
            string objectType, 
            string objectId = "", 
            string objectName = "",
            string messageId = "",
            string chatId = "",
            Dictionary<string, object> attributes = null, 
            HttpContext httpContext = null
        );
        Task LogAsync(
            LogType type = LogType.Information, 
            string message = "", 
            string info = "", 
            HttpContext httpContext = null
        );
    }

    public class DynamicHelpersService : IDynamicHelpersService
    {
        public DynamicHelpers DynamicHelpers { get; }

        public DynamicHelpersService(IDatabaseService databaseService)
        {
            DynamicHelpers = new DynamicHelpers(databaseService);
        }

        public async Task AuditAsync(
            AuditAction action,
            string objectType,
            string objectId = "",
            string objectName = "",
            string messageId = "",
            string chatId = "",
            Dictionary<string, object> attributes = null,
            HttpContext httpContext = null
        )
        {
            await DynamicHelpers.AuditAsync(action, objectType, objectId, objectName, messageId, chatId, attributes);
        }

        public async Task LogAsync(
            LogType type = LogType.Information,
            string message = "",
            string info = "",
            HttpContext httpContext = null)
        {
            await DynamicHelpers.LogAsync(type, message, info);
        }
    }
}
