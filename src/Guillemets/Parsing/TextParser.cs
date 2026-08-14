using Guillemets.Ast;
using Guillemets.Tokenization;

namespace Guillemets.Parsing;

internal class TextParser(TokenCursor _tokens)
{
    public IRenderable Parse(Token token)
    {
        _tokens.Advance();

        return new LiteralNode(token.Text);
    }
}