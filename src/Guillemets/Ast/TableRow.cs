using Guillemets.Rendering;

using static Guillemets.Tokenization.Symbols;

namespace Guillemets.Ast;

internal record TableRow(
    IReadOnlyList<IRenderable> Lead,
    IReadOnlyList<TableCell> Cells,
    IReadOnlyList<IRenderable> Tail
)
{
    public static TableRow From(IReadOnlyList<IRenderable> nodes)
    {
        var lead = new List<IRenderable>();
        var cells = new List<TableCell>();
        var content = new List<IRenderable>();
        PipeNode? open = null;

        foreach (var node in nodes)
        {
            if (node is not PipeNode pipe)
            {
                (open is null ? lead : content).Add(node);

                continue;
            }

            if (open is not null)
            {
                cells.Add(new(open, [.. content]));
                content.Clear();
            }

            open = pipe;
        }

        return new(lead, cells, open is null ? [] : [open, .. content]);
    }

    public string RenderAsHeading(RenderContext context, Scope scope, out IReadOnlyList<int> columnsPerCell)
    {
        var rendered = Cells.Select(cell => cell.Render(context, scope)).ToList();
        columnsPerCell = [.. rendered.Select(ColumnsIn)];

        return WithLeadAndTail(context, scope, rendered);
    }

    public string RenderAsSeparator(RenderContext context, Scope scope, IReadOnlyList<int> columnsPerCell) =>
        WithLeadAndTail(context, scope, Cells.Select((cell, index) =>
            string.Concat(Enumerable.Repeat(cell.Render(context, scope), ColumnsAt(index, columnsPerCell)))
        ));

    static int ColumnsIn(string cell) =>
        cell.Count(character => character == PIPE);

    static int ColumnsAt(int index, IReadOnlyList<int> columnsPerCell) =>
        index < columnsPerCell.Count ? columnsPerCell[index] : 1;

    string WithLeadAndTail(RenderContext context, Scope scope, IEnumerable<string> cells) =>
        context.Renderer.Render(Lead, scope, inTableCell: true) +
        string.Concat(cells) +
        context.Renderer.Render(Tail, scope, inTableCell: true);
}