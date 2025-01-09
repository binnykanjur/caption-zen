using CaptionZen.Maui.Services;
using CaptionZen.Shared;
using Microsoft.Extensions.Logging;

namespace CaptionZen.Maui;

public static class MauiProgram {

    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddHttpClient();
        builder.Services.AddCaptionZenServices<SettingsService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        app.Services.SeedDatabase();

        return app;
    }
}
