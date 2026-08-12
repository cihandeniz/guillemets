using Guillemets.Ast;
using Guillemets.Data;
using Humanizer;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Rendering;

internal class PropertyResolver(VariableStore variables)
{
    static IEnumerable<IDataSource> Project(IDataSource current, PropertyChainNode properties)
    {
        if (properties.Count == 0)
        {
            yield return current;
            yield break;
        }

        if (current.Kind == DataKind.Null)
        {
            yield break;
        }

        if (current.Kind == DataKind.Array)
        {
            foreach (var item in current.EnumerateArray())
            {
                foreach (var result in Project(item, properties))
                {
                    yield return result;
                }
            }

            yield break;
        }

        var name = properties[0].Dehumanize();
        var next = current.TryGetProperty(name, out var property)
            ? property
            : throw new InvalidOperationException($"Property '{name}' was not found.");
        if (properties.Count == 1 && properties.LastSegmentNegated) { next = next.Negate(); }

        foreach (var result in Project(next, properties.Tail()))
        {
            yield return result;
        }
    }

    static bool TryResolveFilteredItemScope(Scope scope, PropertyChainNode properties, [NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        items = null;
        if (properties.Count <= 1) { return false; }

        var containers = Project(scope.FindOwner(properties).Data, properties.WithoutLast()).ToList();
        if (containers.Count != 1 || containers[0].Kind != DataKind.Array) { return false; }

        var lastSegment = new PropertyChainNode([properties[^1]], properties.LastSegmentNegated);

        items = [.. containers[0].EnumerateArray()
            .Where(item => Project(item, lastSegment).SingleOrDefault()?.AsBoolean() == true)];

        return true;
    }

    public VariableStore Variables { get; } = variables;

    public IEnumerable<IDataSource> Resolve(Scope scope, PropertyChainNode properties)
    {
        if (properties.Count == 1 && scope.TryGetMagic(properties[0], properties.LastSegmentNegated, out var magic))
        {
            yield return magic;
            yield break;
        }

        if (properties.Count == 1 && Variables.TryResolve(properties[0], out var defined))
        {
            yield return defined;
            yield break;
        }

        foreach (var result in Project(scope.FindOwner(properties).Data, properties))
        {
            yield return result;
        }
    }

    public bool TryResolveLoopItems(Scope scope, PropertyChainNode properties, [NotNullWhen(true)] out IReadOnlyList<IDataSource>? items) =>
        TryResolveFilteredItemScope(scope, properties, out items) ||
        TryResolveArrayItems(scope, properties, out items);

    bool TryResolveArrayItems(Scope scope, PropertyChainNode properties, [NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        var resolved = Resolve(scope, properties).ToList();
        if (resolved.Count != 1 || resolved[0].Kind != DataKind.Array)
        {
            items = null;

            return false;
        }

        items = [.. resolved[0].EnumerateArray()];

        return true;
    }
}