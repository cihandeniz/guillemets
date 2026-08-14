using Guillemets.Ast;
using Guillemets.Data;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Rendering;

internal class PropertyResolver(Glossary _glossary)
{
    public IEnumerable<IDataSource> Resolve(Scope scope, PropertyChainNode properties) =>
        new PropertyChainResolution(scope, properties, _glossary).Resolve();

    public bool TryResolveLoopItems(Scope scope, PropertyChainNode properties,
        [NotNullWhen(true)] out IReadOnlyList<IDataSource>? items,
        out IReadOnlyList<IDataSource> resolved
    )
    {
        var resolution = new PropertyChainResolution(scope, properties, _glossary);
        if (resolution.TryFilteringItems(out items))
        {
            resolved = items;

            return true;
        }

        resolved = resolution.Resolve(withoutFiltering: true).ToList();
        if (resolved.Count > 0 && resolved.All(value => value.Kind is DataKind.Array))
        {
            items = [.. resolved.SelectMany(value => value.EnumerateArray())];

            return true;
        }

        items = null;

        return false;
    }
}