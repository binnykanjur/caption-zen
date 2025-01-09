using CaptionZen.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Components.Tooltip;

namespace CaptionZen.Shared;

public static class ServiceCollectionExtensions {

    public static IServiceCollection AddCaptionZenServices(this IServiceCollection services) {
        return AddCaptionZenServices<DbSettingsService>(services);
    }

    public static IServiceCollection AddCaptionZenServices<TImplementation>(this IServiceCollection services) where TImplementation : class, ISettingsService {
        return services
            .AddMemoryCache()
            .AddSingleton<MarkdownService>()
            .AddTransient<IYouTubeService, ScraperYouTubeService>()
            .AddScoped<ICaptionZenService, DefaultCaptionZenService>()
            .AddScoped<NavigationParams>()
            .AddDbContext<CaptionZenDbContext>(options =>
                options.UseSqlite("Data Source=CaptionZen.sqlitedb"))
            .AddScoped<ISettingsService, TImplementation>()
            .AddScoped<ITooltipService, TooltipService>()
            .AddFluentUIComponents();
    }
    
    public static void SeedDatabase(this IServiceProvider serviceProvider) {
        using (var scope = serviceProvider.CreateScope()) {
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<CaptionZenDbContext>();
            dbContext.Database.EnsureCreated();
        }
    }

}