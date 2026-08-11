namespace Guillemets.Rendering;

internal record RenderContext(
    PropertyResolver PropertyResolver,
    IRenderer Renderer,
    VariableStore Variables
);