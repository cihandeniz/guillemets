namespace Guillemets.Ast;

internal sealed record TokenNode(IReadOnlyList<string> Segments)
    : INode;
