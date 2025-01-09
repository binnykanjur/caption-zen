using Markdig;

namespace CaptionZen.Shared.Services;

public class MarkdownService {

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public string ConvertToHtml(string markdown) {
        return Markdown.ToHtml(markdown, _pipeline);
    }
}