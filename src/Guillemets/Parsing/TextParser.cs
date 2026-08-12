using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class TextParser(TokenCursor _tokens)
{
    public IRenderable Parse(IToken token)
    {
        var textToken = (ITextToken)token;
        _tokens.Advance();

        return new LiteralNode(textToken.Text);
    }
}