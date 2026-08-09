using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record BlockNode(PropertyChain Properties, IReadOnlyList<INode> Body,
    IReadOnlyList<INode>? ElseBody = null
) : INode
{
    public string Render(RenderContext context, JsonElement data)
    {
        var value = context.PropertyResolver.Resolve(data, Properties).SingleOrDefault();

        if (value.ValueKind == JsonValueKind.True)
        {
            return context.Renderer.RenderAll(Body, data);
        }

        return ElseBody is not null
            ? context.Renderer.RenderAll(ElseBody, data)
            : string.Empty;
    }
}