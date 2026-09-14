using static Guillemets.Tokenization.TokenKind;

using static Guillemets.Position;

namespace Guillemets.Tokenization;

internal readonly record struct Token(TokenKind Kind, string Source, int Start, int Length, Position Position)
{
    public bool IsText =>
        Kind is not (Open or OpenBlock);

    public string Text =>
        Kind == Escaped ? Source.Substring(Start + 1, Length - 1) : Source.Substring(Start, Length);

    public int End =>
        Start + Length;

    public bool StartsWithNewline =>
        Length > 0 && Source[Start] == NEWLINE;

    public bool TerminatesLine =>
        Length > 0 && Source[End - 1] == NEWLINE;

    public bool TrimsBlankLineBefore =>
        Kind is OpenBlock && Source[End - 1] == Symbols.TILDE;

    public bool TrimsBlankLineAfter =>
        Kind is CloseBlock && Source[Start] == Symbols.TILDE;

    public int Depth => Kind switch
    {
        OpenBlock or CloseBlock => Length - TrimMarkerLength,
        Literal or Escaped or Open or Close or Colon
            or BareColon or LocalScope or ParentScope or FilterDelimiter
            or Newline or Quote or Else or Negation or Assign =>
            throw new InvalidOperationException($"{Kind} tokens have no depth."),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unrecognized token kind."),
    };

    int TrimMarkerLength =>
        TrimsBlankLineBefore || TrimsBlankLineAfter ? 1 : 0;
}