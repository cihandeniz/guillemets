using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Position;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class BlockParser(TokenCursor _tokens, ParserRegistry _registry)
{
    static List<IRenderable> WithoutTrailingBlankLine(List<IRenderable> body, int depth)
    {
        var blankLineNodes = depth + 1;
        if (body.Count < blankLineNodes) { return body; }
        if (body[^1] is not LiteralNode { Text: [NEWLINE] }) { return body; }

        return body.GetRange(0, body.Count - blankLineNodes);
    }

    readonly Lazy<BodyParser> _lazyBodyParser = _registry.GetLazy<BodyParser>();
    readonly Lazy<PropertyChainParser> _lazyPropertyChainParser = _registry.GetLazy<PropertyChainParser>();

    BodyParser BodyParser => _lazyBodyParser.Value;
    PropertyChainParser PropertyChainParser => _lazyPropertyChainParser.Value;

    public IRenderable Parse(Token open)
    {
        open.ValidateAsBlockOpen(_tokens);
        var marker = _tokens.CurrentQuoteMarker;
        var depth = _tokens.CurrentQuoteDepth;
        _tokens.Advance();

        var properties = PropertyChainParser.Parse(open.Position, stopAtNewline: true, out var variableName);
        ValidateOpenStaysOnOneLine(open);
        ConsumeBlankLineAfterOpen(open, depth);
        var truthy = ParseBody(stopAtElse: true, depth, out var footer);

        List<IRenderable>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current.Kind is Else)
        {
            _tokens.Advance();
            _tokens.TryConsumeBlankLine(depth);
            falsy = ParseBody(stopAtElse: false, depth, out footer);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        var close = _tokens.Current;
        close.ValidateDepthMatches(open);
        close.ValidateQuoteDepthMatches(depth, _tokens.CurrentQuoteDepth);
        close.ValidateBlankLineAfterBlockClose(_tokens);
        _tokens.Advance();

        string? blankLineAfterClose = null;
        if (_tokens.ConsumeBlankLineAfterClose(out var blankLineMarker) && !close.TrimsBlankLineAfter)
        {
            blankLineAfterClose = blankLineMarker;
        }

        return new BlockNode(properties, truthy,
            ElseBody: falsy,
            VariableName: variableName,
            Footer: footer,
            QuoteMarker: marker,
            QuoteDepth: depth,
            BlankLineAfterClose: blankLineAfterClose
        );
    }

    void ValidateOpenStaysOnOneLine(Token open)
    {
        if (_tokens.AtEnd || _tokens.Current.Position.Line == open.Position.Line) { return; }

        throw new TemplateParseException("A block's opening must stay on one line", open.Position);
    }

    void ConsumeBlankLineAfterOpen(Token open, int depth)
    {
        if (_tokens.TryConsumeBlankLine(depth)) { return; }

        throw new TemplateParseException(
            "Expected a blank line right after the block's opening",
            open.Position.NextLine()
        );
    }

    List<IRenderable> ParseBody(bool stopAtElse, int depth, out IReadOnlyList<FilterNode> footer)
    {
        var body = BodyParser.Parse(insideBlock: true, stopAtElse: stopAtElse, out footer);
        if (footer.Count == 0 && !_tokens.AtEnd && _tokens.Current.Kind is CloseBlock)
        {
            _tokens.Current.ValidateAsBlockClose(_tokens);
        }

        return WithoutTrailingBlankLine(body, depth);
    }
}