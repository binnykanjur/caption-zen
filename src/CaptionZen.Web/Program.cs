using CaptionZen.Shared;
using CaptionZen.Web.Components;

internal class Program {

    private static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddHttpClient();
        builder.Services.AddCaptionZenServices();

        builder.Services.AddOptions<EncryptionSettings>()
            .BindConfiguration("Encryption")
            .Validate(encryptionSettings => {
                if (string.IsNullOrWhiteSpace(encryptionSettings.Key) ||
                    string.IsNullOrWhiteSpace(encryptionSettings.IV)) {
                    return false;
                }

                if (encryptionSettings.Key.Length != 16 || encryptionSettings.IV.Length != 16) return false;

                return true;
            })
            .ValidateOnStart();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(CaptionZen.Shared._Imports).Assembly);

        app.UseStatusCodePagesWithRedirects("/pages/{0}");

        app.Services.SeedDatabase();

        app.Run();
    }
}