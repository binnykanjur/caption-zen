namespace CaptionZen.Shared.Services;

public interface ICaptionZenService {

    Task<IEnumerable<AIProviderInfo>> GetAvailableAiProvidersAsync(CancellationToken cancellationToken = default);
    
    Task<Guid?> GetDefaultAIProviderIdAsync(CancellationToken cancellationToken = default);
    Task SetDefaultAIProviderIdAsync(Guid? aiProviderId, CancellationToken cancellationToken = default);
    Task<AIProviderInfo?> GetDefaultAIProviderAsync(CancellationToken cancellationToken = default);
    Task SaveAIProviderInfoAsync(AIProviderInfo aiProviderInfo, bool? makeDefault, CancellationToken cancellationToken = default);

    Task SaveSettingAsync(AIProviderInfo aIServiceInfo, CancellationToken cancellationToken = default);

    Task<List<ChatInfo>> GetChatsAsync(CancellationToken cancellationToken = default);

    Task<ChatDetail?> GetChatByIdAsync(Guid chatId, CancellationToken cancellationToken = default);

    Task<ChatInfo> NewChatAsync(string videoUrl, CancellationToken cancellationToken = default);
    Task DeleteChatAsync(Guid chatId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<StreamingChatResponseUpdate> CompleteStreamingAsync(Guid chatId, CancellationToken cancellationToken = default);
}

public class ChatInfo {
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

public class ChatDetail : ChatInfo {
    public required byte[] Thumbnail { get; set; }
    public required string VideoUrl { get; set; }
    public List<ChatMessageItem> ChatMessages { get; set; } = new();
}

public class ChatMessageItem {
    public int Id { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public required string AiProviderName { get; set; }
    public ChatMessageRole Role { get; set; }
}

public class AIProviderInfo {

    public AIProviderInfo(Guid id, AIVendor provider, string name, bool isEndpointRequired, bool isApiKeyRequired, bool isModelIdRequired) {
        Id = id;
        Provider = provider;
        Name = name;
        IsEndpointRequired = isEndpointRequired;
        IsApiKeyRequired = isApiKeyRequired;
        IsModelIdRequired = isModelIdRequired;
    }

    public Guid Id { get; }
    public AIVendor Provider { get; }
    public string Name { get; }

    public bool IsEndpointRequired { get; }
    public string? Endpoint { get; set; }
    public string? EndpointHintText { get; set; }

    public bool IsApiKeyRequired { get; }
    public string? ApiKey { get; set; }
    public string? ApiKeyHintText { get; set; }

    public bool IsModelIdRequired { get; }
    public string? ModelId { get; set; }
    public string? ModelIdHintText { get; set; }

    public string? HelpText { get; set; }
    public required string GetStartedText { get; set; }
    public required string GetStartedUrl { get; set; }

    //public override string ToString() {
    //    return Name;
    //}
}

public class StreamingChatResponseUpdate {
    public required string Text { get; set; }
}

public class AIProviderNotConfiguredException : Exception {
    public AIProviderNotConfiguredException() : base("No default AI provider configured or Invalid AI provider configuration") { }
}