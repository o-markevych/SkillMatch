namespace SkillMatch.Core.Taxonomy;

public sealed class TaxonomyValidationException(IReadOnlyList<string> errors)
    : Exception("Invalid skills taxonomy:" + string.Concat(errors.Select(e => Environment.NewLine + "  - " + e)))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
