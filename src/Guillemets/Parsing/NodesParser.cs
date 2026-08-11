using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class NodesParser(TokenCursor _tokens, ParserRegistry _registry)
    : IParser
{
    INode IParser.Parse(IToken token) =>
        Parse(token);

    INode Parse(IToken token) =>
        _registry.Resolve(token).Parse(token);

    public List<INode> ParseNodes(bool insideBlock, bool stopAtElse, bool stopAtOpenParen)
    {
        var nodes = new List<INode>();
        while (!_tokens.AtEnd && !ReachedClose(insideBlock) && !ReachedElse(stopAtElse) && !ReachedOpenParen(stopAtOpenParen))
        {
            nodes.Add(Parse(_tokens.Current));
        }

        return nodes;
    }

    bool ReachedClose(bool insideBlock) =>
        insideBlock && _tokens.Current is CloseBlockToken;

    bool ReachedElse(bool stopAtElse) =>
        stopAtElse && _tokens.Current is ElseToken;

    bool ReachedOpenParen(bool stopAtOpenParen) =>
        stopAtOpenParen && _tokens.Current is OpenParenToken;
}