namespace CaptionZen.Shared.Components;

public interface INavigationView {
    bool IsPaneOpen { get; }
    bool UseCompactPane { get; }
    public bool IsOverlay { get; }
    Task TogglePaneAsync();
}

public enum NavigationViewPaneMode {
    Extended,
    Compact,
    Minimal
}

public record NavigationViewPaneState(bool CompactPaneMode, bool Overlay, bool PaneOpen);