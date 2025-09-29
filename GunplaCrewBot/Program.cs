using GunplaCrewBot;
using GunplaCrewBot.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;


var builder = Host.CreateApplicationBuilder(args);

SetupConfig(builder);

var rootConfig = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<RootConfig>>().Value;

SetupLogging(builder, rootConfig.Logging);

builder.Services
    .AddDiscordGateway(options => {
        options.Token = rootConfig.Discord.Token;
        options.Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent;
    })
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddKernelMemory(rootConfig.KernelMemory)
    ;

var host = builder.Build();

await host.RunAsync();

static void SetupConfig(IHostApplicationBuilder appBuilder)
{
    // bind config to RootConfig class from appsettings.json and appsettings.Development.json
    appBuilder.Configuration
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{appBuilder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.UserName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        ;

    appBuilder.Services
        .Configure<RootConfig>(appBuilder.Configuration)
        .Configure<LoggingConfig>(appBuilder.Configuration.GetSection("Logging"))
        .Configure<MemoryConfig>(appBuilder.Configuration.GetSection("KernelMemory"))
        .Configure<DiscordConfig>(appBuilder.Configuration.GetSection("Discord"))
        ;
}

static void SetupLogging(IHostApplicationBuilder appBuilder, LoggingConfig loggingConfig)
{
    appBuilder.Logging.AddSimpleConsole(options =>
    {
        if (!string.IsNullOrEmpty(loggingConfig.Console.TimestampFormat))
        {
            options.TimestampFormat = loggingConfig.Console.TimestampFormat;
        }
    });

    appBuilder.Logging.SetMinimumLevel(loggingConfig.Console.LogLevel);
}