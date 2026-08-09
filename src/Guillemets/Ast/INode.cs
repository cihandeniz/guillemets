using Guillemets.Ast.Rendering;

namespace Guillemets.Ast;

internal interface INode
{
    string Render(RenderContext context, Scope scope);
}