using System.Text.Json;
using Jomla.Application.Common.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace Jomla.Infrastructure.AI;

public class ModerationService(IChatCompletionService chat) : IModerationService
{
    private readonly IChatCompletionService _chat = chat;

    private const string SystemPrompt = """
        You are a content moderation assistant for a B2B group-buying marketplace.
        Flag content that contains any of the following:
        - Illegal products or services (weapons, drugs, counterfeit goods, etc.)
        - Hate speech, racism, or discriminatory language
        - Adult or sexually explicit content
        - Violent or graphic content
        - Spam or misleading product claims

        You will receive a title, an optional description, and optionally one or more images.
        Respond ONLY with valid JSON, no markdown, no extra text:
        { "approved": true, "reason": null }
        or
        { "approved": false, "reason": "Brief explanation of what was flagged" }
        """;

    // Cache JsonSerializerOptions instance
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<ModerationResult> ModerateAsync(ModerationInput input, CancellationToken ct = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);

        var parts = new ChatMessageContentItemCollection();

        var text = $"Title: {input.Title}";
        if (!string.IsNullOrWhiteSpace(input.Description))
            text += $"\nDescription: {input.Description}";

        parts.Add(new TextContent(text));

        foreach (var url in input.ImageUrls)
            parts.Add(new ImageContent(new Uri(url)));

        history.Add(new ChatMessageContent(AuthorRole.User, parts));


        return Parse(response.Content ?? string.Empty);
    }

    private static ModerationResult Parse(string raw)
    {
        try
        {
            var json = JsonSerializer.Deserialize<ModerationJson>(
                raw,
                _jsonOptions);

            return json is null
                ? Fallback()
        }
        catch
        {
            return Fallback();
        }
    }

    // Fail safe — flag for manual review if model returns garbage
    private static ModerationResult Fallback()
        => new(false, "Moderation service returned an unreadable response.");

}