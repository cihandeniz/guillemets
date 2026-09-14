using static Guillemets.Position;

namespace Guillemets.Filters;

/// <summary>
/// Collapses a list via plain string concatenation (<c>string.Join</c>) —
/// no locale-aware list formatting beyond what the template itself
/// writes. With no argument, defaults to <c>, </c> inline, <c> | </c> in a
/// table cell and a newline in a block footer (see
/// <see cref="GetDefaultArg"/>).
/// </summary>
public class JoinFilter : IFilter
{
    const string INLINE_DEFAULT = ", ";
    const string TABLE_CELL_DEFAULT = " | ";

    /// <inheritdoc/>
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg)
    {
        var materialized = values.ToList();

        return materialized.Count > 0 ? [string.Join(arg, materialized)] : materialized;
    }

    /// <inheritdoc/>
    public string GetDefaultArg(FilterContext context) => context switch
    {
        FilterContext.Inline => INLINE_DEFAULT,
        FilterContext.TableCell => TABLE_CELL_DEFAULT,
        FilterContext.Footer => NEWLINE.ToString(),
        _ => NEWLINE.ToString(),
    };
}