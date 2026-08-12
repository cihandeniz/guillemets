using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class VariableParser(TokenCursor _tokens, ParserRegistry _registry)
{
    readonly Lazy<PropertyChainParser> _lazyPropertyChainParser = _registry.GetLazy<PropertyChainParser>();
    readonly Lazy<FilterParser> _lazyFilterParser = _registry.GetLazy<FilterParser>();

    PropertyChainParser PropertyChainParser => _lazyPropertyChainParser.Value;
    FilterParser FilterParser => _lazyFilterParser.Value;

    public IRenderable Parse(IToken token)
    {
        var open = (OpenToken)token;
        _tokens.Advance();
        var chain = PropertyChainParser.Parse(open.Position, stopAtNewline: false, stopAtPipe: true);
        var filters = FilterParser.ParsePipeline();
        if (_tokens.AtEnd || _tokens.Current is not CloseToken)
        {
            throw new TemplateParseException("Unclosed variable", open.Position);
        }
        _tokens.Advance();

        return new VariableNode(chain, filters);
    }
}