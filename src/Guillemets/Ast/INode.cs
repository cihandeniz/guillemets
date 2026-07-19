using Guillemets.Ast.Rendering;
using System.Text.Json;

namespace Guillemets.Ast;

internal interface INode
{
    string Render(RenderContext context, JsonElement data);
}