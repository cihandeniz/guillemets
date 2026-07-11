using Guillemets.Renderers;
using System.Text;
using System.Text.Json;

namespace Guillemets;

public static class TemplateEngine
{
    static readonly INodeRenderer LITERAL_NODE = new LiteralNodeRenderer();
    static readonly INodeRenderer TOKEN_NODE = new TokenNodeRenderer();

    public static string Render(string template, JsonElement data)
    {
        var tokens = Tokenizer.Tokenize(template);
        var nodes = Parser.Parse(tokens);
        var result = new StringBuilder();
        foreach (var node in nodes)
        {
            result.Append(Renderer(node).Render(node, data));
        }

        return result.ToString();
    }

    static INodeRenderer Renderer(Ast.INode node) =>
        node switch
        {
            Ast.LiteralNode => LITERAL_NODE,
            Ast.TokenNode => TOKEN_NODE,
            _ => throw new InvalidOperationException($"{node.GetType().Name} is not a supported node type")
        };
}
