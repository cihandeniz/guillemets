using Guillemets.Ast;
using Guillemets.Ast.Rendering;
using System.Text;
using System.Text.Json;

namespace Guillemets;

public class TemplateEngine : IRenderer
{
    public static string Render(string template, JsonElement data)
    {
        var tokens = new Tokenizer(template).Tokenize();
        var nodes = new Parser(tokens).Parse();
        IRenderer engine = new TemplateEngine(new());

        return engine.RenderAll(nodes, data);
    }

    readonly RenderContext _context;

    TemplateEngine(PropertyResolver propertyResolver)
    {
        _context = new(propertyResolver, this);
    }

    string IRenderer.RenderAll(IReadOnlyList<INode> nodes, JsonElement data)
    {
        var result = new StringBuilder();
        foreach (var node in nodes)
        {
            result.Append(node.Render(_context, data));
        }

        return result.ToString();
    }
}