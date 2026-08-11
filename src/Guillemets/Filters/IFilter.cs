namespace Guillemets.Filters;

public interface IFilter
{
    string Apply(IReadOnlyList<string> values, IReadOnlyList<string> args);
}