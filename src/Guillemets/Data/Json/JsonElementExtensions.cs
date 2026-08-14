using Guillemets.Data.Json;
using System.Text.Json;

namespace Guillemets;

/// <summary>
/// <see cref="Template"/> extensions for rendering against
/// <see cref="JsonElement"/> data. Lives in the bare root
/// <c>Guillemets</c> namespace rather than <c>Guillemets.Data.Json</c> so
/// any consumer with <c>using Guillemets;</c> gets it for free.
/// </summary>
public static class JsonElementExtensions
{
    static readonly JsonElement EMPTY_OBJECT = JsonDocument.Parse("{}").RootElement;

    /// <summary>
    /// Renders <paramref name="template"/> against <paramref name="data"/>.
    /// A JSON <see langword="null"/> root is treated as an empty object,
    /// so any property looked up against it resolves the same way as an
    /// object simply missing that property, rather than the root's own
    /// nullness short-circuiting resolution.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="data">
    /// The JSON data to resolve template properties against.
    /// </param>
    /// <returns>The rendered output.</returns>
    public static string Render(this Template template, JsonElement data) =>
        template.Render(new JsonElementDataSource(data.ValueKind == JsonValueKind.Null ? EMPTY_OBJECT : data));
}