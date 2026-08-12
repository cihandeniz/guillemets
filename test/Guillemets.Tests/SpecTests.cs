using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class SpecTests
{
    // Fixtures the engine doesn't implement yet. TDD one-case-at-a-time: a
    // fixture listed here is Ignored (not Failed), so the suite is always
    // green at commit time. Remove a fixture's name once its case goes
    // green; this set is empty once the engine is complete.
    static readonly HashSet<string> IGNORED_FIXTURES =
    [
        // PLAN.md milestone 1, "filter-syntax-redesign": spec/test/doc work
        // is done (see PLAN.md for the full grammar), engine still speaks
        // the old `(name = value)` syntax. Comes out of this set one
        // fixture at a time as each is implemented.
        "02-conditional-blocks/009-corrupted-filter-syntax-in-body",
        "05-variable-definitions/003-definition-list-join",
        "05-variable-definitions/004a-populated",
        "05-variable-definitions/004b-empty",
        "05-variable-definitions/006-definition-list-multi-filter-footer",
        "07-inline-lists/003-custom-separator",
        "07-inline-lists/004-join-last",
        "08-filters/001-date",
        "08-filters/002-currency",
        "08-filters/003-truncate-length",
        "08-filters/004-join-default-inline",
        "08-filters/005-join-default-block-footer",
        "08-filters/006-join-escaped-newline",
        "08-filters/007-join-escaped-tab",
        "08-filters/008-join-escaped-pipe",
        "12-escaping/001-guillemets-and-backslash",
        "12-escaping/002-close-inside-block",
        "12-escaping/005-double-backslash-before-guillemet",

        // PLAN.md milestone 4, "inline-lists" (remainder): VariableNode
        // doesn't yet join a resolved list's elements when no filter ran.
        "07-inline-lists/001-inline-scalar-list",
        "07-inline-lists/002-inline-field-selection",
    ];

    static IEnumerable<TestCaseData> FixtureCases() =>
        CaseFiles(".md").Select(TestCaseFor);

    static IEnumerable<TestCaseData> ErrorFixtureCases() =>
        CaseFiles(".error").Select(TestCaseFor);

    static TestCaseData TestCaseFor(string path)
    {
        var testCase = new TestCaseData(TemplateFor(path), DataPathFor(path), path).SetName(FixtureName(path));

        return IGNORED_FIXTURES.Contains(FixtureName(path)) ? testCase.Ignore("not yet implemented") : testCase;
    }

    // Case files are discovered by extension (".md" for success, ".error"
    // for parse errors), excluding *.guil.md templates (which also end in
    // ".md"). A case's *.json data file is optional -- see ReadData.
    // "09-integration" is excluded here: it's exercised explicitly by each
    // data source's own *IntegrationTests, not by the generic spec sweep.
    static IEnumerable<string> CaseFiles(string extension) =>
        Directory.EnumerateFiles(SpecsRoot.PATH, $"*{extension}", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".guil.md", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}09-integration{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);

    static string DataPathFor(string casePath) =>
        BasePath(casePath) + ".json";

    static string BasePath(string path) =>
        Path.ChangeExtension(path, null);

    static string FixtureName(string path) =>
        Path.GetRelativePath(SpecsRoot.PATH, BasePath(path)).Replace('\\', '/');

    // A case file (e.g. "005a-both-truthy.md") reuses the .guil.md whose
    // leading number matches its own, so several cases can share one
    // template without duplicating it -- see "005-nested-blocks.guil.md"
    // and its "005a"/"005b"/"005c" cases.
    static string TemplateFor(string casePath)
    {
        var directory = Path.GetDirectoryName(casePath)
            ?? throw new InvalidOperationException($"Fixture case path '{casePath}' has no directory.");
        var group = LeadingNumber(Path.GetFileName(casePath));

        return Directory.EnumerateFiles(directory, "*.guil.md")
            .SingleOrDefault(path => LeadingNumber(Path.GetFileName(path)) == group)
            ?? throw new InvalidOperationException(
                $"No template found for fixture case '{casePath}' (expected a *.guil.md starting with '{group}' in the same folder).");
    }

    static string LeadingNumber(string fileName) =>
        new([.. fileName.TakeWhile(char.IsDigit)]);

    // A fixture with no *.json file renders against an empty object rather
    // than requiring one -- most fixtures (parse-error cases, plain literal
    // text, etc.) never touch data at all.
    static JsonElement ReadData(string dataPath)
    {
        using var document = JsonDocument.Parse(File.Exists(dataPath) ? File.ReadAllText(dataPath) : "{}");

        return document.RootElement.Clone();
    }

    [TestCaseSource(nameof(FixtureCases))]
    public void Fixture_RendersExpectedOutput(string templatePath, string dataPath, string expectedPath)
    {
        var template = File.ReadAllText(templatePath);
        var expected = File.ReadAllText(expectedPath);

        var actual = Template.Create(template).Render(ReadData(dataPath));

        actual.ShouldBe(expected);
    }

    [TestCaseSource(nameof(ErrorFixtureCases))]
    public void Fixture_ThrowsExpectedError(string templatePath, string dataPath, string errorPath)
    {
        var template = File.ReadAllText(templatePath);
        var expectedError = File.ReadAllText(errorPath).Trim();

        var exception = Should.Throw<TemplateParseException>(
            () => Template.Create(template).Render(ReadData(dataPath))
        );

        exception.Message.ShouldBe(expectedError);
    }
}