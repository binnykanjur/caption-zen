using System.Text.RegularExpressions;
using Google.Apis.YouTube.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Text;

namespace CaptionZen.Shared.Services;

/// <summary>
/// Set up Google OAuth 2.0 credentials
/// ==============================================================
/// 1. Go to Google Cloud Console (https://console.cloud.google.com/)
/// 2. Create a new project
/// 3. Enable the YouTube Data API v3
/// 4. Create OAuth 2.0 credentials
/// 5. Download the credentials JSON file
/// </summary>
internal class GoogleYouTubeService : IYouTubeService {

    private readonly ISettingsService _settingsService;
    public GoogleYouTubeService(ISettingsService settingsService) {
        _settingsService = settingsService;
    }

    public async Task<string?> GetTranscriptAsync(string videoUrl, CancellationToken cancellationToken = default) {
        var videoId = GetVideoId(videoUrl);
        if (string.IsNullOrEmpty(videoId)) {
            throw new ArgumentException("Invalid Video Url");
        }

        var youtubeService = await GetYouTubeServiceAsync();
        var captionRequest = youtubeService.Captions.List("snippet", videoId);
        var captionResponse = await captionRequest.ExecuteAsync();

        if (captionResponse.Items == null || !captionResponse.Items.Any()) {
            return null;
        }

        // Prioritize English captions if available, otherwise take the first available
        var captionTrack = captionResponse.Items
            .FirstOrDefault(c => c.Snippet.Language == "en")
            ?? captionResponse.Items.First();

        var captionTrackId = captionTrack.Id;
        var downloadRequest = youtubeService.Captions.Download(captionTrackId);
        using var stream = await downloadRequest.ExecuteAsStreamAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<VideoDetails?> GetVideoDetailsAsync(string videoUrl, CancellationToken cancellationToken = default) {
        var videoId = GetVideoId(videoUrl);
        if (string.IsNullOrEmpty(videoId)) {
            throw new ArgumentException("Invalid Video Url");
        }

        var youtubeService = await GetYouTubeServiceAsync();
        var detailRequest = youtubeService.Videos.List("snippet,contentDetails");
        detailRequest.Id = videoId;
        var detailResponse = await detailRequest.ExecuteAsync();
        var video = detailResponse.Items.FirstOrDefault();
        if (video is null) {
            return null; //Video not found
        }

        var videoThumbnailUrl = video.Snippet.Thumbnails.Default__;
        return new VideoDetails {
            VideoId = videoId,
            CreatedOn = video.Snippet.PublishedAtDateTimeOffset!.Value,
            Title = video.Snippet.Title,
            Description = video.Snippet.Description,
            ThumbnailUrl = videoThumbnailUrl?.Url,
            Url = videoUrl
        };
    }

    public string? GetVideoId(string videoUrl) {
        var videoIdMatch = Regex.Match(videoUrl, @"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");
        return videoIdMatch.Success ? videoIdMatch.Groups[1].Value : null;
    }

    private YouTubeService? _youtubeService;
    private async Task<YouTubeService> GetYouTubeServiceAsync() {
        if (_youtubeService is null) {
            var json = await _settingsService.GetAsync("google_cred_json");
            UserCredential credential;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json!))) {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    [YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeForceSsl],
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            _youtubeService = new YouTubeService(new BaseClientService.Initializer {
                HttpClientInitializer = credential,
                ApplicationName = "CaptionZen"
            });
        }

        return _youtubeService;
    }
}