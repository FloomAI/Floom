namespace Floom.Logs;

public static class FloomLoggerFactory
{
    private static ILoggerFactory _loggerFactory;

    public static void Configure(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public static ILogger CreateLogger(Type type)
    {
        if (_loggerFactory == null)
        {
            throw new InvalidOperationException("Logger factory not configured. Call Configure() first.");
        }

        return _loggerFactory.CreateLogger(type);
    }
}
