using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Parsing;
using Guillemets.Rendering;
using Guillemets.Tokenization;

namespace Guillemets;

/// <summary>
/// A parsed template, ready to render against any number of data sources.
/// Immutable and stateless once created, so a single instance is safe to
/// reuse — including concurrently across threads — for every
/// <see cref="Render"/> call.
/// </summary>
public class Template
{
    const string CRLF = "\r\n";

    /// <summary>
    /// Parses <paramref name="template"/> into a reusable
    /// <see cref="Template"/>.
    /// </summary>
    /// <param name="template">
    /// The template source, using «» delimiters (see specs.md).
    /// </param>
    /// <param name="configure">
    /// Optional callback to register custom filters
    /// (<see cref="ParseOptions.Filters"/>) or configure a glossary
    /// (<see cref="ParseOptions.Localizer"/>) before parsing.
    /// </param>
    /// <exception cref="TemplateParseException">
    /// The template violates a MUST rule of the template language.
    /// </exception>
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

    /// <summary>
    /// Renders this template against <paramref name="data"/>.
    /// <see cref="JsonElementExtensions"/>, <see cref="PocoExtensions"/>,
    /// and <see cref="JTokenExtensions"/> wrap this for
    /// <c>JsonElement</c>, plain C# objects, and Newtonsoft
    /// <c>JToken</c> respectively, so most callers use one of those instead
    /// of constructing an <see cref="IDataSource"/> directly.
    /// </summary>
    /// <param name="data">
    /// The data to resolve template properties against.
    /// </param>
    /// <returns>The rendered output.</returns>
    public string Render(IDataSource data)
    {
        var glossary = Glossary.GetOrCreate(_options.Localizer, _options.PropertyNameConversion, _options.GlossaryCollisionResolver);
        var propertyResolver = new PropertyResolver(glossary);
        var renderer = new Renderer(propertyResolver);

        var rendered = renderer.Render(_nodes, new(data, Glossary: glossary));

        return _lineEnding == CRLF ? rendered.Replace(Position.NEWLINE.ToString(), CRLF) : rendered;
    }
}