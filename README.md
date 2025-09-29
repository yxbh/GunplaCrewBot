# GunplaCrewBot

Example project Discord bot that auto responds to mentions with RAG content using arbitrarly indexed data via [Microsoft.KernelMemory](https://github.com/microsoft/kernel-memory).

The Discord bot uses [NetCord](https://github.com/NetCordDev/NetCord).

## Setup

1. Update the appropriate appsettings.json with your Discord app token. See the [NetCord guide here](https://netcord.dev/guides/getting-started/making-a-bot.html?tabs=generic-host) for steps.
1. Setup your Azure resources. This is a good starting point: <https://learn.microsoft.com/en-us/azure/ai-foundry/openai/concepts/use-your-data>.
1. You will need the AzureCLI [installed locally](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest&pivots=winget) for the `az login` command.
