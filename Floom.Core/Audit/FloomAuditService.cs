using Floom.Base;
using Floom.Logs;
using Floom.Repository;
using Floom.Utils;

namespace Floom.Audit;

public class FloomAuditService : FloomSingletonBase<FloomAuditService>
{
    private readonly ILogger _logger;
    private IRepository<AuditRowEntity> _repository;

    public FloomAuditService()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    public override void Initialize(IRepositoryFactory repositoryFactory)
    {
        _repository = repositoryFactory.Create<AuditRowEntity>();
    }

    public async Task<IEnumerable<AuditRowEntity>> GetByChatId(string chatId)
    {
        _logger.LogInformation("GetByChatId");
        var models = await _repository.GetAll(chatId, "chatId");
        return models;
    }

    public void Insert(
        AuditAction action,
        string objectType,
        string objectId = "",
        string objectName = "",
        string messageId = "",
        string chatId = "",
        Dictionary<string, object>? attributes = null)
    {
        AuditRowEntity auditRowEntity = new AuditRowEntity()
        {
            action = action,
            createdAt = DateTime.UtcNow,
            objectType = objectType,
            objectId = objectId,
            objectName = objectName,
            messageId = messageId,
            chatId = chatId
        };
        
        auditRowEntity.AddCreatedByApiKey(HttpContextHelper.GetApiKeyFromHttpContext() ?? "");

        if (attributes != null)
        {
            auditRowEntity.attributes = attributes;
        }

        _repository.Insert(auditRowEntity);
    }
}