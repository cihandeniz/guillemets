namespace Guillemets.Filters;

/// <summary>
/// Where a bare filter stage (no <c>: arg</c>) was written — see
/// <see cref="IFilter.GetDefaultArg"/>.
/// </summary>
public enum FilterContext
{
    /// <summary>
    /// Written inline within a variable, e.g. <c>«tags / join»</c>.
    /// </summary>
    Inline,

    /// <summary>
    /// Written as a block's footer, the last line before its close.
    /// </summary>
    Footer,
}