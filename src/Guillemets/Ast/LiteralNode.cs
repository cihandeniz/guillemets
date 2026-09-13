using Guillemets.Rendering;

using static Guillemets.Position;

namespace Guillemets.Ast;

internal record LiteralNode(string Text)
    : IRenderable
{
    public LiteralNode WithoutTrailingNewline() =>
        Text.EndsWith(NEWLINE) ? new(Text[..^1]) : this;

    public string Render(RenderContext context, Scope scope) =>
        Text;
}