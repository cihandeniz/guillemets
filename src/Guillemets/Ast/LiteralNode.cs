using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal record LiteralNode(string Text)
    : INode
{
    public string Render(RenderContext context, JsonElement data) =>
        Text;
}