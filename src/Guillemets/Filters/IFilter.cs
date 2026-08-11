namespace Guillemets.Filters;

internal interface IFilter
{
    string Apply(IReadOnlyList<string> values, IReadOnlyList<string> args);
}