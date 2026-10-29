namespace SkillMatch.Core.Models;

/// <summary>Result of comparing vacancy skills with the user's profile.</summary>
/// <param name="Matched">Skills present in both the vacancy and the profile.</param>
/// <param name="Missing">Skills the vacancy asks for that the profile lacks.</param>
/// <param name="Extra">Profile skills the vacancy doesn't mention.</param>
/// <param name="Score">Overall match score, 0–100.</param>
/// <param name="ByCategory">Per-category breakdown.</param>
public sealed record MatchResult(
    IReadOnlyList<ExtractedSkill> Matched,
    IReadOnlyList<ExtractedSkill> Missing,
    IReadOnlyList<UserSkill> Extra,
    double Score,
    IReadOnlyList<CategoryMatch> ByCategory)
{
    public IEnumerable<ExtractedSkill> MissingRequired =>
        Missing.Where(s => s.Importance == SkillImportance.Required);

    public IEnumerable<ExtractedSkill> MissingNiceToHave =>
        Missing.Where(s => s.Importance == SkillImportance.NiceToHave);
}
