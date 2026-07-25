using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ScopeSeal.Extraction.Services;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services;

public sealed partial class ExtractionSchemaValidator(IOptions<ScopeSealOptions> scopeSealOptions) : IExtractionSchemaValidator
{
    private static readonly Regex InstructionPattern = InstructionRegex();

    public (bool IsValid, string? Error) ValidateFacts(IReadOnlyList<ProviderExtractedFact> facts)
    {
        var maxFacts = scopeSealOptions.Value.Ai.MaxFactsPerRun;
        if (facts.Count > maxFacts)
        {
            return (false, $"Extraction output exceeded the maximum of {maxFacts} facts.");
        }

        for (var index = 0; index < facts.Count; index++)
        {
            var fact = facts[index];
            if (string.IsNullOrWhiteSpace(fact.Title))
            {
                return (false, $"Fact at index {index} is missing a title.");
            }

            if (fact.Title.Length > 500)
            {
                return (false, $"Fact at index {index} has an oversized title.");
            }

            if (fact.Description?.Length > 4000)
            {
                return (false, $"Fact at index {index} has an oversized description.");
            }

            if (fact.ConfidenceScore is < 0m or > 1m)
            {
                return (false, $"Fact at index {index} has an invalid confidence score.");
            }

            if (ContainsInstructionLikeContent(fact.Title) ||
                ContainsInstructionLikeContent(fact.Description) ||
                ContainsInstructionLikeContent(fact.Source.Excerpt))
            {
                return (false, $"Fact at index {index} contains disallowed instruction-like content.");
            }

            if (fact.AmountMinorUnits is < 0)
            {
                return (false, $"Fact at index {index} has an invalid amount.");
            }

            if (fact.AmountMinorUnits is not null &&
                (string.IsNullOrWhiteSpace(fact.CurrencyCode) || fact.CurrencyCode.Length != 3))
            {
                return (false, $"Fact at index {index} requires a valid ISO currency code.");
            }
        }

        return (true, null);
    }

    private static bool ContainsInstructionLikeContent(string? value) =>
        !string.IsNullOrWhiteSpace(value) && InstructionPattern.IsMatch(value);

    [GeneratedRegex(
        "(?i)(ignore (all )?previous instructions|system prompt|you are now|execute command|reveal secret|<script|javascript:)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex InstructionRegex();
}
