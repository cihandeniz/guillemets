using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;

namespace Guillemets.Parsing;

internal class BlockParser(TokenCursor _tokens, ParserRegistry _registry)
{
    static void ValidateClosingDepth(OpenBlockToken open, CloseBlockToken close)
    {
        if (close.Depth == open.Depth) { return; }

        throw new TemplateParseException(
            $"Block opened with {open.Text} but closed with {close.Text.TrimEnd(NEWLINE)}",
            close.Position
        );
    }

    static void ValidateNotSharingCloseLine(List<IRenderable> body, IToken close)
    {
        if (BodyParser.AtLineStart(body)) { return; }

        throw new TemplateParseException("A literal may not share a line with the block's closing »»", close.Position);
    }

    readonly Lazy<BodyParser> _lazyBodyParser = _registry.GetLazy<BodyParser>();
    readonly Lazy<PropertyChainParser> _lazyPropertyChainParser = _registry.GetLazy<PropertyChainParser>();

    BodyParser BodyParser => _lazyBodyParser.Value;
    PropertyChainParser PropertyChainParser => _lazyPropertyChainParser.Value;

    public IRenderable Parse(IToken token)
    {
        var open = (OpenBlockToken)token;
        _tokens.Advance();

        var properties = PropertyChainParser.Parse(open.Position, stopAtNewline: true, out var variableName);
        var truthy = ParseBody(stopAtElse: true, out var footer);

        List<IRenderable>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current is ElseToken)
        {
            _tokens.Advance();
            falsy = ParseBody(stopAtElse: false, out footer);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        ValidateClosingDepth(open, (CloseBlockToken)_tokens.Current);
        _tokens.Advance();

        return new BlockNode(properties, truthy,
            ElseBody: falsy,
            VariableName: variableName,
            Footer: footer
        );
    }

    List<IRenderable> ParseBody(bool stopAtElse, out IReadOnlyList<FilterNode> footer)
    {
        var body = BodyParser.ParseNodes(insideBlock: true, stopAtElse: stopAtElse, out footer);
        if (!_tokens.AtEnd && _tokens.Current is CloseBlockToken)
        {
            ValidateNotSharingCloseLine(body, _tokens.Current);
        }

        return body;
    }
}