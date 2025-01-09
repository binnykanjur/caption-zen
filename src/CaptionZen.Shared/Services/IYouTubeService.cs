namespace CaptionZen.Shared.Services;

public interface IYouTubeService {
    string? GetVideoId(string videoUrl);
    Task<VideoDetails?> GetVideoDetailsAsync(string videoUrl, CancellationToken cancellationToken = default);
    Task<string?> GetTranscriptAsync(string videoUrl, CancellationToken cancellationToken = default);
}

public class VideoDetails {
    public required string VideoId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}