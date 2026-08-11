using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Parsing;
using Guillemets.Tokenization;

namespace Guillemets;

public class Template
{
    readonly IReadOnlyList<INode> _nodes;

    internal Template(IReadOnlyList<INode> nodes) =>
        _nodes = nodes;

    public static Template Create(string template)
    {
        var tokens = new Tokenizer(template, Symbols.TREE).Tokenize();
        var nodes = new Parser(tokens).Parse();

        return new Template(nodes);
    }

    public string Render(IDataSource data)
    {
        var variables = new VariableStore();
        var propertyResolver = new PropertyResolver(variables);
        var renderer = new Renderer(propertyResolver, variables);

        return renderer.Render(_nodes, data);
    }
}
