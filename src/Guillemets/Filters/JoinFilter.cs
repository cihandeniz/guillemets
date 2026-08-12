using static Guillemets.Position;

namespace Guillemets.Filters;

internal class JoinFilter : IFilter
{
    const string INLINE_DEFAULT = ", ";

    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        values.Any() ? [string.Join(arg, values)] : values;

    public string GetDefaultArg(FilterContext context) =>
        context == FilterContext.Inline ? INLINE_DEFAULT : NEWLINE.ToString();
}