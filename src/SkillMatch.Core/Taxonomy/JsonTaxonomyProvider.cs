using System.Text.Json;
using SkillMatch.Core.Models;

namespace SkillMatch.Core.Taxonomy;

/// <summary>Loads the taxonomy from a JSON file once and caches it.</summary>
public sealed class JsonTaxonomyProvider(string filePath) : ITaxonomyProvider
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Lazy<SkillTaxonomy> _taxonomy = new(() =>
    {
        using var stream = File.OpenRead(filePath);
        return Load(stream);
    });

    public SkillTaxonomy GetTaxonomy() => _taxonomy.Value;

    /// <exception cref="JsonException">The content is not valid JSON.</exception>
    /// <exception cref="TaxonomyValidationException">The content breaks a taxonomy rule.</exception>
    public static SkillTaxonomy Load(Stream json)
    {
        var entries = JsonSerializer.Deserialize<List<TaxonomyEntry>>(json, Options)
            ?? throw new TaxonomyValidationException(["The taxonomy is empty."]);

        var errors = new List<string>();
        var skills = new List<SkillDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            if (!Enum.TryParse<SkillCategory>(entry.Category, ignoreCase: true, out var category)
                || !Enum.IsDefined(category))
            {
                errors.Add($"Skill '{entry.Name}' has unknown category '{entry.Category}'.");
                continue;
            }

            skills.Add(new SkillDefinition(entry.Name ?? "", category, entry.Aliases ?? []));
        }

        if (errors.Count > 0)
        {
            throw new TaxonomyValidationException(errors);
        }

        return new SkillTaxonomy(skills);
    }

    private sealed record TaxonomyEntry(string? Name, string? Category, List<string>? Aliases);
}
