namespace Guillemets.Filters;

/// <summary>
/// One pipeline stage behind <c>«expr / filter: arg»</c> — a sequence-in,
/// sequence-out transform. Register a custom implementation via
/// <see cref="FilterRegistry.Register{TFilter}"/>.
/// </summary>
public interface IFilter
{
    /// <summary>
    /// Transforms <paramref name="values"/>. A single-value filter (e.g.
    /// <see cref="DateFilter"/>) returns one string per input; a
    /// collapsing filter (e.g. <see cref="JoinFilter"/>) returns fewer
    /// strings than it received.
    /// </summary>
    /// <param name="values">
    /// The values flowing through the pipeline so far.
    /// </param>
    /// <param name="arg">
    /// The <c>: arg</c> text following the filter name, or
    /// <see langword="null"/> if omitted.
    /// </param>
    /// <returns>The transformed values.</returns>
    IEnumerable<string> Apply(IEnumerable<string> values, string? arg);

    /// <summary>
    /// The argument to use when this filter is written with no <c>: arg</c>
    /// at all. A bare stage can mean something different depending on
    /// where it's written — <see cref="JoinFilter"/> overrides this to
    /// return <c>, </c> inline but a newline in a block footer. Most
    /// filters don't override it and stay context-free.
    /// </summary>
    /// <param name="context">Where the bare filter stage was written.</param>
    string? GetDefaultArg(FilterContext context) =>
        null;
}