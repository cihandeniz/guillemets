namespace Guillemets.Filters;

internal class SeparatorFilter : IFilter
{
    public string Apply(IReadOnlyList<string> values, IReadOnlyList<string> args) =>
        string.Join(args[0], values);
}