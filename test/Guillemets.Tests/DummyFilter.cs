using Guillemets.Filters;

namespace Guillemets.Tests;

class DummyFilter : IFilter
{
    public string Apply(IReadOnlyList<string> values, IReadOnlyList<string> args) =>
        string.Empty;
}