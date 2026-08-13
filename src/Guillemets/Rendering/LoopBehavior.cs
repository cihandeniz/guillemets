using Guillemets.Ast;
using Guillemets.Data;

using static Guillemets.Position;

namespace Guillemets.Rendering;

internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items)
    : IBlockBehavior
{
    const char TABLE_ROW_DELIMITER = '|';

    public IEnumerable<string> Render(RenderContext context, IReadOnlyList<IRenderable> body, IReadOnlyList<IRenderable>? elseBody)
    {
        if (!_items.Any())
        {
            return elseBody is not null
                ? [context.Renderer.Render(elseBody, _scope)]
                : [];
        }

        if (body is not [LiteralNode { Text: var first }, ..] || !first.StartsWith(TABLE_ROW_DELIMITER))
        {
            return RenderItems(context, body);
        }

        var rows = SplitRows(body);
        if (rows.Count < 3) { return RenderItems(context, body); }

        var heading = context.Renderer.Render([.. rows[0], .. rows[1]], _scope);
        var tableFooter = context.Renderer.Render([.. rows.Skip(3).SelectMany(row => row)], _scope);

        return [$"{heading}{string.Concat(RenderItems(context, rows[2]))}{tableFooter}"];
    }

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