using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Filters;

using static Guillemets.Position;

namespace Guillemets.Rendering;

internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items)
    : IBlockBehavior
{
    const char TABLE_ROW_DELIMITER = '|';

    static readonly FilterNode SEPARATOR = new(new JoinFilter(), NEWLINE.ToString());

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
            return RenderItems(context, body);
        }

        var rows = SplitRows(body);
        if (rows.Count < 3) { return RenderItems(context, body); }

        var heading = context.Renderer.Render([.. rows[0], .. rows[1]], _scope);
        var footer = context.Renderer.Render([.. rows.Skip(3).SelectMany(row => row)], _scope);

        return heading + RenderItems(context, rows[2]) + footer;
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

        return SEPARATOR.Filter.Apply([.. renders.Select(render => render.TrimEnd(NEWLINE))], SEPARATOR.Arg).Single() + NEWLINE;
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