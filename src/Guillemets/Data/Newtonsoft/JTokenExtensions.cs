using Guillemets.Data.Newtonsoft;
using Newtonsoft.Json.Linq;

namespace Guillemets;

/// <summary>
/// <see cref="Template"/> extensions for rendering against
/// Newtonsoft <see cref="JToken"/> data. Lives in the bare root
/// <c>Guillemets</c> namespace rather than <c>Guillemets.Data.Newtonsoft</c>
/// so any consumer with <c>using Guillemets;</c> gets it for free.
/// </summary>
public static class JTokenExtensions
{
    /// <summary>
    /// Renders <paramref name="template"/> against <paramref name="data"/>.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="data">
    /// The JSON data to resolve template properties against.
    /// </param>
    /// <returns>The rendered output.</returns>
    public static string Render(this Template template, JToken data) =>
        template.Render(new JTokenDataSource(data));
}