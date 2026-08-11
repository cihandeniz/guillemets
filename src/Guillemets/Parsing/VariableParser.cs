using Guillemets.Ast;
using Guillemets.Rendering;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class VariableParser(TokenCursor _tokens)
    : IParser
{
    public INode Parse(IToken token)
    {
        var open = (OpenToken)token;
        _tokens.Advance();

        var chain = new PropertyChainBuilder();
        while (true)
        {
            if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed variable", open.Position); }
            if (_tokens.Current is CloseToken) { break; }

            if (_tokens.Current is NegationToken)
            {
                chain.Negate();
            }
            else if (_tokens.Current is LiteralToken literal)
            {
                chain.Add(literal.Text);
            }

            _tokens.Advance();
        }

        _tokens.Advance();

        return new VariableNode(chain.Build());
    }
}