using Guillemets.Ast;

namespace Guillemets.Rendering;

internal interface IRenderer
{
    string RenderAll(IReadOnlyList<INode> nodes, Scope scope);
}