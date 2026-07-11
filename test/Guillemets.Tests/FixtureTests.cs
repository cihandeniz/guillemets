using System.Text.Json;
using Guillemets;

namespace Guillemets.Tests;

public class FixtureTests
{
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

            yield return new TestCaseData(templatePath, dataPath, expectedPath).SetName(relativeName);
        }
    }

    [TestCaseSource(nameof(FixtureCases))]
    public void Fixture_RendersExpectedOutput(string templatePath, string dataPath, string expectedPath)
    {
        var template = File.ReadAllText(templatePath);
        var expected = File.ReadAllText(expectedPath);
        using var dataDoc = JsonDocument.Parse(File.ReadAllText(dataPath));

        var actual = TemplateEngine.Render(template, dataDoc.RootElement);

        Assert.That(actual, Is.EqualTo(expected));
    }
}
