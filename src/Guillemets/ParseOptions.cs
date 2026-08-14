using Guillemets.Filters;
using Microsoft.Extensions.Localization;

namespace Guillemets;

/// <summary>
/// Configures a <see cref="Template.Create"/> call — filters, glossary,
/// and property-name resolution.
/// </summary>
public class ParseOptions
{
    /// <summary>
    /// The filter registry this template's filters resolve against.
    /// Starts out with the built-ins registered; <c>Register</c> adds a
    /// custom filter alongside them (re-registering an existing name
    /// replaces it) and <c>Remove&lt;TFilter&gt;</c> drops one entirely.
    /// </summary>
    public FilterRegistry Filters { get; } = FilterRegistry.CreateDefault();

    /// <summary>
    /// The glossary backing this template's «natural word» → property-name
    /// resolution, or <see langword="null"/> for direct resolution only.
    /// Any <c>IStringLocalizer</c> works — a <c>.resx</c> file, a database,
    /// or a translation service.
    /// </summary>
    public IStringLocalizer? Localizer { get; set; }

    /// <summary>
    /// Turns a glossary entry's <c>Name</c> — or, when nothing matches, the
    /// template segment itself — into the actual property name. Defaults to
    /// <c>Dehumanize()</c> (<c>full name</c> → <c>FullName</c>); setting
    /// this replaces the default outright rather than layering on top of it.
    /// </summary>
    public Func<string, string> PropertyNameConversion { get; set; } = TextCasing.Dehumanize;

    /// <summary>
    /// Resolves a glossary ambiguity — two entries translating to the same
    /// term — by receiving the colliding entries' <c>Name</c>s and
    /// returning the one that should win. Leave <see langword="null"/> to
    /// throw a <see cref="Rendering.GlossaryException"/> on
    /// collision instead.
    /// </summary>
    public Func<IEnumerable<string>, string>? GlossaryCollisionResolver { get; set; }
}