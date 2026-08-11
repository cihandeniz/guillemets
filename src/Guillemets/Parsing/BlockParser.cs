using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Rendering;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;

namespace Guillemets.Parsing;

// TODO REFACTOR
internal class BlockParser(TokenCursor _tokens, ParserRegistry _registry)
    : IParser
{
    readonly NodesParser _nodesParser = _registry.Get<NodesParser>();
    readonly FilterParser _filterParser = _registry.Get<FilterParser>();

    public INode Parse(IToken token)
    {
        var open = (OpenBlockToken)token;
        _tokens.Advance();

        var properties = ParseHeader(open.Position, out var variableName);
        var truthy = ParseBody(stopAtElse: true, out var truthySeparator);

        List<INode>? falsy = null;
        var falsySeparator = (FilterNode?)null;
        if (!_tokens.AtEnd && _tokens.Current is ElseToken)
        {
            _tokens.Advance();
            falsy = ParseBody(stopAtElse: false, out falsySeparator);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        ValidateClosingDepth(open, (CloseBlockToken)_tokens.Current);
        _tokens.Advance();

        return new BlockNode(properties, truthy, falsy, variableName, falsy is not null ? falsySeparator : truthySeparator);
    }

    PropertyChain ParseHeader(Position openPosition, out string? variableName)
    {
        var chain = new PropertyChainBuilder();
        variableName = null;
        while (true)
        {
            if (_tokens.AtEnd) { throw new TemplateParseException("Unclosed block header", openPosition); }

            if (_tokens.Current is NegationToken)
            {
                chain.Negate();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is EqualsToken)
            {
                variableName = chain.PopVariableName();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is not LiteralToken literal)
            {
                _tokens.Advance();

                continue;
            }

            var newlineIndex = literal.Text.IndexOf(NEWLINE);
            if (newlineIndex < 0)
            {
                chain.Add(literal.Text);
                _tokens.Advance();

                continue;
            }

            chain.Add(literal.Text[..newlineIndex]);
            _tokens.TrimCurrentLiteral(newlineIndex + 1);

            return chain.Build();
        }
    }

    List<INode> ParseBody(bool stopAtElse, out FilterNode? separator)
    {
        var body = new List<INode>();
        while (true)
        {
            body.AddRange(_nodesParser.ParseNodes(insideBlock: true, stopAtElse, stopAtOpenParen: true));
            if (_tokens.AtEnd || _tokens.Current is not OpenParenToken openParen) { separator = null; return body; }

            if (AtLineStart(body) && TryParseSeparatorFooter(out var value))
            {
                separator = value;

                return body;
            }

            body.Add(new LiteralNode(openParen.Text));
            _tokens.Advance();
        }
    }

    static bool AtLineStart(List<INode> body) =>
        body.Count == 0 || (body[^1] is LiteralNode literal && literal.Text.EndsWith(NEWLINE));

    bool TryParseSeparatorFooter(out FilterNode? separator)
    {
        separator = null;
        var start = _tokens.Position;

        if (!_filterParser.TryParse(out var filter)
            || filter.Filter is not SeparatorFilter
            || (!_tokens.AtEnd && _tokens.Current is not CloseBlockToken))
        {
            _tokens.Rewind(start);

            return false;
        }

        separator = filter;

        return true;
    }

    void ValidateClosingDepth(OpenBlockToken open, CloseBlockToken close)
    {
        if (close.Depth == open.Depth) { return; }

        throw new TemplateParseException(
            $"Block opened with {open.Text} but closed with {close.Text.TrimEnd(NEWLINE)}",
            close.Position
        );
    }
}