using Guillemets.Ast;
using Guillemets.Data;

namespace Guillemets.Rendering;

internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items, TableBody? _table)
    : IBlockBehavior
{
    public IEnumerable<string> Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody)
    {
        if (!_items.Any())
        {
            return elseBody is not null
                ? [context.Renderer.Render(elseBody, _scope)]
                : [];
        }

        return _table is null
            ? RenderItems(context, body)
            : [RenderTable(context, _table)];
    }

    IEnumerable<string> RenderItems(RenderContext context, IReadOnlyList<IRenderable> itemBody, bool inTableCell = false)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var itemScope = new Scope(_items[i],
                Parent: _scope,
                IsFirst: i == 0,
                IsLast: i == _items.Count - 1
            );
            yield return context.Renderer.Render(itemBody, itemScope, inTableCell);
        }
    }

    string RenderTable(RenderContext context, TableBody table)
    {
        var heading = table.Heading.RenderAsHeading(context, _scope, out var columnsPerCell);
        var separator = table.Separator.RenderAsSeparator(context, _scope, columnsPerCell);
        var rows = string.Concat(RenderItems(context, table.Row, inTableCell: true));
        var footer = context.Renderer.Render(table.Footer, _scope, inTableCell: true);

        return $"{heading}{separator}{rows}{footer}";
    }
}