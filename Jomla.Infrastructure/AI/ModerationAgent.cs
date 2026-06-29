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
            {
                if (IsPublicUri(uri))
                {
                    parts.Add(new ImageContent(uri));
                }
                else
                {
                    _logger.LogWarning("Local/private image URL skipped: {Url}", url);
                }
            }
            else
            {
                _logger.LogWarning("Invalid image URL skipped: {Url}", url);
            }
        }

        history.Add(new ChatMessageContent(AuthorRole.User, parts));

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = "json_object"
        };

        var response = await _chat.GetChatMessageContentAsync(history, executionSettings, cancellationToken: ct);

        if (response.Metadata is not null && 
            response.Metadata.TryGetValue("Refusal", out var refusalObj) && 
            refusalObj is string refusal && 
            !string.IsNullOrWhiteSpace(refusal))
        {
            _logger.LogWarning("Moderation agent request was refused by the model safety filters. Refusal: {Refusal}", refusal);
            return new ModerationResult(false, $"Content violated safety policies: {refusal}");
        }

        _logger.LogInformation("Moderation agent raw response: {ResponseContent}", response.Content);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("Moderation agent returned empty content without a refusal message.");
            return Fallback();
        }

        return Parse(response.Content);
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

    private static bool IsPublicUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var host = uri.Host;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1") ||
            string.Equals(host, "[::1]"))
        {
            return false;
        }

        // Check if it's a private IP address (IPv4)
        // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length == 4)
            {
                if (bytes[0] == 10) return false;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                if (bytes[0] == 192 && bytes[1] == 168) return false;
            }
            else if (bytes.Length == 16)
            {
                // IPv6 loopback or link-local/site-local
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || System.Net.IPAddress.IsLoopback(ip))
                    return false;
            }
        }

        return true;
    }

    // Fail safe — flag for manual review if model returns garbage
    private static ModerationResult Fallback()
        => new(false, "Moderation service returned an unreadable response.");

    private sealed record ModerationJson(bool Approved, string? Reason);
}
