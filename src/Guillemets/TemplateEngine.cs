using Guillemets.Renderers;
using System.Text;
using System.Text.Json;

namespace Guillemets;

public static class TemplateEngine
{
    static readonly Dictionary<Type, INodeRenderer> Renderers = new()
    {
        [typeof(Ast.LiteralNode)] = new LiteralNodeRenderer(),
        [typeof(Ast.TokenNode)] = new TokenNodeRenderer(),
    };

    public static string Render(string template, JsonElement data)
    {
        var nodes = Tokenizer.Tokenize(template);
        var result = new StringBuilder();
        foreach (var node in nodes)
        {
            result.Append(Renderer(node).Render(node, data));
        }

        return result.ToString();
    }

    static INodeRenderer Renderer(Ast.INode node) =>
        Renderers[node.GetType()];
}
