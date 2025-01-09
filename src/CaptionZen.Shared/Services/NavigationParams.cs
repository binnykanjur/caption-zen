using CaptionZen.Shared.Components;

namespace CaptionZen.Shared.Services;

internal class NavigationParams {
    public Guid? NewChatId { get; set; }
    public string? ChatTitle { get; set; } //Will be used by the MainLayout Listbox to add the newly added chat
    public DateTimeOffset? ChatCreatedOn { get; set; }
    /// <summary>
    /// Set by NavigationView so that it could be accessed from any component
    /// </summary>
    public INavigationView? NavigationView { get; set; }
}