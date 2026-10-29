using SkillMatch.Core.Models;
using SkillMatch.Core.Taxonomy;

namespace SkillMatch.Core.Tests.Taxonomy;

public class SkillTaxonomyTests
{
    private static SkillDefinition Skill(string name, params string[] aliases) =>
        new(name, SkillCategory.Other, aliases);

    [Fact]
    public void TryFind_ResolvesNameAndAliasesCaseInsensitively()
    {
        var postgres = Skill("PostgreSQL", "postgres", "psql");
        var taxonomy = new SkillTaxonomy([postgres, Skill("C#", "csharp")]);

        Assert.True(taxonomy.TryFind("POSTGRES", out var byAlias));
        Assert.Same(postgres, byAlias);
        Assert.True(taxonomy.TryFind(" postgresql ", out var byName));
        Assert.Same(postgres, byName);
        Assert.False(taxonomy.TryFind("mysql", out _));
    }

    [Fact]
    public void Constructor_AllowsAliasRepeatedWithinOneSkill()
    {
        var taxonomy = new SkillTaxonomy([Skill("C#", "c#", "C#", "csharp")]);

        Assert.Single(taxonomy.Skills);
    }

    [Fact]
    public void Constructor_RejectsDuplicateCanonicalNames()
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() =>
            new SkillTaxonomy([Skill("Docker"), Skill("docker")]));

        Assert.Equal(["Duplicate skill name 'docker'."], ex.Errors);
    }

    [Fact]
    public void Constructor_RejectsAliasSharedByTwoSkills()
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() =>
            new SkillTaxonomy([Skill("Docker", "containers"), Skill("Kubernetes", "Containers")]));

        Assert.Equal(["Alias 'Containers' points to both 'Docker' and 'Kubernetes'."], ex.Errors);
    }

    [Fact]
    public void Constructor_RejectsAliasMatchingAnotherSkillsName()
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() =>
            new SkillTaxonomy([Skill("Go"), Skill("Golang", "go")]));

        Assert.Equal(["Alias 'go' points to both 'Go' and 'Golang'."], ex.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankNamesAndAliases(string blank)
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() =>
            new SkillTaxonomy([Skill(blank), Skill("Git", blank)]));

        Assert.Equal(["A skill has an empty name.", "Skill 'Git' has an empty alias."], ex.Errors);
    }

    [Fact]
    public void Constructor_ReportsAllErrorsAtOnce()
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() =>
            new SkillTaxonomy([Skill("A", "x"), Skill("B", "x"), Skill("a")]));

        Assert.Equal(2, ex.Errors.Count);
        Assert.Contains("'x'", ex.Message);
        Assert.Contains("'a'", ex.Message);
    }
}
