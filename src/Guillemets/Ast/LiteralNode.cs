using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record LiteralNode(string Text)
    : IRenderable
{
    public bool EndsAtLineEnd { get; } = Text.EndsWith(NEWLINE);

    public string Render(RenderContext context, Scope scope) =>
        Text;
}