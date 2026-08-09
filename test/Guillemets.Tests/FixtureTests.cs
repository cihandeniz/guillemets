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
        "03-loop-blocks/001a-populated",
        "03-loop-blocks/001b-empty",
        "03-loop-blocks/002-magic-loop-vars",
        "03-loop-blocks/003-negation",
        "03-loop-blocks/004-filtered-item-scope",
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
        foreach (var dataPath in DataFiles())
        {
            if (File.Exists(WithExtension(dataPath, ".error"))) { continue; }

            var testCase = new TestCaseData(TemplateFor(dataPath), dataPath, WithExtension(dataPath, ".md"))
                .SetName(FixtureName(dataPath));

            if (IGNORED_FIXTURES.Contains(FixtureName(dataPath)))
            {
                testCase.Ignore("not yet implemented");
            }

            yield return testCase;
        }
    }

    static IEnumerable<TestCaseData> ErrorFixtureCases()
    {
        foreach (var dataPath in DataFiles())
        {
            var errorPath = WithExtension(dataPath, ".error");
            if (!File.Exists(errorPath)) { continue; }

            yield return new TestCaseData(TemplateFor(dataPath), dataPath, errorPath).SetName(FixtureName(dataPath));
        }
    }

    static IEnumerable<string> DataFiles() =>
        Directory.EnumerateFiles(SPECS_ROOT, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);

    static string WithExtension(string dataPath, string extension) =>
        dataPath[..^".json".Length] + extension;

    static string FixtureName(string dataPath) =>
        Path.GetRelativePath(SPECS_ROOT, dataPath[..^".json".Length]).Replace('\\', '/');

    // A case file (e.g. "005a-both-truthy.json") reuses the .guil.md whose
    // leading number matches its own, so several cases can share one
    // template without duplicating it -- see "005-nested-blocks.guil.md"
    // and its "005a"/"005b"/"005c" cases.
    static string TemplateFor(string dataPath)
    {
        var directory = Path.GetDirectoryName(dataPath)
            ?? throw new InvalidOperationException($"Fixture data path '{dataPath}' has no directory.");
        var group = LeadingNumber(Path.GetFileName(dataPath));

        return Directory.EnumerateFiles(directory, "*.guil.md")
            .SingleOrDefault(path => LeadingNumber(Path.GetFileName(path)) == group)
            ?? throw new InvalidOperationException(
                $"No template found for fixture data '{dataPath}' (expected a *.guil.md starting with '{group}' in the same folder).");
    }

    static string LeadingNumber(string fileName) =>
        new([.. fileName.TakeWhile(char.IsDigit)]);

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