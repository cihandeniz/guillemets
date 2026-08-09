namespace Guillemets.Ast.Rendering;

internal interface IRenderer
{
    string RenderAll(IReadOnlyList<INode> nodes, Scope scope);
}