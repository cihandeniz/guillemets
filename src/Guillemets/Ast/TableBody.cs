using static Guillemets.Position;

namespace Guillemets.Ast;

internal record TableBody(TableRow Heading,
    TableRow Separator,
    IReadOnlyList<IRenderable> Row,
    IReadOnlyList<IRenderable> Footer
)
{
    const int MINIMUM_ROWS = 3;

    public static TableBody? From(IReadOnlyList<IRenderable> body, int quoteDepth)
    {
        if (!OpensRow(body, quoteDepth)) { return null; }

        var rows = SplitRows(body);
        if (rows.Count < MINIMUM_ROWS) { return null; }

        return new(TableRow.From(rows[0]),
            TableRow.From(rows[1]),
            rows[2],
            [.. rows.Skip(MINIMUM_ROWS).SelectMany(row => row)]
        );
    }

    static bool OpensRow(IReadOnlyList<IRenderable> body, int quoteDepth) =>
        body.Count > quoteDepth && body[quoteDepth] is PipeNode;

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