using System.Text;
using System.Text.Json;
using SkillMatch.Core.Models;
using SkillMatch.Core.Taxonomy;

namespace SkillMatch.Core.Tests.Taxonomy;

public class JsonTaxonomyProviderTests
{
    private static SkillTaxonomy Load(string json) =>
        JsonTaxonomyProvider.Load(new MemoryStream(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public void Load_ParsesSkillsWithCategoriesAndAliases()
    {
        var taxonomy = Load("""
            [
              // comments and trailing commas are allowed
              { "name": "C#",  "category": "Language", "aliases": ["csharp", "c sharp"] },
              { "name": "Git", "category": "tool" },
            ]
            """);

        Assert.Equal(2, taxonomy.Skills.Count);
        Assert.True(taxonomy.TryFind("c sharp", out var csharp));
        Assert.Equal(new[] { "csharp", "c sharp" }, csharp.Aliases);
        Assert.Equal(SkillCategory.Language, csharp.Category);
        Assert.True(taxonomy.TryFind("git", out var git));
        Assert.Equal(SkillCategory.Tool, git.Category);
        Assert.Empty(git.Aliases);
    }

    [Theory]
    [InlineData("""{ "name": "Foo", "category": "Gadget" }""", "Skill 'Foo' has unknown category 'Gadget'.")]
    [InlineData("""{ "name": "Foo", "category": "42" }""", "Skill 'Foo' has unknown category '42'.")]
    [InlineData("""{ "name": "Foo" }""", "Skill 'Foo' has unknown category ''.")]
    public void Load_RejectsUnknownCategory(string entry, string expectedError)
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() => Load($"[{entry}]"));

        Assert.Equal([expectedError], ex.Errors);
    }

    [Fact]
    public void Load_RejectsMissingName()
    {
        var ex = Assert.Throws<TaxonomyValidationException>(() => Load("""[{ "category": "Tool" }]"""));

        Assert.Equal(["A skill has an empty name."], ex.Errors);
    }

    [Fact]
    public void Load_RejectsNullDocument()
    {
        Assert.Throws<TaxonomyValidationException>(() => Load("null"));
    }

    [Fact]
    public void Load_ThrowsOnMalformedJson()
    {
        Assert.ThrowsAny<JsonException>(() => Load("[{"));
    }

    [Fact]
    public void GetTaxonomy_ReadsFileOnceAndCachesResult()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """[{ "name": "Git", "category": "Tool" }]""");
            var provider = new JsonTaxonomyProvider(path);

            var first = provider.GetTaxonomy();
            File.Delete(path);

            Assert.Same(first, provider.GetTaxonomy());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BundledTaxonomy_IsValid()
    {
        var taxonomy = new JsonTaxonomyProvider(Path.Combine(AppContext.BaseDirectory, "data", "skills-taxonomy.json"))
            .GetTaxonomy();

        Assert.InRange(taxonomy.Skills.Count, 100, 200);
        Assert.All(Enum.GetValues<SkillCategory>(), c => Assert.Contains(taxonomy.Skills, s => s.Category == c));
        Assert.True(taxonomy.TryFind("dotnet", out var dotnet));
        Assert.Equal(".NET", dotnet.Name);
    }
}
