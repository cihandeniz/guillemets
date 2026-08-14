using Guillemets.Ast;
using Guillemets.Data;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Rendering;

internal class PropertyResolver(Glossary _glossary)
{
    public IEnumerable<IDataSource> Resolve(Scope scope, PropertyChainNode properties) =>
        new PropertyChainResolution(scope, properties, _glossary).Resolve();

    public bool TryResolveLoopItems(Scope scope, PropertyChainNode properties, [NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        var resolution = new PropertyChainResolution(scope, properties, _glossary);

        return resolution.TryFilteredItemScope(out items) || resolution.TryArrayItems(out items);
    }
}