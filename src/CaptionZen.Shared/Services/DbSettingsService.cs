using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace CaptionZen.Shared.Services;

internal class DbSettingsService(CaptionZenDbContext dbContext, IOptions<EncryptionSettings> encryptionSettingsOptions) : ISettingsService {

    private const string DEFAULT_AI_SERVICE_ID_SETTING_KEY = "DefaultServiceId";
    private const string API_KEY_SUFFIX = "ApiKey";

    public async Task<string?> GetApiKeyAsync(Guid aiProviderId, CancellationToken cancellationToken = default) {
        var apiKeySettingsKey = $"{aiProviderId}_{API_KEY_SUFFIX}";
        var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Name == apiKeySettingsKey, cancellationToken);
        if (setting is null || setting.Value is null) {
            return null;
        }

        var encryptionSettings = encryptionSettingsOptions.Value;
        return setting.Value.Decrypt(Encoding.UTF8.GetBytes(encryptionSettings.Key!),
            Encoding.UTF8.GetBytes(encryptionSettings.IV!));
    }

    public async Task<Guid?> GetDefaultAiProviderIdAsync(CancellationToken cancellationToken = default) {
        var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Name == DEFAULT_AI_SERVICE_ID_SETTING_KEY, cancellationToken);
        if (setting is null || setting.Value is null) {
            return null;
        }

        return Guid.Parse(setting.Value);
    }

    public async Task SetApiKeyAsync(Guid aiProviderId, string? apiKey, CancellationToken cancellationToken = default) {
        var apiKeySettingsKey = $"{aiProviderId}_{API_KEY_SUFFIX}";
        var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Name == apiKeySettingsKey, cancellationToken);

        if (apiKey is not null) {
            var encryptionSettings = encryptionSettingsOptions.Value;
            apiKey = apiKey.Encrypt(Encoding.UTF8.GetBytes(encryptionSettings.Key!),
                Encoding.UTF8.GetBytes(encryptionSettings.IV!));
        }

        if (setting is null) {
            dbContext.Settings.Add(new Setting {
                Name = apiKeySettingsKey,
                Value = apiKey,
                ModifiedOn = DateTimeOffset.Now
            });
        } else {
            setting.Value = apiKey;
            setting.ModifiedOn = DateTimeOffset.Now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetDefaultAiProviderIdAsync(Guid? aiProviderId, CancellationToken cancellationToken = default) {
        var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Name == DEFAULT_AI_SERVICE_ID_SETTING_KEY, cancellationToken);
        if (setting is null) {
            dbContext.Settings.Add(new Setting {
                Name = DEFAULT_AI_SERVICE_ID_SETTING_KEY,
                Value = aiProviderId?.ToString() ?? null,
                ModifiedOn = DateTimeOffset.Now
            });
        } else {
            setting.Value = aiProviderId is null ? null : aiProviderId.ToString();
            setting.ModifiedOn = DateTimeOffset.Now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }

    public Task SetAsync(string key, string? value, CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }
}