using Microsoft.Extensions.Logging;

namespace GunplaCrewBot.Config;

internal class RootConfig
{
    public DiscordConfig Discord { get; set; } = new();

    public LoggingConfig Logging { get; set; } = new();

    public MemoryConfig KernelMemory { get; set; } = new();
}

internal class DiscordConfig
{
    public string Token { get; set; } = string.Empty;
}
