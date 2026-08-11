using Guillemets.Tokens;

namespace Guillemets.Tokenization;

internal static class Tokens
{
    public static LiteralToken Literal(TokenContext context) => new(context.Text, context.Position);
    public static OpenToken Open(TokenContext context) => new(context.Position);
    public static OpenBlockToken OpenBlock(TokenContext context) => new(context.Text, context.Position);
    public static CloseToken Close(TokenContext context) => new(context.Position);
    public static CloseBlockToken CloseBlock(TokenContext context) => new(context.Text, context.Position);
    public static ColonToken Colon(TokenContext context) => new(context.Text, context.Position);
    public static ElseToken Else(TokenContext context) => new(context.Text, context.Position);
    public static NegationToken Negation(TokenContext context) => new(context.Text, context.Position);
    public static EqualsToken Equals(TokenContext context) => new(context.Text, context.Position);
}