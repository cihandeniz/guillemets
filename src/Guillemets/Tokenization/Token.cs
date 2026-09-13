using static Guillemets.Position;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Tokenization;

internal readonly record struct Token(TokenKind Kind, string Source, int Start, int Length, Position Position)
{
    static readonly string CLOSE_BLOCK = new(Symbols.CLOSE, 2);

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

    public int Depth => Kind switch
    {
        OpenBlock or CloseBlock => Length,
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

    public void ValidateAsBlockOpen()
    {
        if (PrecededByBlankLine) { return; }

        throw new TemplateParseException("Expected a blank line right before the block's opening", Position);
    }

    public void ValidateAsBlockClose()
    {
        if (!Position.AtLineStart)
        {
            throw new TemplateParseException(
                $"A literal may not share a line with the block's closing {CLOSE_BLOCK}",
                Position
            );
        }

        if (PrecededByBlankLine) { return; }

        throw new TemplateParseException(
            $"Expected a blank line right before the block's closing {CLOSE_BLOCK}",
            Position
        );
    }

    public void ValidateBlankLineAfterBlockClose()
    {
        if (FollowedByBlankLine) { return; }

        throw new TemplateParseException(
            $"Expected a blank line right after the block's closing {CLOSE_BLOCK}",
            Position.NextLine()
        );
    }

    public void ValidateDepthMatches(Token open)
    {
        if (Depth == open.Depth) { return; }

        throw new TemplateParseException(
            $"Block opened with {open.Text} but closed with {Text}",
            Position
        );
    }

    bool LineBreakBefore(int distance) =>
        Start - distance < 0 || Source[Start - distance] == NEWLINE;

    bool LineBreakAfter(int distance) =>
        End + distance >= Source.Length || Source[End + distance] == NEWLINE;
}