namespace Guillemets.Tokenization;

internal enum TokenKind
{
    Literal,
    Escaped,
    Open,
    OpenBlock,
    Close,
    CloseBlock,
    Colon,
    BareColon,
    LocalScope,
    ParentScope,
    FilterDelimiter,
    Newline,
    Else,
    Negation,
    Assign,
}