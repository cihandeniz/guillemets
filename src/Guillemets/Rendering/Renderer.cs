using Guillemets.Ast;
using System.Text;

namespace Guillemets.Rendering;

internal class Renderer
{
    readonly RenderContext _context;

    public Renderer(PropertyResolver propertyResolver) =>
        _context = new(propertyResolver, this);

    public string Render(IReadOnlyList<IRenderable> nodes, Scope scope)
    {
        var result = new StringBuilder();
        foreach (var node in nodes)
        {
            result.Append(node.Render(_context, scope));
        }

        return result.ToString();
    }
}