namespace Guillemets.Rendering;

internal record RenderContext(
    PropertyResolver PropertyResolver,
    Renderer Renderer,
    VariableStore Variables
);