using static Guillemets.Position;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal readonly record struct Token(TokenKind Kind, string Source, int Start, int Length, Position Position)
{
    public bool IsText =>
        Kind is not (Open or OpenBlock);

    public string Text =>
        Kind == Escaped ? Source.Substring(Start + 1, Length - 1) : Source.Substring(Start, Length);

    public int End =>
        Start + Length;

    public bool EndsLine =>
        LineBreakAfter(0);

    public bool PrecededByBlankLine =>
        LineBreakBefore(1) && LineBreakBefore(2);

    public bool FollowedByBlankLine =>
        LineBreakAfter(0) && LineBreakAfter(1);

    public bool TrimsBlankLineBefore =>
        Kind is OpenBlock && Source[End - 1] == Symbols.TILDE;

    public bool TrimsBlankLineAfter =>
        Kind is CloseBlock && Source[Start] == Symbols.TILDE;

    public int Depth => Kind switch
    {
        OpenBlock or CloseBlock => Length - TrimMarkerLength,
        Literal or Escaped or Open or Close or Colon
            or BareColon or LocalScope or ParentScope or FilterDelimiter
            or Newline or Else or Negation or Assign =>
            throw new InvalidOperationException($"{Kind} tokens have no depth."),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unrecognized token kind."),
    };

    public Token Skip(int count) =>
        this with
        {
            Start = Start + count,
            Length = Length - count,
            Position = Position.NextLine(count),
        };

    int TrimMarkerLength =>
        TrimsBlankLineBefore || TrimsBlankLineAfter ? 1 : 0;

    bool LineBreakBefore(int distance) =>
        Start - distance < 0 || Source[Start - distance] == NEWLINE;

    bool LineBreakAfter(int distance) =>
        End + distance >= Source.Length || Source[End + distance] == NEWLINE;
}