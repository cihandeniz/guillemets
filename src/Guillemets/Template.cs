using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Filters;
using Guillemets.Parsing;
using Guillemets.Rendering;
using Guillemets.Tokenization;

namespace Guillemets;

public class Template
{
    const string CRLF = "\r\n";

    public static Template Create(string template) =>
        Create(template, static _ => { });

    public static Template Create(string template, Action<FilterRegistry> configureFilters)
    {
        var lineEnding = template.Contains(CRLF) ? CRLF : Position.NEWLINE.ToString();
        var normalized = template.Replace(CRLF, Position.NEWLINE.ToString());

        var tokens = new Tokenizer(normalized, Symbols.TREE).Tokenize();
        var filters = FilterRegistry.CreateDefault(configureFilters);
        var nodes = new Parser(tokens, filters).Parse();

        return new Template(nodes, lineEnding);
    }

    readonly IReadOnlyList<IRenderable> _nodes;
    readonly string _lineEnding;

    internal Template(IReadOnlyList<IRenderable> nodes, string lineEnding) =>
        (_nodes, _lineEnding) = (nodes, lineEnding);

    public string Render(IDataSource data)
    {
        var variables = new VariableStore();
        var propertyResolver = new PropertyResolver(variables);
        var renderer = new Renderer(propertyResolver, variables);

        var rendered = renderer.Render(_nodes, new(data));

        return _lineEnding == CRLF ? rendered.Replace(Position.NEWLINE.ToString(), CRLF) : rendered;
    }
}