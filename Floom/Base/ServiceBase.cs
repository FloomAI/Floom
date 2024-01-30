using Floom.Audit;

namespace Floom.Base;

public class ServiceBase
{
    protected readonly FloomAuditService _auditService;
    protected readonly ILogger _logger;
    protected IServiceProvider _serviceProvider;

    public IServiceProvider ServiceProvider => _serviceProvider;

    public ServiceBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _auditService = serviceProvider.GetRequiredService<FloomAuditService>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(GetType());
    }
}