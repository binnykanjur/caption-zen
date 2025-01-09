using Azure.AI.Inference;
using Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using Azure.AI.OpenAI;
using Azure.Identity;
using System.Text;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Runtime.CompilerServices;

namespace CaptionZen.Shared.Services;

internal class DefaultCaptionZenService : ICaptionZenService {

    private readonly CaptionZenDbContext _dbContext;
    private readonly ISettingsService _settingsService;
    private readonly IYouTubeService _youTubeService;
    private readonly HttpClient _httpClient;
    
    public DefaultCaptionZenService(CaptionZenDbContext dbContext, ISettingsService settingsService, IYouTubeService youTubeService, HttpClient httpClient) {

        _dbContext = dbContext;
        _settingsService = settingsService;
        _youTubeService = youTubeService;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<AIProviderInfo>> GetAvailableAiProvidersAsync(CancellationToken cancellationToken = default) {
        List<AIProviderInfo> aiProviderInfos = new();

        var aiProviders = await _dbContext.Set<AiProvider>().ToListAsync().ConfigureAwait(false);
        foreach (var aiProvider in aiProviders) {
            aiProviderInfos.Add(await GetAIProviderInfo(aiProvider).ConfigureAwait(false));
        }

        return aiProviderInfos.AsEnumerable();
    }

    public Task<Guid?> GetDefaultAIProviderIdAsync(CancellationToken cancellationToken = default) {
        return _settingsService.GetDefaultAiProviderIdAsync(cancellationToken);
    }

    public Task SetDefaultAIProviderIdAsync(Guid? aiProviderId, CancellationToken cancellationToken = default) {
        return _settingsService.SetDefaultAiProviderIdAsync(aiProviderId, cancellationToken);
    }

    public async Task SaveSettingAsync(AIProviderInfo aiProviderInfo, CancellationToken cancellationToken = default) {
        var modifiedOn = DateTimeOffset.Now;
        var aiProvider = await _dbContext.AiProviders.FirstAsync(ai => ai.Id == aiProviderInfo.Id, cancellationToken).ConfigureAwait(false);
        aiProvider.Endpoint = aiProviderInfo.Endpoint;
        aiProvider.ModelId = aiProviderInfo.ModelId;
        aiProvider.ModifiedOn = modifiedOn;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _settingsService.SetApiKeyAsync(aiProviderInfo.Id, aiProviderInfo.ApiKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AIProviderInfo?> GetDefaultAIProviderAsync(CancellationToken cancellationToken = default) {
        var aiProviderId = await GetDefaultAIProviderIdAsync(cancellationToken).ConfigureAwait(false);
        if(aiProviderId == null) return null;

        var aiProvider = await _dbContext.AiProviders.FindAsync(aiProviderId, cancellationToken).ConfigureAwait(false);
        if (aiProvider is null) return null;

        return await GetAIProviderInfo(aiProvider).ConfigureAwait(false);
    }

    public async Task SaveAIProviderInfoAsync(AIProviderInfo aiProviderInfo, bool? makeDefault, CancellationToken cancellationToken = default) {
        var modifiedOn = DateTimeOffset.Now;
        await _dbContext.AiProviders.Where(e => e.Id == aiProviderInfo.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ai => ai.Endpoint, aiProviderInfo.Endpoint)
                .SetProperty(ai => ai.ModelId, aiProviderInfo.ModelId)
                .SetProperty(ai => ai.ModifiedOn, modifiedOn), cancellationToken).ConfigureAwait(false);

        if (makeDefault.HasValue && makeDefault.Value) {
            await SetDefaultAIProviderIdAsync(aiProviderInfo.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<List<ChatInfo>> GetChatsAsync(CancellationToken cancellationToken = default) {
        return await _dbContext.Set<Chat>().OrderByDescending(c => c.CreatedOn).Select(c => new ChatInfo {
            Id = c.Id,
            Title = c.Title,
            CreatedOn = c.CreatedOn
        }).ToListAsync().ConfigureAwait(false);
    }

    public async Task<ChatDetail?> GetChatByIdAsync(Guid chatId, CancellationToken cancellationToken = default) {
        var chat = await _dbContext.Chats
            .Include(c => c.ChatMessages)
            .ThenInclude(cm => cm.AiProvider)
            .FirstOrDefaultAsync(e => e.Id == chatId);
        if (chat == null) return null;

        var chatMessages = chat.ChatMessages.Select(cm => new ChatMessageItem {
            Id = cm.Id,
            Message = cm.Message,
            AiProviderName = cm.AiProvider.Name,
            CreatedOn = cm.CreatedOn,
            Role = cm.Role
        }).ToList();
        return new ChatDetail {
            Id = chatId,
            Title = chat.Title,
            Thumbnail = chat.Thumbnail,
            VideoUrl = chat.VideoUrl,
            CreatedOn = chat.CreatedOn,
            ChatMessages = chatMessages
        };
    }

    public async Task<ChatInfo> NewChatAsync(string videoUrl, CancellationToken cancellationToken = default) {
        //Make sure an AI Provider is configured before proceeding
        var chatClient = await GetChatClientAsync(cancellationToken).ConfigureAwait(false);
        var videoDetail = await _youTubeService.GetVideoDetailsAsync(videoUrl, cancellationToken).ConfigureAwait(false);
        if (videoDetail is null) {
            throw new ArgumentException("Invalid Video");
        }
        
        var transcript = await _youTubeService.GetTranscriptAsync(videoUrl, cancellationToken).ConfigureAwait(false);
        if (transcript is null) {
            throw new ArgumentException("No transcript found");
        }

        EntityEntry<Chat>? chatEntityEntry = null;

        var defaultAIProvider = await GetDefaultAIProviderAsync(cancellationToken).ConfigureAwait(false);

        var chatMessages = await ToChatMessagesAsync(transcript).ConfigureAwait(false);
        var chat = new Chat {
            Id = Guid.NewGuid(),
            VideoUrl = videoUrl,
            Title = videoDetail.Title!,
            Description = videoDetail.Description,
            Thumbnail = await _httpClient.GetByteArrayAsync(videoDetail.ThumbnailUrl),
            CreatedOn = DateTimeOffset.Now
        };
        chatEntityEntry = await _dbContext.Chats.AddAsync(chat).ConfigureAwait(false);

        foreach (var message in chatMessages) {
            var chatMessage = new ChatMessage {
                Message = message.Text!,
                CreatedOn = DateTimeOffset.Now,
                Role = Helper.FromChatRole(message.Role),
                AiProviderId = defaultAIProvider!.Id,
                ModelId = defaultAIProvider.ModelId!,
                Chat = chatEntityEntry.Entity
            };
            await _dbContext.ChatMessages.AddAsync(chatMessage, cancellationToken).ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ChatInfo {
            Id = chatEntityEntry.Entity.Id,
            Title = videoDetail.Title!,
            CreatedOn = chat.CreatedOn
        };
    }

    public Task DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<StreamingChatResponseUpdate> CompleteStreamingAsync(Guid chatId, [EnumeratorCancellation] CancellationToken cancellationToken = default) {

        var chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.Id == chatId).ConfigureAwait(false);
        if (chat is null) throw new ArgumentException($"'Chat: {chatId}' not found");

        var chatMessages = await _dbContext.ChatMessages
            .OrderByDescending(cm => cm.CreatedOn)
            .Where(cm => cm.ChatId == chatId)
            .Select(cm => new Microsoft.Extensions.AI.ChatMessage(Helper.ToChatRole(cm.Role), cm.Message))
            .ToListAsync().ConfigureAwait(false);
        if (chatMessages.Count == 0) throw new ArgumentException($"'Invalid Chat state. No user message found");

        //Make sure an AI Provider is configured before proceeding
        var chatClient = await GetChatClientAsync(cancellationToken).ConfigureAwait(false);
        StringBuilder streamingChatResponseText = new();

        var defaultAIProvider = await GetDefaultAIProviderAsync(cancellationToken).ConfigureAwait(false);

        try {
            await foreach (var item in CompleteStreamingAsync(chatClient, chatMessages, cancellationToken: cancellationToken)) {

                if (cancellationToken.IsCancellationRequested) break;

                streamingChatResponseText.Append(item.Text!);
                yield return new StreamingChatResponseUpdate {
                    Text = item.Text!
                };
            }
        } finally {
            var response = new ChatMessage {
                Message = streamingChatResponseText.ToString(),
                CreatedOn = DateTimeOffset.Now,
                Role = ChatMessageRole.Assistant,
                AiProviderId = defaultAIProvider!.Id,
                ModelId = defaultAIProvider.ModelId!,
                Chat = chat
            };
            await _dbContext.ChatMessages.AddAsync(response, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IChatClient chatClient, List<Microsoft.Extensions.AI.ChatMessage> chatMessages, CancellationToken cancellationToken) {
#if DEBUG
        return chatClient.CompleteStreamingAsync(chatMessages, cancellationToken: cancellationToken);
        //return GetDummyStreamingMarkdown();
#else
        return chatClient.CompleteStreamingAsync(chatMessages, cancellationToken: cancellationToken);
#endif
    }

    private async Task<AIProviderInfo> GetAIProviderInfo(AiProvider aiProvider) {
        var apiKey = await _settingsService.GetApiKeyAsync(aiProvider.Id).ConfigureAwait(false);
        return new AIProviderInfo(aiProvider.Id, aiProvider.Vendor, aiProvider.Name, aiProvider.IsEndpointRequired, aiProvider.IsApiKeyRequired, aiProvider.IsModelIdRequired) {
            Endpoint = aiProvider.Endpoint,
            EndpointHintText = aiProvider.EndpointHintText,
            ApiKey = apiKey,
            ApiKeyHintText = aiProvider.ApiKeyHintText,
            ModelId = aiProvider.ModelId,
            ModelIdHintText = aiProvider.ModelIdHintText,
            GetStartedText = aiProvider.GetStartedText,
            GetStartedUrl = aiProvider.GetStartedUrl,
            HelpText = aiProvider.HelpText
        };
    }

    private async Task<IChatClient> GetChatClientAsync(CancellationToken cancellationToken = default) {
        var defaultAIProvider = await GetDefaultAIProviderAsync(cancellationToken).ConfigureAwait(false);
        if (defaultAIProvider is null) throw new AIProviderNotConfiguredException();

        if ((defaultAIProvider.IsEndpointRequired && string.IsNullOrWhiteSpace(defaultAIProvider.Endpoint)) ||
            (defaultAIProvider.IsApiKeyRequired && string.IsNullOrWhiteSpace(defaultAIProvider.ApiKey)) ||
            (defaultAIProvider.IsModelIdRequired && string.IsNullOrWhiteSpace(defaultAIProvider.ModelId))) {
            throw new AIProviderNotConfiguredException();
        }

        switch (defaultAIProvider.Provider) {
            case AIVendor.AzureAIInference:
                return new ChatCompletionsClient(
                    new Uri(defaultAIProvider.Endpoint!),
                    new AzureKeyCredential(defaultAIProvider.ApiKey!))
                    .AsChatClient(defaultAIProvider.ModelId);
            case AIVendor.AzureOpenAI:
                return new AzureOpenAIClient(new Uri(defaultAIProvider.Endpoint!),
                        new DefaultAzureCredential())
                   .AsChatClient(defaultAIProvider.ModelId!);
            case AIVendor.Ollama:
                return new OllamaChatClient(new Uri(defaultAIProvider.Endpoint!), defaultAIProvider.ModelId!);
            case AIVendor.OpenAI:
                return new OpenAIClient(defaultAIProvider.ApiKey!)
                    .AsChatClient(defaultAIProvider.ModelId!);
            default:
                throw new NotSupportedException("AI Provider not supported");
        }

    }

    private async Task<IEnumerable<Microsoft.Extensions.AI.ChatMessage>> ToChatMessagesAsync(string transcript) {
        const char LF = '\n';
        var systemPrompt = await Helper.ReadAllTextAsync("extract_wisdom-system.md");
        var prompt = $"{systemPrompt} {transcript}";
        return [new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.User, prompt)];
    }

#if DEBUG

    private async IAsyncEnumerable<StreamingChatCompletionUpdate> GetDummyStreamingMarkdown() {
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("# Welcome to the Markdown Renderer\n")]
        };
        await Task.Delay(500);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("## Features\n")]
        };
        await Task.Delay(500);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("The Markdown renderer supports:\n\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("- **Bold** text\n")]
        };
        await Task.Delay(300);
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("- *Italic* text\n")]
        };
        await Task.Delay(300);
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("- ***Bold and Italic*** text\n")]
        };
        await Task.Delay(300);
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("- Lists with nested items:\n  - Item 1\n    - Sub-item A\n    - Sub-item B\n")]
        };
        await Task.Delay(500);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("\n## Code Block Example\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("Here’s a sample of a C# code block:\n\n")]
        };
        await Task.Delay(500);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("```csharp\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("public class Example\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("{\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("    public void SayHello()\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("    {\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("        Console.WriteLine(\"Hello, World!\");\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("    }\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("}\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("```\n")]
        };
        await Task.Delay(700);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("\n## Table Example\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("| Feature       | Description                           |\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("|---------------|---------------------------------------|\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("| **Bold**      | Highlights important text             |\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("| *Italic*      | Emphasizes text                       |\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("| `Code`        | Renders inline code                   |\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("| Nested Lists  | Supports lists within lists           |\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("| Tables        | Organizes data in rows and columns    |\n")]
        };
        await Task.Delay(700);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("\n## Links and Images\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("For more details, visit the [official documentation](https://example.com).\n")]
        };
        await Task.Delay(500);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("Here's an example image:\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("![Sample Image](https://via.placeholder.com/150)\n")]
        };
        await Task.Delay(500);

        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("\n---\n\n")]
        };
        yield return new StreamingChatCompletionUpdate() {
            Contents = [new TextContent("End of the Markdown sample.")]
        };
    }

#endif

}