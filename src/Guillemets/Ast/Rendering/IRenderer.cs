using System.Text.Json;

namespace Guillemets.Ast.Rendering;

internal interface IRenderer
{
    string RenderAll(IReadOnlyList<INode> nodes, JsonElement data);
}