using System.Text.Json;

using static Guillemets.Ast;

namespace Guillemets.Renderers;

internal interface INodeRenderer
{
    string Render(INode node, JsonElement data);
}
