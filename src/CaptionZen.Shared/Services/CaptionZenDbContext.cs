using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel.DataAnnotations;

namespace CaptionZen.Shared.Services;

internal class CaptionZenDbContext : DbContext {

    private const string AZURE_AI_INFERENCE_ID = "316ca9b2-37f5-403b-8cf1-2026ae51cd36";
    private const string AZURE_OPEN_AI_ID = "a2deb243-d5e8-419a-bdac-2b48e53d588d";
    private const string OLLAMA_ID = "d59aad1d-a365-4f85-81fb-9052dfeb6395";
    private const string OPEN_AI_ID = "2d12f7d2-6fe7-47c2-a57b-f13ef63b4dda";

    public required DbSet<AiProvider> AiProviders { get; set; }
    public required DbSet<Setting> Settings { get; set; }
    public required DbSet<Chat> Chats { get; set; }
    public required DbSet<ChatMessage> ChatMessages { get; set; }

    public CaptionZenDbContext(DbContextOptions<CaptionZenDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<AiProvider>().ToTable(nameof(AiProvider));
        modelBuilder.Entity<Setting>().ToTable(nameof(Setting));
        modelBuilder.Entity<Chat>().ToTable(nameof(Chat));

        modelBuilder.Entity<ChatMessage>().ToTable(nameof(ChatMessage));
        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.Chat)
            .WithMany(c => c.ChatMessages)
            .HasForeignKey(cm => cm.ChatId);
        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.AiProvider)
            .WithMany(a => a.ChatMessages)
            .HasForeignKey(cm => cm.AiProviderId);

        //https://blog.dangl.me/archive/handling-datetimeoffset-in-sqlite-with-entity-framework-core/
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite") {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                            || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties) {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }

        modelBuilder.Entity<AiProvider>().HasData([
            new AiProvider {
                Id = new Guid(AZURE_AI_INFERENCE_ID),
                Vendor = AIVendor.AzureAIInference,
                Name = "Azure AI Inference (GitHub Models)",
                IsEndpointRequired = true,
                IsApiKeyRequired =  true,
                IsModelIdRequired= true,
                EndpointHintText = "https://models.inference.ai.azure.com",
                ApiKeyHintText = "GitHub Token",
                ModelIdHintText = "Phi-3.5-MoE-instruct",
                HelpText = "This key is stored locally and only used to make API requests from this tool.",
                GetStartedText = "Get details on GitHub Models here.",
                GetStartedUrl = "https://docs.github.com/en/github-models/prototyping-with-ai-models#experimenting-with-ai-models-using-the-api",
                ModifiedOn = DateTimeOffset.Now
            },
            new AiProvider{
                Id = new Guid(AZURE_OPEN_AI_ID),
                Vendor = AIVendor.AzureOpenAI,
                Name = "Azure OpenAI",
                IsEndpointRequired = true,
                IsApiKeyRequired = false,
                IsModelIdRequired = true,
                EndpointHintText = "Azure OpenAI Endpoint",
                ModelIdHintText = "gpt-4o-mini",
                GetStartedText = "Get started.",
                GetStartedUrl = "https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal",
                ModifiedOn = DateTimeOffset.Now
            },
            new AiProvider{
                Id = new Guid(OLLAMA_ID),
                Vendor = AIVendor.Ollama,
                Name = "Ollama",
                IsEndpointRequired = true,
                IsApiKeyRequired = false,
                IsModelIdRequired = true,
                EndpointHintText = "http://localhost:11434/",
                ModelIdHintText = "phi3.5",
                HelpText = "Ollama allows you to run models locally on your computer.",
                GetStartedText = "Quickstart guide",
                GetStartedUrl = "https://github.com/ollama/ollama/blob/main/README.md",
                ModifiedOn = DateTimeOffset.Now
            },
            new AiProvider{
                Id = new Guid(OPEN_AI_ID),
                Vendor = AIVendor.OpenAI,
                Name = "OpenAI",
                IsEndpointRequired = false,
                IsApiKeyRequired = true,
                IsModelIdRequired = true,
                ApiKeyHintText = "OpenAI API Key",
                ModelIdHintText = "gpt-4o-mini",
                HelpText = "This key is stored locally and only used to make API requests from this tool.",
                GetStartedText = "You can get an OpenAI API key by signing up here.",
                GetStartedUrl = "https://platform.openai.com/api-keys",
                ModifiedOn = DateTimeOffset.Now
            }]);
    }

}

public class AiProvider {
    public Guid Id { get; set; }
    public AIVendor Vendor { get; set; }
    [Required]
    public required string Name { get; set; }

    public bool IsEndpointRequired { get; set; }
    public string? Endpoint { get; set; }
    public string? EndpointHintText { get; set; }

    public bool IsApiKeyRequired { get; set; }
    public string? ApiKeyHintText { get; set; }

    public bool IsModelIdRequired { get; set; }
    public string? ModelId { get; set; }
    public string? ModelIdHintText { get; set; }

    public string? HelpText { get; set; }
    [Required]
    public required string GetStartedText { get; set; }
    [Required]
    public required string GetStartedUrl { get; set; }

    public DateTimeOffset? ModifiedOn { get; set; }

    public ICollection<ChatMessage> ChatMessages { get; set; } = null!;
}

public enum AIVendor : byte {
    AzureAIInference = 1,
    AzureOpenAI,
    Ollama,
    OpenAI,
    OpenAICompatible
}

public class Setting {
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    public string? Value { get; set; }
    public bool Sensitive { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
}

public class Chat {
    public Guid Id { get; set; }
    [Required]
    public required string VideoUrl { get; set; }
    [Required]
    public required string Title { get; set; }
    public string? Description { get; set; }
    [Required]
    public required byte[] Thumbnail { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public ICollection<ChatMessage> ChatMessages { get; set; } = null!;
}

public class ChatMessage {
    public int Id { get; set; }
    [Required]
    public string Message { get; set; } = null!;
    public DateTimeOffset CreatedOn { get; set; }
    
    public ChatMessageRole Role { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public Guid AiProviderId { get; set; }
    public AiProvider AiProvider { get; set; } = null!;
    [Required]
    public string ModelId { get; set; } = null!;
}

public enum ChatMessageRole : byte {
    System = 1,
    User,
    Assistant
}