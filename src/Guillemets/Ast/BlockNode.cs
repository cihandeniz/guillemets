using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChain Properties, IReadOnlyList<INode> Body)
    : INode
{
    public string Render(RenderContext context, JsonElement data)
    {
        var value = context.PropertyResolver.Resolve(data, Properties).SingleOrDefault();

        return value.ValueKind == JsonValueKind.True
            ? context.Renderer.RenderAll(Body, data)
            : string.Empty;
    }
}