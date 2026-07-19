using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record TokenNode(PropertyChain Properties)
    : INode
{
    public string Render(RenderContext context, JsonElement data) =>
        string.Join(", ", context.PropertyResolver.Resolve(data, Properties).Select(value => value.ToString()));
}