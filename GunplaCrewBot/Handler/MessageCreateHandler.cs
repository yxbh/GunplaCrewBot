namespace GunplaCrewBot.Handler;

using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using GunplaCrewBot.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Context;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using OpenAI.Chat;

public class MessageCreateHandler(
    RestClient restClient,
    GatewayClient gatewayClient,
    AzureOpenAIClient azureOpenAIClient,
    IKernelMemory kernelMemory,
    IOptions<MemoryConfig> memoryConfig,
    ILogger<MessageCreateHandler> logger
    )
    : IMessageCreateGatewayHandler
{
    public async ValueTask HandleAsync(Message message)
    {
        logger.LogInformation("{Content}", message.Content);

        // Ignore any bot authored messages
        if (message.Author.IsBot)
            return;

        // Consider bot mentioned if any mentioned user is a bot (assumes only this bot is mentioned or acceptable heuristic)
        bool thisBotMentioned = message.MentionedUsers.Any(u => u.Id == gatewayClient.Id);

        if (!thisBotMentioned)
        {
            foreach (var user in message.MentionedUsers)
                logger.LogDebug("Mentioned: IsBot={IsBot}, GlobalName={GlobalName}, Username={Username}", user.IsBot, user.GlobalName, user.Username);
            return;
        }

        logger.LogInformation("Bot was mentioned in Channel {ChannelId} by User {UserId}", message.ChannelId, message.Author.Id);

        ///
        /// Some sent a message and mentioned the bot.
        /// Let's check if it's a question or just a casual mention.
        ///

        var chatClient = azureOpenAIClient.GetChatClient(memoryConfig.Value.Services.AzureOpenAIText.Deployment);
        var chatResult = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage("""
                    You are an agent specialised in assessing whether a given input query is a question or message contain intent of looking up information.
                    If it is the case, reply "INPUT QUERY IS A QUESTION", otherwise, reply "INPUT QUERY IS NOT A QUESTION".
                    """),
                new UserChatMessage($"Query:\n{message.Content}"),
            ])
            .ConfigureAwait(false);

        if (chatResult.Value.FinishReason == ChatFinishReason.ContentFilter)
        {
#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var requestContentFilterResult = chatResult.Value.GetRequestContentFilterResult();
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            logger.LogWarning("Content filter triggered: {RequestContentFilterResult}", requestContentFilterResult);
            return;
        }

        if (chatResult.Value.Content.First().Text.Contains("INPUT QUERY IS NOT A QUESTION"))
        {
            // Just a casual mention, reply with a friendly message

            try
            {
                await restClient.SendMessageAsync(message.ChannelId, new()
                    {
                        Content =
                        $"""
                        👋 Hi <@{message.Author.Id}>! You mentioned me. How can I help?

                        I am here to answer questions based on the wisdom of our gunpla family (previous messages) :)
                        """,
                    MessageReference = MessageReferenceProperties.Reply(message.Id),
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send reply message");
            }

            return;
        }

        ///
        /// From this point on, we assume that the user asked a question.
        ///

        var searchContext = new RequestContext();

        // Use a custom RAG prompt
        searchContext.SetArg(
            Constants.CustomContext.Rag.Prompt,
            """
            Facts:
            {{$facts}}
            ======
            You are a friendly, chill and safe bot used for answering questions within a Discord server focused on Gunpla building.

            You should only answer questions relating to:
            - gunpla building
            - plastic model kit building
            - gunpla and plastic model kit painting
            - other gunpla and plastic model kit adjacent topics
            
            Given only the timestamped facts above, provide a structured answer (limit response to 500 words MAX and ensure sentences are complete).
            Make sure to reference where the information come from in the form of `(<userID> in <channelID> on [timestamp], <link>)` which is a link to the original content.
            `<userID>` is the referenced message's author ID. This is NOT the same us the user name.
            `<channelID>` is the referenced message's channel ID.
            `[timestamp]` is the timestamp of the referenced message.
            `<link>` is the URL to the referenced message in the form of `https://discord.com/channels/<guildId>/<channelId>/<messageId>`.
            Omit information that are not available.
            
            <ExampleQuery>
            what thinner ratio should i use for mr hobby lacquer paint?
            </ExampleQuery>

            <ExampleResponse>
            The recommended thinner ratio for Mr. Hobby lacquer paint (Mr. Color) is typically 3 parts thinner to 1 part paint, as suggested on their website. (<@12345678> in <#12345678> on [29/10/2023 3:30:14 PM +00:00], https://discord.com/channels/<0000>/<0000>/<0000>)
            
            However, some users also use a 1:1 or 2:1 ratio depending on the specific paint and their airbrushing setup (<@12345678> in <#12345678> on [12/11/2024 1:30:14 PM +00:00], https://discord.com/channels/<0000>/<0000>/<1111>). It's best to test on a spoon first to find what works for you.
            </ExampleResponse>


            If you don't have sufficient information, reply with '{{$notFound}}'.
            Question: {{$input}}
            Answer:
            """
            );

        var memoryAnswer = await kernelMemory.AskAsync(
            message.Content,
            memoryConfig.Value.IndexName,
            context: searchContext
            )
            .ConfigureAwait(false);

        var botDiscordReplyContent = string.Empty;

        if (memoryAnswer.NoResult)
        {
            botDiscordReplyContent = $"""
                👋 Hi <@{message.Author.Id}>!

                I don't have an answer to your query due to "{memoryAnswer.NoResultReason}".

                **DISCLAIMER: The content in this message are AI generated.**
                """;
        }
        else
        {
            botDiscordReplyContent = $"""
                👋 Hi <@{message.Author.Id}>!

                {memoryAnswer.Result}

                **DISCLAIMER: The content in this message are AI generated. The response is grounded with relevant past messages from within this community. For any issues, please flag to your nearest gunplacrew member.**
                """;
        }

        try
        {
            await restClient.SendMessageAsync(message.ChannelId, new()
            {
                Content = botDiscordReplyContent,
                MessageReference = MessageReferenceProperties.Reply(message.Id),
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reply message");
        }
    }
}