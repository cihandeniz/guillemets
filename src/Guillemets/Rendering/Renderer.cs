using Guillemets.Ast;
using Guillemets.Data;
using System.Text;

namespace Guillemets.Rendering;

internal class Renderer : IRenderer
{
    readonly RenderContext _context;

    public Renderer(PropertyResolver propertyResolver, VariableStore variables) =>
        _context = new(propertyResolver, this, variables);

    public string Render(IReadOnlyList<INode> nodes, IDataSource data) =>
        RenderAll(nodes, new Scope(data));

    string IRenderer.RenderAll(IReadOnlyList<INode> nodes, Scope scope) =>
        RenderAll(nodes, scope);

    string RenderAll(IReadOnlyList<INode> nodes, Scope scope)
    {
        var result = new StringBuilder();
        foreach (var node in nodes)
        {
            result.Append(node.Render(_context, scope));
        }

        return result.ToString();
    }
}