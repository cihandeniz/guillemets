namespace Guillemets;

internal static class Ast
{
    internal interface INode;
    internal sealed record LiteralNode(string Text) : INode;
    internal sealed record TokenNode(string Path) : INode;
}
