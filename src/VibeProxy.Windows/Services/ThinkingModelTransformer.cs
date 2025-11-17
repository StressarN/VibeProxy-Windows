using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VibeProxy.Windows.Services;

internal static class ThinkingModelTransformer
{
    private const int HardCap = 32000;

    public static (string Body, bool Modified) Apply(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (body, false);
        }

        try
        {
            var node = JsonNode.Parse(body) as JsonObject;
            if (node is null)
            {
                return (body, false);
            }

            if (node["model"] is not JsonValue modelValue)
            {
                return (body, false);
            }

            var model = modelValue.GetValue<string>();
            if (string.IsNullOrWhiteSpace(model) || !model.StartsWith("claude-", StringComparison.OrdinalIgnoreCase))
            {
                return (body, false);
            }

            const string marker = "-thinking-";
            var markerIndex = model.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return (body, false);
            }

            var budgetSegment = model[(markerIndex + marker.Length)..];
            var cleanedModel = model[..markerIndex];
            node["model"] = cleanedModel;

            if (!int.TryParse(budgetSegment, out var requestedBudget) || requestedBudget <= 0)
            {
                return (node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }), true);
            }

            var effectiveBudget = Math.Min(requestedBudget, HardCap - 1);
            node["thinking"] = new JsonObject
            {
                ["type"] = "enabled",
                ["budget_tokens"] = effectiveBudget
            };

            var desiredHeadroom = Math.Max(1024, effectiveBudget / 10);
            var desiredMaxTokens = Math.Min(HardCap, effectiveBudget + desiredHeadroom);

            var preferredTokenField = node.ContainsKey("max_tokens")
                ? "max_tokens"
                : node.ContainsKey("max_output_tokens")
                    ? "max_output_tokens"
                    : "max_tokens";

            AdjustTokenField(node, preferredTokenField, desiredMaxTokens, effectiveBudget);

            return (node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }), true);
        }
        catch (JsonException)
        {
            return (body, false);
        }
    }

    private static void AdjustTokenField(JsonObject node, string propertyName, int desiredValue, int budget)
    {
        if (node[propertyName] is JsonValue existingValue && existingValue.TryGetValue<int>(out var current) && current > budget)
        {
            return;
        }

        if (!node.ContainsKey(propertyName))
        {
            node[propertyName] = Math.Max(budget + 1, desiredValue);
            return;
        }

        node[propertyName] = Math.Max(budget + 1, desiredValue);
    }
}
