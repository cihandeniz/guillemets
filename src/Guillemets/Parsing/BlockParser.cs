using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Position;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class BlockParser(TokenCursor _tokens, ParserRegistry _registry)
{
    static void ValidateClosingDepth(Token open, Token close)
    {
        if (close.Depth == open.Depth) { return; }

        throw new TemplateParseException(
            $"Block opened with {open.Text} but closed with {close.Text.TrimEnd(NEWLINE)}",
            close.Position
        );
    }

    static void ValidateNotSharingCloseLine(List<IRenderable> body, Token close)
    {
        if (BodyParser.AtLineStart(body)) { return; }

        throw new TemplateParseException("A literal may not share a line with the block's closing »»", close.Position);
    }

    readonly Lazy<BodyParser> _lazyBodyParser = _registry.GetLazy<BodyParser>();
    readonly Lazy<PropertyChainParser> _lazyPropertyChainParser = _registry.GetLazy<PropertyChainParser>();

    BodyParser BodyParser => _lazyBodyParser.Value;
    PropertyChainParser PropertyChainParser => _lazyPropertyChainParser.Value;

    public IRenderable Parse(Token open)
    {
        _tokens.Advance();

        var properties = PropertyChainParser.Parse(open.Position, stopAtNewline: true, out var variableName);
        var truthy = ParseBody(stopAtElse: true, out var footer);

        List<IRenderable>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current.Kind is Else)
        {
            _tokens.Advance();
            falsy = ParseBody(stopAtElse: false, out footer);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        ValidateClosingDepth(open, _tokens.Current);
        _tokens.Advance();

        return new BlockNode(properties, truthy,
            ElseBody: falsy,
            VariableName: variableName,
            Footer: footer
        );
    }

    List<IRenderable> ParseBody(bool stopAtElse, out IReadOnlyList<FilterNode> footer)
    {
        var body = BodyParser.Parse(insideBlock: true, stopAtElse: stopAtElse, out footer);
        if (!_tokens.AtEnd && _tokens.Current.Kind is CloseBlock)
        {
            ValidateNotSharingCloseLine(body, _tokens.Current);
        }

        return body;
    }
}