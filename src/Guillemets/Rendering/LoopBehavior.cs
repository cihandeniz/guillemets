using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Filters;

using static Guillemets.Position;

namespace Guillemets.Rendering;

internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items,
    FilterNode? separator = null
) : IBlockBehavior
{
    const char TABLE_ROW_DELIMITER = '|';

    static readonly FilterNode DEFAULT_SEPARATOR = new(new SeparatorFilter(), [NEWLINE.ToString()]);

    readonly FilterNode _separator = separator ?? DEFAULT_SEPARATOR;

    public string Render(RenderContext context, IReadOnlyList<INode> body, IReadOnlyList<INode>? elseBody)
    {
        if (!_items.Any())
        {
            return elseBody is not null
                ? context.Renderer.RenderAll(elseBody, _scope)
                : string.Empty;
        }

        if (body is not [LiteralNode { Text: var first }, ..] || !first.StartsWith(TABLE_ROW_DELIMITER))
        {
            return RenderItems(context, body);
        }

        var rows = SplitRows(body);
        if (rows.Count < 3) { return RenderItems(context, body); }

        var heading = context.Renderer.RenderAll([.. rows[0], .. rows[1]], _scope);
        var footer = context.Renderer.RenderAll([.. rows.Skip(3).SelectMany(row => row)], _scope);

        return heading + RenderItems(context, rows[2]) + footer;
    }

    string RenderItems(RenderContext context, IReadOnlyList<INode> itemBody)
    {
        var renders = new List<string>(_items.Count);
        for (var i = 0; i < _items.Count; i++)
        {
            var itemScope = new Scope(_items[i],
                Parent: _scope,
                IsFirst: i == 0,
                IsLast: i == _items.Count - 1
            );
            renders.Add(context.Renderer.RenderAll(itemBody, itemScope));
        }

        return _separator.Filter.Apply([.. renders.Select(render => render.TrimEnd(NEWLINE))], _separator.Args) + NEWLINE;
    }

    static List<List<INode>> SplitRows(IReadOnlyList<INode> body)
    {
        var rows = new List<List<INode>>();
        var current = new List<INode>();
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