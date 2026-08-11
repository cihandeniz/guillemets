using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Filters;

using static Guillemets.Position;

namespace Guillemets.Rendering;

internal class LoopBehavior(Scope _scope, IReadOnlyList<IDataSource> _items,
    FilterNode? separator = null
) : IBlockBehavior
{
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

        var renders = new List<string>(_items.Count);
        for (var i = 0; i < _items.Count; i++)
        {
            var itemScope = new Scope(_items[i],
                Parent: _scope,
                IsFirst: i == 0,
                IsLast: i == _items.Count - 1
            );
            renders.Add(context.Renderer.RenderAll(body, itemScope));
        }

        return _separator.Filter.Apply([.. renders.Select(render => render.TrimEnd(NEWLINE))], _separator.Args) + NEWLINE;
    }
}