namespace SkillMatch.Core.Models;

/// <summary>A skill found in vacancy text.</summary>
public sealed record ExtractedSkill(
    SkillDefinition Definition,
    SkillImportance Importance,
    int Occurrences);
