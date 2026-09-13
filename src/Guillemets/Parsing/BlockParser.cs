using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class BlockParser(TokenCursor _tokens, ParserRegistry _registry)
{
    const int BLANK_LINE = 2;

    readonly Lazy<BodyParser> _lazyBodyParser = _registry.GetLazy<BodyParser>();
    readonly Lazy<PropertyChainParser> _lazyPropertyChainParser = _registry.GetLazy<PropertyChainParser>();

    BodyParser BodyParser => _lazyBodyParser.Value;
    PropertyChainParser PropertyChainParser => _lazyPropertyChainParser.Value;

    public IRenderable Parse(Token open)
    {
        open.ValidateAsBlockOpen();
        _tokens.Advance();

        var properties = PropertyChainParser.Parse(open.Position, stopAtNewline: true, out var variableName);
        ValidateOpenStaysOnOneLine(open);
        ConsumeBlankLineAfterOpen();
        var truthy = ParseBody(stopAtElse: true, out var footer);

        List<IRenderable>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current.Kind is Else)
        {
            _tokens.Advance();
            _tokens.ConsumeNewlines(BLANK_LINE);
            falsy = ParseBody(stopAtElse: false, out footer);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        var close = _tokens.Current;
        close.ValidateDepthMatches(open);
        close.ValidateBlankLineAfterBlockClose();
        _tokens.Advance();

        var swallowedBlankLine = _tokens.ConsumeNewlines(BLANK_LINE) == BLANK_LINE;

        return new BlockNode(properties, truthy,
            ElseBody: falsy,
            VariableName: variableName,
            Footer: footer,
            BlankLineAfterClose: swallowedBlankLine && !close.TrimsBlankLineAfter
        );
    }

    void ValidateOpenStaysOnOneLine(Token open)
    {
        if (_tokens.AtEnd || _tokens.Current.Position.Line == open.Position.Line) { return; }

        throw new TemplateParseException("A block's opening must stay on one line", open.Position);
    }

    void ConsumeBlankLineAfterOpen()
    {
        var position = NextLinePosition();
        if (_tokens.ConsumeNewlines(BLANK_LINE) == BLANK_LINE) { return; }

        throw new TemplateParseException("Expected a blank line right after the block's opening", position);
    }

    Position NextLinePosition() =>
        _tokens.Current.Kind is Newline
            ? _tokens.Current.Position.NextLine(_tokens.Current.Length)
            : _tokens.Current.Position;

    List<IRenderable> ParseBody(bool stopAtElse, out IReadOnlyList<FilterNode> footer)
    {
        var body = BodyParser.Parse(insideBlock: true, stopAtElse: stopAtElse, out footer);
        if (footer.Count == 0 && !_tokens.AtEnd && _tokens.Current.Kind is CloseBlock)
        {
            _tokens.Current.ValidateAsBlockClose();
        }

        return body;
    }
}