using Floom.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using MongoDB.Driver;

namespace Floom.Helpers
{
    public class DynamicHelpers
    {
        private readonly IDatabaseService _db;

        public DynamicHelpers(IDatabaseService databaseService)
        {
            _db = databaseService;
        }

        public async Task LogAsync(LogType type = LogType.Information, string message = "", string info = "", HttpContext httpContext = null)
        {
            LogRow logRow = new LogRow()
            {
                createdAt = DateTime.UtcNow,
                //createdBy = (httpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null,
                message = message,
                info = info,
                type = type,
                url = httpContext.Request.GetDisplayUrl()
            };

            await _db.Log.InsertOneAsync(logRow);

            return;
        }

        public async Task AuditAsync(
            AuditAction action,
            string objectType,
            string objectId = "",
            string objectName = "", //Can be temporary
            string messageId = "",
            string chatId = "",
            Dictionary<string, object> attributes = null,
            HttpContext httpContext = null
        )
        {
            AuditRow auditRow = new AuditRow()
            {
                createdAt = DateTime.UtcNow,
                //createdBy = (httpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null,
                action = action,
                objectType = objectType,
                objectId = objectId,
                objectName = objectName,
                messageId = messageId,
                chatId = chatId
            };

            if (attributes != null)
            {
                auditRow.attributes = attributes;
            }

            await _db.Audit.InsertOneAsync(auditRow);

            return;
        }
    }
}
