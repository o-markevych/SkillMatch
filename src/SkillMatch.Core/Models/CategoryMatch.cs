namespace SkillMatch.Core.Models;

/// <summary>Match breakdown for a single skill category.</summary>
public sealed record CategoryMatch(
    SkillCategory Category,
    IReadOnlyList<ExtractedSkill> Matched,
    IReadOnlyList<ExtractedSkill> Missing,
    double Score);
