using System.Diagnostics.CodeAnalysis;
using SkillMatch.Core.Models;

namespace SkillMatch.Core.Taxonomy;

/// <summary>
/// A validated set of known skills with case-insensitive lookup by canonical name or alias.
/// </summary>
public sealed class SkillTaxonomy
{
    private readonly Dictionary<string, SkillDefinition> _byAlias = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SkillDefinition> Skills { get; }

    /// <exception cref="TaxonomyValidationException">
    /// A name is blank or duplicated, or an alias points to more than one skill.
    /// </exception>
    public SkillTaxonomy(IEnumerable<SkillDefinition> skills)
    {
        Skills = skills.ToList();

        var errors = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name))
            {
                errors.Add("A skill has an empty name.");
                continue;
            }

            if (!names.Add(skill.Name.Trim()))
            {
                errors.Add($"Duplicate skill name '{skill.Name}'.");
                continue;
            }

            // The canonical name always resolves to its own skill, even if not listed among the aliases.
            foreach (var alias in skill.Aliases.Prepend(skill.Name))
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    errors.Add($"Skill '{skill.Name}' has an empty alias.");
                    continue;
                }

                var key = alias.Trim();
                if (!_byAlias.TryAdd(key, skill) && !ReferenceEquals(_byAlias[key], skill))
                {
                    errors.Add($"Alias '{key}' points to both '{_byAlias[key].Name}' and '{skill.Name}'.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new TaxonomyValidationException(errors);
        }
    }

    public bool TryFind(string nameOrAlias, [NotNullWhen(true)] out SkillDefinition? skill) =>
        _byAlias.TryGetValue(nameOrAlias.Trim(), out skill);
}
