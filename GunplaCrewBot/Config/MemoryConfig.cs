using Microsoft.KernelMemory;

namespace GunplaCrewBot.Config;

public class MemoryConfig
{
    public string IndexName { get; set; } = "default";

    public ServicesConfig Services { get; set; } = new();

    public class ServicesConfig
    {
        public AzureAISearchConfig AzureAISearch { get; set; } = new();

        public AzureOpenAIConfig AzureOpenAIEmbedding { get; set; } = new();

        public AzureOpenAIConfig AzureOpenAIText { get; set; } = new();

        public AzureBlobsConfig AzureBlobs { get; set; } = new();
    }
}
