using Microsoft.Extensions.Logging;

namespace GunplaCrewBot.Config;


internal class LoggingConfig
{
    public ConsoleLoggingConfig Console { get; set; } = new();
}

internal class ConsoleLoggingConfig
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string TimestampFormat { get; set; } = "[yyyy-MM-dd HH:mm:ss.fff] ";
}
