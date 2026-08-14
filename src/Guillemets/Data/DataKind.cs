namespace Guillemets.Data;

/// <summary>
/// The shape an <see cref="IDataSource"/> resolves to — what drives a
/// block's inferred behavior (scope/loop/conditional) and a bare value's
/// truthiness (see specs.md's Blocks section).
/// </summary>
public enum DataKind
{
    /// <summary>
    /// An object with properties. A block keyed on one becomes a scope.
    /// </summary>
    Object,

    /// <summary>A list. A block keyed on one becomes a loop.</summary>
    Array,

    /// <summary>
    /// A scalar string. Truthy whenever present, regardless of content
    /// (including <c>""</c>).
    /// </summary>
    String,

    /// <summary>
    /// A scalar number. Truthy whenever present, regardless of content
    /// (including <c>0</c>).
    /// </summary>
    Number,

    /// <summary>
    /// A scalar boolean. A block keyed on one becomes a conditional.
    /// </summary>
    Boolean,

    /// <summary>An explicit null value. Always falsy.</summary>
    Null,

    /// <summary>
    /// A property chain that didn't resolve to anything. Always falsy.
    /// </summary>
    Undefined,
}