using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;

namespace Guillemets.Parsing;

internal class BlockParser(TokenCursor _tokens, ParserRegistry _registry)
    : IParser
{
    static void ValidateClosingDepth(OpenBlockToken open, CloseBlockToken close)
    {
        if (close.Depth == open.Depth) { return; }

        throw new TemplateParseException(
            $"Block opened with {open.Text} but closed with {close.Text.TrimEnd(NEWLINE)}",
            close.Position
        );
    }

    static void ValidateNotSharingCloseLine(List<INode> body, IToken close)
    {
        if (AtLineStart(body)) { return; }

        throw new TemplateParseException("A literal may not share a line with the block's closing »»", close.Position);
    }

    static void ValidateIsSeparatorFilter(FilterNode filter, Position position)
    {
        if (filter.Filter is SeparatorFilter) { return; }

        throw new TemplateParseException("Blocks only accept the separator filter", position);
    }

    static bool AtLineStart(List<INode> body) =>
        body.Count == 0
        || body[^1] is BlockNode
        || (body[^1] is LiteralNode literal && literal.Text.EndsWith(NEWLINE));

    readonly NodesParser _nodesParser = _registry.Get<NodesParser>();
    readonly FilterParser _filterParser = _registry.Get<FilterParser>();
    readonly PropertyChainParser _propertyChainParser = _registry.Get<PropertyChainParser>();

    public INode Parse(IToken token)
    {
        var open = (OpenBlockToken)token;
        _tokens.Advance();

        var properties = _propertyChainParser.Parse(open.Position, stopAtNewline: true, out var variableName);
        var truthy = ParseBody(stopAtElse: true, out var separator);

        List<INode>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current is ElseToken)
        {
            _tokens.Advance();
            falsy = ParseBody(stopAtElse: false, out separator);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        ValidateClosingDepth(open, (CloseBlockToken)_tokens.Current);
        _tokens.Advance();

        return new BlockNode(properties, truthy, falsy, variableName, separator);
    }

    List<INode> ParseBody(bool stopAtElse, out FilterNode? separator)
    {
        var body = new List<INode>();
        while (true)
        {
            body.AddRange(_nodesParser.ParseNodes(insideBlock: true, stopAtElse, stopAtOpenParen: true));

            if (!_tokens.AtEnd && _tokens.Current is CloseBlockToken)
            {
                ValidateNotSharingCloseLine(body, _tokens.Current);
            }

            if (_tokens.AtEnd || _tokens.Current is not OpenParenToken openParen)
            {
                separator = null;

                return body;
            }

            if (AtLineStart(body) && TryParseSeparatorFooter(out var value))
            {
                separator = value;

                return body;
            }

            body.Add(new LiteralNode(openParen.Text));
            _tokens.Advance();
        }
    }

    bool TryParseSeparatorFooter(out FilterNode? separator)
    {
        separator = null;
        var start = _tokens.Position;
        var openPosition = _tokens.Current.Position;
        if (!_filterParser.TryParse(out var filter)) { return Reject(start); }
        if (!_tokens.AtEnd && _tokens.Current is not CloseBlockToken) { return Reject(start); }

        ValidateIsSeparatorFilter(filter, openPosition);
        separator = filter;

        return true;
    }

    bool Reject(int start)
    {
        _tokens.Rewind(start);

        return false;
    }
}