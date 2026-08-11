using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;

namespace Guillemets.Parsing;

internal class BlockParser(TokenCursor _tokens, NodesParser _nodesParser)
    : IParser
{
    public INode Parse(IToken token)
    {
        var open = (OpenBlockToken)token;
        _tokens.Advance();

        var properties = ParseHeader(open.Position, out var variableName);
        var truthy = _nodesParser.ParseNodes(insideBlock: true, stopAtElse: true);

        List<INode>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current is ElseToken)
        {
            _tokens.Advance();
            falsy = _nodesParser.ParseNodes(insideBlock: true, stopAtElse: false);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        ValidateClosingDepth(open, (CloseBlockToken)_tokens.Current);
        _tokens.Advance();

        return new BlockNode(properties, truthy, falsy, variableName);
    }

    void ValidateClosingDepth(OpenBlockToken open, CloseBlockToken close)
    {
        if (close.Depth == open.Depth) { return; }

        throw new TemplateParseException(
            $"Block opened with {open.Text} but closed with {close.Text.TrimEnd(NEWLINE)}",
            close.Position
        );
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
}
