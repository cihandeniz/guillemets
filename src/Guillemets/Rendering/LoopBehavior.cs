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

        return _table is null ? RenderItems(context, body) : [RenderTable(context, _table)];
    }

    string RenderTable(RenderContext context, TableBody table) =>
        context.Renderer.Render(table.Heading, _scope) +
        string.Concat(RenderItems(context, table.Row)) +
        context.Renderer.Render(table.Footer, _scope);

    IEnumerable<string> RenderItems(RenderContext context, IReadOnlyList<IRenderable> itemBody)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var itemScope = new Scope(_items[i],
                Parent: _scope,
                IsFirst: i == 0,
                IsLast: i == _items.Count - 1
            );
            yield return context.Renderer.Render(itemBody, itemScope);
        }
    }
}