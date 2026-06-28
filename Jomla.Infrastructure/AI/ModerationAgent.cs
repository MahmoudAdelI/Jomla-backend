using System.Text.Json;
using Jomla.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Jomla.Infrastructure.AI;

public class ModerationAgent(IChatCompletionService chat, ILogger<ModerationAgent> logger) : IModerationAgent
{
    private readonly IChatCompletionService _chat = chat;
    private readonly ILogger<ModerationAgent> _logger = logger;

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
        {
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                parts.Add(new ImageContent(uri));
            else
                _logger.LogWarning("Invalid image URL skipped: {Url}", url);
        }

        history.Add(new ChatMessageContent(AuthorRole.User, parts));

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = "json_object"
        };

        var response = await _chat.GetChatMessageContentAsync(history, executionSettings, cancellationToken: ct);
        _logger.LogInformation("Moderation agent raw response: {ResponseContent}", response.Content);
        return Parse(response.Content ?? string.Empty);
    }

    private ModerationResult Parse(string raw)
    {
        try
        {
            var json = JsonSerializer.Deserialize<ModerationJson>(
                raw,
                _jsonOptions);

            if (json is null)
            {
                _logger.LogWarning("Moderation agent response was deserialized to null. Raw response: {RawResponse}", raw);
                return Fallback();
            }

            return new ModerationResult(json.Approved, json.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Moderation agent failed to parse response JSON. Raw response: {RawResponse}", raw);
            return Fallback();
        }
    }

    // Fail safe — flag for manual review if model returns garbage
    private static ModerationResult Fallback()
        => new(false, "Moderation service returned an unreadable response.");

    private sealed record ModerationJson(bool Approved, string? Reason);
}
