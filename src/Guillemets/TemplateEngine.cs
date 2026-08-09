using Guillemets.Ast;
using Guillemets.Ast.Rendering;
using Guillemets.Tokenization;
using System.Text;
using System.Text.Json;

namespace Guillemets;

public class TemplateEngine : IRenderer
{
    static readonly JsonElement EMPTY_OBJECT = JsonDocument.Parse("{}").RootElement;

    public static string Render(string template, JsonElement data)
    {
        var tokens = new Tokenizer(template, Symbols.TREE).Tokenize();
        var nodes = new Parser(tokens).Parse();
        IRenderer engine = new TemplateEngine(new());
        var scope = new Scope(data.ValueKind == JsonValueKind.Null ? EMPTY_OBJECT : data);

        return engine.RenderAll(nodes, scope);
    }

    readonly RenderContext _context;

    TemplateEngine(PropertyResolver propertyResolver)
    {
        _context = new(propertyResolver, this);
    }

    string IRenderer.RenderAll(IReadOnlyList<INode> nodes, Scope scope)
    {
        var result = new StringBuilder();
        foreach (var node in nodes)
        {
            result.Append(node.Render(_context, scope));
        }

        return result.ToString();
    }
}