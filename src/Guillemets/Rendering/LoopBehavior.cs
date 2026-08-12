using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Filters;

using static Guillemets.Position;

namespace Guillemets.Rendering;

internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items, IReadOnlyList<FilterNode> _footer)
    : IBlockBehavior
{
    const char TABLE_ROW_DELIMITER = '|';

    // TODO will return IEnumerable<string>
    public string Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody)
    {
        if (!_items.Any())
        {
            return elseBody is not null
                ? context.Renderer.Render(elseBody, _scope)
                : string.Empty;
        }

        if (body is not [LiteralNode { Text: var first }, ..] || !first.StartsWith(TABLE_ROW_DELIMITER))
        {
            // TODO not table, return multi item
            return RenderItems(context, body);
        }

        // TODO table, return single item, join NEWLINE by default in here
        var rows = SplitRows(body);
        if (rows.Count < 3) { return RenderItems(context, body); }

        var heading = context.Renderer.Render([.. rows[0], .. rows[1]], _scope);
        var tableFooter = context.Renderer.Render([.. rows.Skip(3).SelectMany(row => row)], _scope);

        return heading + RenderItems(context, rows[2]) + tableFooter;
    }

    string RenderItems(RenderContext context, IReadOnlyList<IRenderable> itemBody)
    {
        var renders = new List<string>(_items.Count);
        for (var i = 0; i < _items.Count; i++)
        {
            var itemScope = new Scope(_items[i],
                Parent: _scope,
                IsFirst: i == 0,
                IsLast: i == _items.Count - 1
            );
            renders.Add(context.Renderer.Render(itemBody, itemScope));
        }

        // TODO not here
        var values = renders.Select(render => render.TrimEnd(NEWLINE));
        foreach (var filter in _footer)
        {
            values = filter.Apply(values, FilterContext.Footer);
        }

        // TODO defeats the purpose of join filter in a loop. don't join,
        // return string enumerable
        return string.Join(NEWLINE.ToString(), values) + NEWLINE;
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