using Jomla.Application.Common.DTOs;
using Jomla.Application.Common.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jomla.Infrastructure.AI
{
    public class CategoryAgent(IChatCompletionService chat) : ICategoryAgent
    {
        private readonly IChatCompletionService _chat = chat;

        private const string SystemPrompt = """
                You are a product categorization assistant for a B2B marketplace.
                You will receive a product title and a list of available categories.

                Rules:
                - You MUST pick exactly one category from the provided list.
                - Prefer the most specific (leaf) category if it fits well.
                - If no leaf fits, pick the closest parent category.
                - Never invent a category. Never return null.

                Respond ONLY with valid JSON, no markdown, no extra text:
                {"categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}
                """;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        public async Task<Guid> ResolveCategoryAsync(string itemTitle, IEnumerable<CategoryDto> categories, CancellationToken ct = default)
        {
            var categoryList = string.Join("\n", categories.Select(c => $"{c.Id} | {c.Label}"));

            var history = new ChatHistory();
            history.AddSystemMessage(SystemPrompt);
            history.AddUserMessage($"Product title: \"{itemTitle}\"\n\nAvailable categories:\n{categoryList}");

            var settings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object",
                Temperature = 0
            };

            var response = await chat.GetChatMessageContentAsync(history, settings, cancellationToken: ct);
            return Parse(response.Content ?? string.Empty);
        }

        private static Guid Parse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var value = doc.RootElement.GetProperty("categoryId").GetString()!;
                return Guid.Parse(value);
            }
            catch
            {
                throw new InvalidOperationException("Categorization agent returned an unreadable response.");
            }
        }

    }
}
