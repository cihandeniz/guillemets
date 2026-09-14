using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Tokenization.Symbols;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class TextParser(TokenCursor _tokens)
{
    public IRenderable Parse(Token token)
    {
        var marksBlankLine = token.Kind is Quote && _tokens.CurrentOnBlankLine;
        var isPipe = token.Kind is Pipe;
        _tokens.Advance();

        var text = TextOf(token, marksBlankLine);

        return isPipe ? new PipeNode(text) : new LiteralNode(text);
    }

    string TextOf(Token token, bool marksBlankLine)
    {
        if (PrecedesTrimmedOpen(token)) { return token.Text[..^1]; }
        if (!marksBlankLine) { return token.Text; }
        if (!_tokens.CurrentStartsLine) { return token.Text; }

        return token.Text.TrimEnd(SPACE);
    }

    bool PrecedesTrimmedOpen(Token token) =>
        token.Kind is Newline && !_tokens.AtEnd && _tokens.Current.TrimsBlankLineBefore;
}