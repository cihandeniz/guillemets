using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class FixtureTests
{
    // Fixtures the engine doesn't implement yet. TDD one-case-at-a-time: a
    // fixture listed here is Ignored (not Failed), so the suite is always
    // green at commit time. Remove a fixture's name once its case goes
    // green; this set is empty once the engine is complete.
    static readonly HashSet<string> IGNORED_FIXTURES =
    [
        "02-conditional-blocks/003-else-truthy",
        "02-conditional-blocks/004-else-falsy",
        "02-conditional-blocks/005-null-object-else",
        "03-loop-blocks/001-list-of-objects",
        "03-loop-blocks/002-empty-list",
        "03-loop-blocks/003-magic-loop-vars",
        "03-loop-blocks/004-negation",
        "03-loop-blocks/005-filtered-item-scope",
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

    static readonly string SPECS_ROOT = FindSpecsRoot();

    static string FindSpecsRoot()
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

    static IEnumerable<TestCaseData> FixtureCases()
    {
        foreach (var templatePath in Directory
                     .EnumerateFiles(SPECS_ROOT, "*.guil.md", SearchOption.AllDirectories)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var basePath = templatePath[..^".guil.md".Length];
            if (File.Exists(basePath + ".error")) { continue; }

            var dataPath = basePath + ".json";
            var expectedPath = basePath + ".md";
            var relativeName = Path.GetRelativePath(SPECS_ROOT, basePath).Replace('\\', '/');

            var testCase = new TestCaseData(templatePath, dataPath, expectedPath).SetName(relativeName);
            if (IGNORED_FIXTURES.Contains(relativeName))
            {
                testCase.Ignore("not yet implemented");
            }

            yield return testCase;
        }
    }

    static IEnumerable<TestCaseData> ErrorFixtureCases()
    {
        foreach (var templatePath in Directory
                     .EnumerateFiles(SPECS_ROOT, "*.guil.md", SearchOption.AllDirectories)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var basePath = templatePath[..^".guil.md".Length];
            var errorPath = basePath + ".error";
            if (!File.Exists(errorPath)) { continue; }

            var dataPath = basePath + ".json";
            var relativeName = Path.GetRelativePath(SPECS_ROOT, basePath).Replace('\\', '/');

            yield return new TestCaseData(templatePath, dataPath, errorPath).SetName(relativeName);
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

    [TestCaseSource(nameof(ErrorFixtureCases))]
    public void Fixture_ThrowsExpectedError(string templatePath, string dataPath, string errorPath)
    {
        var template = File.ReadAllText(templatePath);
        var expectedError = File.ReadAllText(errorPath);
        using var dataDoc = JsonDocument.Parse(File.ReadAllText(dataPath));

        var exception = Should.Throw<TemplateParseException>(() => TemplateEngine.Render(template, dataDoc.RootElement));

        exception.Message.ShouldBe(expectedError);
    }
}