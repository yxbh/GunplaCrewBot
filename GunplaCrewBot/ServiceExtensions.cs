using Azure.AI.OpenAI;
using Azure.Identity;
using GunplaCrewBot.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;

namespace GunplaCrewBot;

internal static class ServiceExtensions
{    internal static IServiceCollection AddKernelMemory(
        this IServiceCollection services, MemoryConfig config)
    {
        var credential = new DefaultAzureCredential();

        services.AddKernelMemory<MemoryServerless>(builder =>
        {

            config.Services.AzureOpenAIText.SetCredential(credential);
            config.Services.AzureOpenAIEmbedding.SetCredential(credential);
            config.Services.AzureAISearch.SetCredential(credential);
            config.Services.AzureBlobs.SetCredential(credential);

            builder
                .WithAzureOpenAITextEmbeddingGeneration(config.Services.AzureOpenAIEmbedding)
                .WithAzureOpenAITextGeneration(config.Services.AzureOpenAIText)
                .WithAzureBlobsDocumentStorage(config.Services.AzureBlobs)
                .WithAzureAISearchMemoryDb(config.Services.AzureAISearch)
                ;

        });

        services.AddTransient<AzureOpenAIClient>(
            (_) => new AzureOpenAIClient(new Uri(config.Services.AzureOpenAIText.Endpoint), credential));

        return services;
    }
}
