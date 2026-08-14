using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class VariableParser(TokenCursor _tokens, ParserRegistry _registry)
{
    readonly Lazy<PropertyChainParser> _lazyPropertyChainParser = _registry.GetLazy<PropertyChainParser>();
    readonly Lazy<FilterParser> _lazyFilterParser = _registry.GetLazy<FilterParser>();

    PropertyChainParser PropertyChainParser => _lazyPropertyChainParser.Value;
    FilterParser FilterParser => _lazyFilterParser.Value;

    public IRenderable Parse(Token open)
    {
        _tokens.Advance();
        var chain = PropertyChainParser.Parse(open.Position, stopAtNewline: false, stopAtDelimiter: true);
        var filters = FilterParser.Parse(expectLeadingDelimiter: true);
        if (_tokens.AtEnd || _tokens.Current.Kind is not Close)
        {
            throw new TemplateParseException("Unclosed variable", open.Position);
        }
        _tokens.Advance();

        return new VariableNode(chain, filters);
    }
}