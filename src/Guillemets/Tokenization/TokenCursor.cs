using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal class TokenCursor(List<Token> _tokens, TokenLines _lines)
{
    int _position;

    public bool AtEnd => _position >= _tokens.Count;
    public Token Current => _tokens[_position];
    public int Position => _position;

    TokenLine CurrentLine => _lines.At(_position);
    public int CurrentQuoteDepth => CurrentLine.Depth;
    public string CurrentQuoteMarker => CurrentLine.QuoteMarker;
    public bool CurrentOnBlankLine => CurrentLine.IsBlank;

    public bool CurrentAtLineStart => CurrentLine.Start == _position;
    public bool CurrentStartsLine => CurrentLine.FirstContent == _position;
    public bool CurrentEndsLine => _position + 1 >= _tokens.Count || _tokens[_position + 1].StartsWithNewline;
    public bool CurrentPrecededByBlankLine => CurrentStartsLine && IsBlankOrMissing(_lines.Above(CurrentLine));
    public bool CurrentFollowedByBlankLine => CurrentEndsLine && IsBlankOrMissing(_lines.Below(CurrentLine));

    public bool AtQuotedBlockLine =>
        !AtEnd && Current.Kind is Quote && _position == CurrentLine.Start && OpensBlock(CurrentLine);

    public void Advance() =>
        _position++;

    public void Rewind(int position) =>
        _position = position;

    public void SkipQuotes() =>
        _position = CurrentLine.FirstContent;

    public bool TryConsumeBlankLine(int depth)
    {
        if (AtEnd || Current.Kind is not Newline) { return false; }
        if (_lines.Below(CurrentLine) is not { IsBlank: true } blank) { return false; }
        if (blank.Depth != depth) { return false; }

        _position = blank.End;

        return true;
    }

    public bool ConsumeBlankLineAfterClose()
    {
        if (AtEnd || Current.Kind is not Newline) { return false; }

        var line = CurrentLine;
        if (_lines.Below(line) is not { IsBlank: true } blank)
        {
            _position = line.End;

            return false;
        }

        _position = blank.End;

        return true;
    }

    public bool LineReaches(TokenKind kind)
    {
        var line = CurrentLine;
        for (var i = _position; i < line.End; i++)
        {
            if (_tokens[i].Kind == kind) { return true; }
        }

        return false;
    }

    static bool IsBlankOrMissing(TokenLine? line) =>
        line is null or { IsBlank: true };

    bool OpensBlock(TokenLine line) =>
        line.FirstContent < _tokens.Count && _tokens[line.FirstContent].Kind is OpenBlock or CloseBlock or Else;
}