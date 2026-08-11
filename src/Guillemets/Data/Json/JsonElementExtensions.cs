using Guillemets.Data.Json;
using System.Text.Json;

namespace Guillemets;

public static class JsonElementExtensions
{
    static readonly JsonElement EMPTY_OBJECT = JsonDocument.Parse("{}").RootElement;

    public static string Render(this Template template, JsonElement data) =>
        template.Render(new JsonElementDataSource(data.ValueKind == JsonValueKind.Null ? EMPTY_OBJECT : data));
}