using static Guillemets.Position;

namespace Guillemets.Filters;

public class JoinFilter : IFilter
{
    const string INLINE_DEFAULT = ", ";

    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg)
    {
        var materialized = values.ToList();

        return materialized.Count > 0 ? [string.Join(arg, materialized)] : materialized;
    }

    public string GetDefaultArg(FilterContext context) =>
        context == FilterContext.Inline ? INLINE_DEFAULT : NEWLINE.ToString();
}