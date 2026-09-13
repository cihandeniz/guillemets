using Microsoft.Extensions.Localization;
using Shouldly;
using System.Text.Json;

namespace Guillemets.Tests;

public class SpecTests
{
    static readonly HashSet<string> IGNORED_FIXTURES =
        [
            "10-whitespace/002-line-wrap-drops-required-space",
            "02-conditional-blocks/001a-true",
            "02-conditional-blocks/001b-false",
            "02-conditional-blocks/002a-truthy",
            "02-conditional-blocks/002b-falsy",
            "02-conditional-blocks/003-null-object-else",
            "02-conditional-blocks/004-unresolved-property-no-else",
            "02-conditional-blocks/005a-both-truthy",
            "02-conditional-blocks/005b-inner-falsy",
            "02-conditional-blocks/005c-outer-falsy",
            "02-conditional-blocks/006a-truthy",
            "02-conditional-blocks/006b-falsy",
            "02-conditional-blocks/007a-truthy",
            "02-conditional-blocks/007b-falsy",
            "02-conditional-blocks/008-three-level-nesting",
            "02-conditional-blocks/009-corrupted-filter-syntax-in-body",
            "02-conditional-blocks/010-negation-of-non-boolean",
            "02-conditional-blocks/011-no-trailing-newline-at-eof",
            "02-conditional-blocks/012-tilde-in-text-is-literal",
            "02-conditional-blocks/013-max-guillemet-depth",
            "03-loop-blocks/001a-populated",
            "03-loop-blocks/001b-empty",
            "03-loop-blocks/002-magic-loop-vars",
            "03-loop-blocks/003-negation",
            "03-loop-blocks/004-filtered-item-scope",
            "03-loop-blocks/005-filtered-item-scope-negated",
            "03-loop-blocks/006-filtered-item-scope-no-match",
            "03-loop-blocks/007-empty-list-else",
            "03-loop-blocks/008-magic-var-property-collision",
            "03-loop-blocks/009-doubly-flattened-header",
            "03-loop-blocks/010-nested-loop-first-last",
            "03-loop-blocks/011-first-last-after-filter",
            "03-loop-blocks/012-filtered-item-scope-sparse-flag",
            "03-loop-blocks/013-filtered-item-scope-nested-chain",
            "03-loop-blocks/014-filtered-item-scope-pinned",
            "04-scope-blocks/001-object-scope",
            "04-scope-blocks/002-upper-scope-fallback",
            "04-scope-blocks/003-magic-var-through-nested-scope",
            "05-variable-definitions/001-definition-boolean",
            "05-variable-definitions/002-definition-object",
            "05-variable-definitions/003-definition-list-join",
            "05-variable-definitions/004a-populated",
            "05-variable-definitions/004b-empty",
            "05-variable-definitions/005-variable-shadows-property",
            "05-variable-definitions/006-definition-list-multi-filter-footer",
            "05-variable-definitions/007-definition-scope-boundaries",
            "06-tables/001-table-block",
            "06-tables/002-multi-footer-rows",
            "06-tables/003-no-footer-rows",
            "06-tables/004-below-minimum-rows",
            "06-tables/005-footer-scope-fallback",
            "06-tables/006-mismatched-columns",
            "06-tables/007-line-before-close-becomes-footer-row",
            "06-tables/008-filter-footer-glued-to-close",
            "08-filters/005-join-default-block-footer",
            "99-errors/003-mismatched-block-depth",
            "99-errors/019-missing-blank-before-else",
            "99-errors/020-missing-blank-after-else",
            "99-errors/021-missing-blank-before-footer",
            "11-escaping/002-close-inside-block",
            "11-escaping/006-close-run-inside-deeper-block",
            "11-escaping/007-escaped-tilde",
            "10-whitespace/001-crlf",
            "12-glossary-localization/005-block-header",
            "13-scope-navigation/001-this-scope-only-reaches-own-property-over-magic-var",
            "13-scope-navigation/002-this-scope-only-skips-fallback",
            "13-scope-navigation/003-climb-to-parent-scope",
            "13-scope-navigation/004-climb-two-levels",
            "13-scope-navigation/005-combine-climb-and-this-scope-only",
            "13-scope-navigation/006-negation-with-navigator",
            "13-scope-navigation/007-filter-with-navigator",
            "13-scope-navigation/008-navigator-as-block-header",
            "13-scope-navigation/010-climb-past-available-nesting-resolves-to-nothing",
            "13-scope-navigation/013-this-scope-only-skips-defined-variable",
        ];

    static IEnumerable<TestCaseData> FixtureCases() =>
        CaseFiles(".md").Select(TestCaseFor);

    static IEnumerable<TestCaseData> ErrorFixtureCases() =>
        CaseFiles(".error").Select(TestCaseFor);

    static TestCaseData TestCaseFor(string path)
    {
        var testCase = new TestCaseData(TemplateFor(path), DataPathFor(path), LocalizerFor(path), path).SetName(FixtureName(path));

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

    static IStringLocalizer? LocalizerFor(string casePath)
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
    public void Fixture_renders_expected_output(string templatePath, string dataPath, IStringLocalizer? localizer, string expectedPath)
    {
        var template = File.ReadAllText(templatePath);
        var expected = File.ReadAllText(expectedPath);

        var actual = Template.Create(template, options => options.Localizer = localizer).Render(ReadData(dataPath));

        actual.ShouldBe(expected);
    }

    [TestCaseSource(nameof(ErrorFixtureCases))]
    public void Fixture_throws_expected_error(string templatePath, string dataPath, IStringLocalizer? localizer, string errorPath)
    {
        var template = File.ReadAllText(templatePath);
        var expectedError = File.ReadAllText(errorPath).Trim();

        var exception = Should.Throw<TemplateParseException>(
            () => Template.Create(template, options => options.Localizer = localizer).Render(ReadData(dataPath))
        );

        exception.Message.ShouldBe(expectedError);
    }
}