using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal readonly record struct Token(TokenKind Kind, string Source, int Start, int Length, Position Position)
{
    public bool IsText =>
        Kind is not (Open or OpenBlock or CloseBlock);

    public string Text =>
        Kind == Escaped ? Source.Substring(Start + 1, Length - 1) : Source.Substring(Start, Length);

    public int Depth => Kind switch
    {
        OpenBlock => Length,
        CloseBlock => CountNonNewline(),
        Literal or Escaped or Open or Close or Colon
            or BareColon or LocalScope or ParentScope or Pipe
            or Newline or Else or Negation or Assign =>
            throw new InvalidOperationException($"{Kind} tokens have no depth."),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unrecognized token kind."),
    };

    int CountNonNewline()
    {
        var count = 0;
        foreach (var ch in Source.AsSpan(Start, Length))
        {
            if (ch != Position.NEWLINE) { count++; }
        }

        return count;
    }
}