using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Tokenization.Symbols;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class TextParser(TokenCursor _tokens)
{
    static readonly string BLANK_LINE_MARKER = QUOTE.ToString();

    public IRenderable Parse(Token token)
    {
        var marksBlankLine = token.Kind is Quote && _tokens.CurrentOnBlankLine;
        _tokens.Advance();

        return new LiteralNode(TextOf(token, marksBlankLine));
    }

    string TextOf(Token token, bool marksBlankLine)
    {
        if (PrecedesTrimmedOpen(token)) { return token.Text[..^1]; }

        return marksBlankLine ? BLANK_LINE_MARKER : token.Text;
    }

    bool PrecedesTrimmedOpen(Token token) =>
        token.Kind is Newline && !_tokens.AtEnd && _tokens.Current.TrimsBlankLineBefore;
}