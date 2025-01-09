using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using YoutubeTranscriptApi;

namespace CaptionZen.Shared.Services;

internal class ScraperYouTubeService : IYouTubeService {

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;

    public ScraperYouTubeService(HttpClient httpClient, IMemoryCache memoryCache) {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
    }

    public Task<string?> GetTranscriptAsync(string videoUrl, CancellationToken cancellationToken = default) {
        return Task.Run(() => {
#if DEBUG
            //return (string?)"Blblbl Blblbl Blblbl Blblbl Blblbl Blblbl Blblbl";
#endif
            var videoId = ExtractVideoId(videoUrl);
            if (string.IsNullOrEmpty(videoId)) {
                throw new ArgumentException("Invalid Video Url");
            }

            var youTubeTranscriptApi = new YouTubeTranscriptApi();
            var transcriptItems = youTubeTranscriptApi.GetTranscript(videoId);
            if (transcriptItems is null) {
                return null;
            }

            return ToString(transcriptItems);
        }, cancellationToken);
    }

    public async Task<VideoDetails?> GetVideoDetailsAsync(string videoUrl, CancellationToken cancellationToken = default) {
        var videoId = GetVideoId(videoUrl);
        if (videoId is null) return null;

        if (_memoryCache.TryGetValue(videoId, out VideoDetails? videoDetails)) {
            if (videoDetails is not null) return videoDetails;
        }

        var detail = await _httpClient.GetFromJsonAsync<NoEmbedDetail>($"https://noembed.com/embed?url=https://www.youtube.com/watch?v={videoId}", cancellationToken);
        if (detail!.Title is null) return null;

        videoDetails = new VideoDetails {
            VideoId = videoId!,
            Title = detail!.Title,
            Description = detail.Description,
            ThumbnailUrl = detail.Thumbnail_url,
            Url = videoUrl
        };
        _memoryCache.Set(videoId, videoDetails);

        return videoDetails;
    }

    public string? GetVideoId(string videoUrl) {
        return ExtractVideoId(videoUrl);
    }

    private string? ExtractVideoId(string url) {
        var videoIdMatch = Regex.Match(url, @"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");
        return videoIdMatch.Success ? videoIdMatch.Groups[1].Value : null;
    }

    private string? ToString(IEnumerable<TranscriptItem>? items) {
        if (items is null) {
            return null;
        }

        var concatenatedText = string.Join(" ", items.Select(item => item.Text));
        concatenatedText = concatenatedText.Replace("\n", " ");

        return concatenatedText;
    }

    private class NoEmbedDetail {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Thumbnail_url { get; set; }
    }
}