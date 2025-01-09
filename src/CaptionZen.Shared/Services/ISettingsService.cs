namespace CaptionZen.Shared.Services;

public interface ISettingsService {
    Task<Guid?> GetDefaultAiProviderIdAsync(CancellationToken cancellationToken = default);
    Task SetDefaultAiProviderIdAsync(Guid? aiProviderId, CancellationToken cancellationToken = default);

    Task<string?> GetApiKeyAsync(Guid aiProviderId, CancellationToken cancellationToken = default);
    Task SetApiKeyAsync(Guid aiProviderId, string? apiKey, CancellationToken cancellationToken = default);

    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string? value, CancellationToken cancellationToken = default);
}