using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

public static class LoggerManager
{
    private static ILoggerFactory loggerFactory;

    public static ILoggerFactory LoggerFactory { get => loggerFactory ??= CreateTempLogger(); set => loggerFactory = value; }

    private static ILoggerFactory CreateTempLogger()
    {
        return Logging.LoggerFactory.Create(builder => builder.AddConsole().AddDebug());
    }
}
