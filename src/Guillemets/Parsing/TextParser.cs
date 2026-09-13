using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class TextParser(TokenCursor _tokens)
{
    public IRenderable Parse(Token token)
    {
        _tokens.Advance();

        return new LiteralNode(PrecedesTrimmedOpen(token) ? token.Text[..^1] : token.Text);
    }

    bool PrecedesTrimmedOpen(Token token) =>
        token.Kind is Newline && !_tokens.AtEnd && _tokens.Current.TrimsBlankLineBefore;
}