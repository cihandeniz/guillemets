namespace Guillemets.Filters;

/// <summary>
/// Substitutes its argument for any value that would otherwise render as
/// empty — an unresolved chain, or a property whose own value is empty
/// (an explicit null, or an empty string). A resolved, non-empty value
/// passes through unchanged.
/// </summary>
public class DefaultFilter : IFilter
{
    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg)
    {
        arg ??= string.Empty;

        var replaced = values.Select(value => string.IsNullOrEmpty(value) ? arg : value).ToList();

        return replaced.Count == 0 ? [arg] : replaced;
    }
}