using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace VibeProxy.Windows.Services;

internal static class ThinkingModelTransformer
{
    private const int HardCap = 32000;

    private static readonly Regex OpenAIFastModePattern = new(
        @"^(gpt-5(?:\.\d+)?(?:-codex)?)(?:-(none|minimal|low|medium|high|xhigh|max))?-fast$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            if (string.IsNullOrWhiteSpace(model))
            {
                return (body, false);
            }

            var fastModeApplied = ApplyFastMode(node, model);
            if (fastModeApplied)
            {
                model = node["model"]!.GetValue<string>();
            }

            if (!model.StartsWith("claude-", StringComparison.OrdinalIgnoreCase))
            {
                if (fastModeApplied)
                {
                    return (node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }), true);
                }

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

    private static bool ApplyFastMode(JsonObject node, string model)
    {
        var match = OpenAIFastModePattern.Match(model.Trim());
        if (!match.Success)
        {
            return false;
        }

        var baseModel = match.Groups[1].Value.ToLowerInvariant();
        var normalizedModel = baseModel;

        if (match.Groups[2].Success)
        {
            var level = match.Groups[2].Value.ToLowerInvariant();
            normalizedModel = $"{baseModel}({level})";
        }

        node["model"] = normalizedModel;

        var hasExplicitServiceTier = node["service_tier"] is JsonValue tierValue
            && !string.IsNullOrWhiteSpace(tierValue.GetValue<string>());

        if (!hasExplicitServiceTier)
        {
            node["service_tier"] = "priority";
        }

        return true;
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
