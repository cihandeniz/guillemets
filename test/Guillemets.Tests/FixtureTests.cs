using Guillemets;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class FixtureTests
{
    // Fixtures the engine doesn't implement yet. TDD one-case-at-a-time: a
    // fixture listed here is Ignored (not Failed), so the suite is always
    // green at commit time. Remove a fixture's name once its case goes
    // green; this set is empty once the engine is complete.
    private static readonly HashSet<string> IgnoredFixtures =
    [
        "01-variables/002-nested-property",
        "01-variables/003-nested-property-chained-list",
        "01-variables/004-multiline-whitespace",
        "02-conditional-blocks/001-boolean-true-no-else",
        "02-conditional-blocks/002-boolean-false-no-else",
        "02-conditional-blocks/003-else-truthy",
        "02-conditional-blocks/004-else-falsy",
        "02-conditional-blocks/005-null-object-else",
        "03-loop-blocks/001-list-of-objects",
        "03-loop-blocks/002-empty-list",
        "03-loop-blocks/003-magic-loop-vars",
        "03-loop-blocks/004-negation",
        "04-scope-blocks/001-object-scope",
        "04-scope-blocks/002-upper-scope-fallback",
        "05-variable-definitions/001-definition-boolean",
        "05-variable-definitions/002-definition-object",
        "05-variable-definitions/003-definition-list-separator",
        "06-tables/001-table-block",
        "07-inline-lists/001-inline-scalar-list",
        "07-inline-lists/002-inline-field-selection",
        "07-inline-lists/003-custom-separator",
        "08-parameters/001-format-date",
        "08-parameters/002-currency",
        "08-parameters/003-truncate-length",
        "09-integration/001-customer-offer",
    ];

    private static readonly string SpecsRoot = FindSpecsRoot();

    private static string FindSpecsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Guillemets.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException(
                "Could not locate repo root (Guillemets.slnx) from test assembly location.");
        }

        return Path.Combine(dir.FullName, "specs");
    }

    private static IEnumerable<TestCaseData> FixtureCases()
    {
        foreach (var templatePath in Directory
                     .EnumerateFiles(SpecsRoot, "*.guil.md", SearchOption.AllDirectories)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var basePath = templatePath[..^".guil.md".Length];
            var dataPath = basePath + ".json";
            var expectedPath = basePath + ".md";
            var relativeName = Path.GetRelativePath(SpecsRoot, basePath).Replace('\\', '/');

            var testCase = new TestCaseData(templatePath, dataPath, expectedPath).SetName(relativeName);
            if (IgnoredFixtures.Contains(relativeName))
            {
                testCase.Ignore("not yet implemented");
            }

            yield return testCase;
        }
    }

    [TestCaseSource(nameof(FixtureCases))]
    public void Fixture_RendersExpectedOutput(string templatePath, string dataPath, string expectedPath)
    {
        var template = File.ReadAllText(templatePath);
        var expected = File.ReadAllText(expectedPath);
        using var dataDoc = JsonDocument.Parse(File.ReadAllText(dataPath));

        var actual = TemplateEngine.Render(template, dataDoc.RootElement);

        actual.ShouldBe(expected);
    }
}
