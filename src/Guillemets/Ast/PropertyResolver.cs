using Guillemets.Data;
using Guillemets.Data.Primitives;
using Humanizer;

namespace Guillemets.Ast;

internal class PropertyResolver(VariableStore variables)
{
    public VariableStore Variables { get; } = variables;

    public IEnumerable<IDataSource> Resolve(Scope scope, PropertyChain properties)
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

        foreach (var result in Resolve(ResolveScope(scope, properties).Data, properties))
        {
            yield return result;
        }
    }

    public IReadOnlyList<IDataSource>? ResolveLoopItems(Scope scope, PropertyChain properties) =>
        ResolveItemsMatchingLastSegment(scope, properties) ?? ResolveArrayItems(scope, properties);

    IReadOnlyList<IDataSource>? ResolveItemsMatchingLastSegment(Scope scope, PropertyChain properties)
    {
        if (properties.Count <= 1) { return null; }

        var containers = Resolve(ResolveScope(scope, properties).Data, properties.WithoutLast()).ToList();
        if (containers.Count != 1 || containers[0].Kind != DataKind.Array) { return null; }

        var lastSegment = new PropertyChain([properties[^1]], properties.LastSegmentNegated);

        return [.. containers[0].EnumerateArray()
            .Where(item => Resolve(item, lastSegment).SingleOrDefault()?.AsBoolean() == true)];
    }

    IReadOnlyList<IDataSource>? ResolveArrayItems(Scope scope, PropertyChain properties)
    {
        var resolved = Resolve(scope, properties).ToList();

        return resolved.Count == 1 && resolved[0].Kind == DataKind.Array
            ? [.. resolved[0].EnumerateArray()]
            : null;
    }

    Scope ResolveScope(Scope scope, PropertyChain properties)
    {
        if (properties.Count == 0 || HasProperty(scope.Data, properties[0])) { return scope; }

        return scope.Parent is not null ? ResolveScope(scope.Parent, properties) : scope;
    }

    static bool HasProperty(IDataSource data, string property) =>
        data.Kind == DataKind.Object && data.TryGetProperty(property.Dehumanize(), out _);

    public IEnumerable<IDataSource> Resolve(IDataSource current, PropertyChain properties)
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
                foreach (var result in Resolve(item, properties))
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
        if (properties.Count == 1 && properties.LastSegmentNegated) { next = Negate(next); }

        foreach (var result in Resolve(next, properties.Tail()))
        {
            yield return result;
        }
    }

    static IDataSource Negate(IDataSource value) =>
        value.AsBoolean() ? BooleanDataSource.FALSE : BooleanDataSource.TRUE;
}
