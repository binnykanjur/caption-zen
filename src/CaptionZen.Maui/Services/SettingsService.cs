using CaptionZen.Shared.Services;

namespace CaptionZen.Maui.Services;

internal class SettingsService : ISettingsService {

    private const string DEFAULT_AI_SERVICE_ID_SETTING_KEY = "DefaultServiceId";
    private const string API_KEY_SUFFIX = "ApiKey";

    public Task<string?> GetApiKeyAsync(Guid aiProviderId, CancellationToken cancellationToken = default) {
        var apiKeySettingsKey = $"{aiProviderId}_{API_KEY_SUFFIX}";
        return Task.FromResult(Preferences.Get(apiKeySettingsKey, null));
    }

    public Task<Guid?> GetDefaultAiProviderIdAsync(CancellationToken cancellationToken = default) {
        var id = Preferences.Get(DEFAULT_AI_SERVICE_ID_SETTING_KEY, null);
        return Task.FromResult(id is null ? (Guid?)null : Guid.Parse(id));
    }

    public Task SetApiKeyAsync(Guid aiProviderId, string? apiKey, CancellationToken cancellationToken = default) {
        var apiKeySettingsKey = $"{aiProviderId}_{API_KEY_SUFFIX}";
        Preferences.Set(apiKeySettingsKey, apiKey);
        return Task.CompletedTask;
    }

    public Task SetDefaultAiProviderIdAsync(Guid? aiProviderId, CancellationToken cancellationToken = default) {
        Preferences.Set(DEFAULT_AI_SERVICE_ID_SETTING_KEY, aiProviderId?.ToString());
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task SetAsync(string key, string? value, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

}