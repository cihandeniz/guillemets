using static Guillemets.Position;

namespace Guillemets.Ast;

internal record TableBody(IReadOnlyList<IRenderable> Heading,
    IReadOnlyList<IRenderable> Row,
    IReadOnlyList<IRenderable> Footer
)
{
    const char ROW_DELIMITER = '|';
    const int MINIMUM_ROWS = 3;

    public static TableBody? From(IReadOnlyList<IRenderable> body)
    {
        if (body is not [LiteralNode { Text: var first }, ..] || !first.StartsWith(ROW_DELIMITER)) { return null; }

        var rows = SplitRows(WithoutBodyPadding(body));
        if (rows.Count < MINIMUM_ROWS) { return null; }

        return new([.. rows[0], .. rows[1]],
            rows[2],
            [.. rows.Skip(MINIMUM_ROWS).SelectMany(row => row)]
        );
    }

    static IReadOnlyList<IRenderable> WithoutBodyPadding(IReadOnlyList<IRenderable> body)
    {
        if (body is not [.., LiteralNode { Text: var last }] || !last.EndsWith(NEWLINE)) { return body; }

        return [.. body.Take(body.Count - 1), new LiteralNode(last[..^1])];
    }

    static List<List<IRenderable>> SplitRows(IReadOnlyList<IRenderable> body)
    {
        var rows = new List<List<IRenderable>>();
        var current = new List<IRenderable>();
        foreach (var node in body)
        {
            current.Add(node);
            if (node is LiteralNode { Text: [NEWLINE] })
            {
                rows.Add(current);
                current = [];
            }
        }

        if (current.Count > 0) { rows.Add(current); }

        return rows;
    }
}