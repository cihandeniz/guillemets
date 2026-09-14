using static Guillemets.Position;

namespace Guillemets.Ast;

internal record TableBody(IReadOnlyList<IRenderable> Heading,
    IReadOnlyList<IRenderable> Row,
    IReadOnlyList<IRenderable> Footer
)
{
    const char ROW_DELIMITER = '|';
    const int MINIMUM_ROWS = 3;

    public static TableBody? From(IReadOnlyList<IRenderable> body, int quoteDepth)
    {
        if (!OpensRow(body, quoteDepth)) { return null; }

        var rows = SplitRows(body);
        if (rows.Count < MINIMUM_ROWS) { return null; }

        return new([.. rows[0], .. rows[1]],
            rows[2],
            [.. rows.Skip(MINIMUM_ROWS).SelectMany(row => row)]
        );
    }

    static bool OpensRow(IReadOnlyList<IRenderable> body, int quoteDepth) =>
        body.Count > quoteDepth &&
        body[quoteDepth] is LiteralNode { Text: var first } &&
        first.StartsWith(ROW_DELIMITER);

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