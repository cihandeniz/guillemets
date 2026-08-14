using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Parsing;
using Guillemets.Rendering;
using Guillemets.Tokenization;

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

        return new(nodes, lineEnding, options);
    }

    readonly IReadOnlyList<IRenderable> _nodes;
    readonly string _lineEnding;
    readonly ParseOptions _options;

    internal Template(IReadOnlyList<IRenderable> nodes, string lineEnding, ParseOptions options)
    {
        _nodes = nodes;
        _lineEnding = lineEnding;
        _options = options;
    }

    public string Render(IDataSource data)
    {
        var glossary = Glossary.GetOrCreate(_options.Localizer, _options.PropertyNameConversion, _options.GlossaryCollisionResolver);
        var propertyResolver = new PropertyResolver(glossary);
        var renderer = new Renderer(propertyResolver);

        var rendered = renderer.Render(_nodes, new(data, Glossary: glossary));

        return _lineEnding == CRLF ? rendered.Replace(Position.NEWLINE.ToString(), CRLF) : rendered;
    }
}