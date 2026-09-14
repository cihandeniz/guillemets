namespace Guillemets.Tokenization;

internal readonly record struct TokenLine(int Start, int End, int Depth, bool IsBlank)
{
    public int FirstContent => Start + Depth;

    public bool Covers(int index) =>
        index >= Start && index < End;
}