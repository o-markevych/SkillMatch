namespace SkillMatch.Core.Models;

/// <summary>A skill from the user's profile.</summary>
public sealed record UserSkill(
    string Name,
    SkillLevel Level,
    double? YearsOfExperience = null);
