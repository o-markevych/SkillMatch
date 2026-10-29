namespace SkillMatch.Core.Models;

/// <summary>A known skill from the taxonomy, identified by its canonical name.</summary>
public sealed record SkillDefinition(
    string Name,
    SkillCategory Category,
    IReadOnlyList<string> Aliases);
