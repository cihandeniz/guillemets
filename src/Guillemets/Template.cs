using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Parsing;
using Guillemets.Rendering;
using Guillemets.Tokenization;
using Microsoft.Extensions.Localization;

namespace Guillemets;

public class Template
{
    const string CRLF = "\r\n";

    public static Template Create(string template, Action<ParseOptions>? configure = null)
    {
        var options = new ParseOptions();
        configure?.Invoke(options);

        var lineEnding = template.Contains(CRLF) ? CRLF : Position.NEWLINE.ToString();
        var normalized = template.Replace(CRLF, Position.NEWLINE.ToString());
        var tokens = new Tokenizer(normalized, Symbols.TREE).Tokenize();
        var nodes = new Parser(tokens, options.Filters).Parse();

        return new(nodes, lineEnding, options.Glossary);
    }

    readonly IReadOnlyList<IRenderable> _nodes;
    readonly string _lineEnding;
    readonly IStringLocalizer? _localizer;

    internal Template(IReadOnlyList<IRenderable> nodes, string lineEnding, IStringLocalizer? localizer) =>
        (_nodes, _lineEnding, _localizer) = (nodes, lineEnding, localizer);

    public string Render(IDataSource data)
    {
        var glossary = Glossary.GetOrCreate(_localizer);
        var variables = new VariableStore();
        var propertyResolver = new PropertyResolver(variables, glossary);
        var renderer = new Renderer(propertyResolver, variables);

        var rendered = renderer.Render(_nodes, new(data, Glossary: glossary));

        return _lineEnding == CRLF ? rendered.Replace(Position.NEWLINE.ToString(), CRLF) : rendered;
    }
}