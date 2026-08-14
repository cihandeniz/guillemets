using Guillemets.Data.Poco;

namespace Guillemets;

/// <summary>
/// <see cref="Template"/> extension for rendering against a plain C#
/// object via reflection. Lives in the bare root <c>Guillemets</c>
/// namespace rather than <c>Guillemets.Data.Poco</c> so any consumer with
/// <c>using Guillemets;</c> gets it for free.
/// </summary>
public static class PocoExtensions
{
    /// <summary>
    /// Renders <paramref name="template"/> against <paramref name="data"/>.
    /// Named <c>RenderObject</c> rather than overloading <c>Render</c> —
    /// <see langword="object"/> is broad enough that folding it into the
    /// same overload set would blur which overload a call actually hits.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="data">
    /// The object to resolve template properties against.
    /// </param>
    /// <returns>The rendered output.</returns>
    public static string RenderObject(this Template template, object data) =>
        template.Render(new PocoDataSource(data));
}