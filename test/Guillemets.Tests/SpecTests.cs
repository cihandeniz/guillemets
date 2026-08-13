using Microsoft.Extensions.Localization;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class SpecTests
{
    static readonly HashSet<string> IGNORED_FIXTURES = [
        "14-scope-navigation/003-climb-to-parent-scope",
        "14-scope-navigation/004-climb-two-levels",
        "14-scope-navigation/005-combine-climb-and-this-scope-only",
        "14-scope-navigation/006-negation-with-navigator",
        "14-scope-navigation/007-filter-with-navigator",
        "14-scope-navigation/008-navigator-as-block-header",
        "14-scope-navigation/009-climb-past-root-scope-resolves-to-nothing",
        "14-scope-navigation/010-climb-past-available-nesting-resolves-to-nothing",
        "14-scope-navigation/011-this-scope-only-not-last-before-chain",
        "14-scope-navigation/012-this-scope-only-repeated",
    ];

    static IEnumerable<TestCaseData> FixtureCases() =>
        CaseFiles(".md").Select(TestCaseFor);

    static IEnumerable<TestCaseData> ErrorFixtureCases() =>
        CaseFiles(".error").Select(TestCaseFor);

    static TestCaseData TestCaseFor(string path)
    {
        var testCase = new TestCaseData(TemplateFor(path), DataPathFor(path), GlossaryFor(path), path).SetName(FixtureName(path));

        return IGNORED_FIXTURES.Contains(FixtureName(path)) ? testCase.Ignore("not yet implemented") : testCase;
    }

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

    static JsonElement ReadData(string dataPath)
    {
        using var document = JsonDocument.Parse(File.Exists(dataPath) ? File.ReadAllText(dataPath) : "{}");

        return document.RootElement.Clone();
    }

    static IStringLocalizer? GlossaryFor(string casePath)
    {
        var basePath = BasePath(casePath);
        var directory = Path.GetDirectoryName(basePath)
            ?? throw new InvalidOperationException($"Fixture case path '{casePath}' has no directory.");
        var baseName = Path.GetFileName(basePath);

        var entries = Directory.EnumerateFiles(directory, $"{baseName}.*.json")
            .SelectMany(path => JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? throw new InvalidOperationException($"Glossary sidecar '{path}' did not deserialize to a JSON object."))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        return entries.Count == 0 ? null : new FakeStringLocalizer(entries);
    }

    [TestCaseSource(nameof(FixtureCases))]
    public void Fixture_renders_expected_output(string templatePath, string dataPath, IStringLocalizer? glossary, string expectedPath)
    {
        var template = File.ReadAllText(templatePath);
        var expected = File.ReadAllText(expectedPath);

        var actual = Template.Create(template, options => options.Glossary = glossary).Render(ReadData(dataPath));

        actual.ShouldBe(expected);
    }

    [TestCaseSource(nameof(ErrorFixtureCases))]
    public void Fixture_throws_expected_error(string templatePath, string dataPath, IStringLocalizer? glossary, string errorPath)
    {
        var template = File.ReadAllText(templatePath);
        var expectedError = File.ReadAllText(errorPath).Trim();

        var exception = Should.Throw<TemplateParseException>(
            () => Template.Create(template, options => options.Glossary = glossary).Render(ReadData(dataPath))
        );

        exception.Message.ShouldBe(expectedError);
    }
}