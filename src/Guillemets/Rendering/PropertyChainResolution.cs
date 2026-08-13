using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Data.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Rendering;

internal class PropertyChainResolution(Scope _scope,
    PropertyChainNode _properties,
    VariableStore _variables,
    Glossary _glossary
)
{
    Scope? ClimbedScope => _scope.Climb(_properties.ClimbLevels);
    Scope? Owner =>
        ClimbedScope switch
        {
            null => null,
            { } scope => _properties.ThisScopeOnly ? scope : scope.FindOwner(_properties),
        };

    public IEnumerable<IDataSource> Resolve()
    {
        if (TryMagic(out var magic))
        {
            yield return magic;
            yield break;
        }

        if (TryDefinedVariable(out var defined))
        {
            yield return defined;
            yield break;
        }

        if (TryFilteredItemScope(out var filtered))
        {
            foreach (var item in filtered)
            {
                yield return item;
            }

            yield break;
        }

        if (Owner is not { } owner) { yield break; }

        foreach (var result in Project(owner.Data, _properties))
        {
            yield return result;
        }
    }

    bool TryMagic(out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;

        return !_properties.ThisScopeOnly &&
            _properties.Count == 1 &&
            ClimbedScope is { } scope &&
            scope.TryGetMagic(_properties[0], _properties.LastSegmentNegated, out value);
    }

    bool TryDefinedVariable(out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;

        return !_properties.ThisScopeOnly &&
            _properties.Count == 1 &&
            ClimbedScope is not null &&
            _variables.TryResolve(_properties[0], out value);
    }

    public bool TryFilteredItemScope([NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        items = null;
        if (_properties.ThisScopeOnly || _properties.Count <= 1 || ClimbedScope is not { } scope) { return false; }

        var containers = Project(scope.FindOwner(_properties).Data, _properties.WithoutLast()).ToList();
        if (containers.Count != 1 || containers[0].Kind != DataKind.Array) { return false; }

        var lastSegment = _properties.LastSegment();
        var matches = new List<IDataSource>();
        foreach (var item in containers[0].EnumerateArray())
        {
            var flag = Project(item, lastSegment).SingleOrDefault();
            if (flag is not { Kind: DataKind.Boolean }) { return false; }
            if (!flag.AsBoolean()) { continue; }

            matches.Add(item);
        }

        items = matches;

        return true;
    }

    public bool TryArrayItems([NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        var resolved = Resolve().ToList();
        if (resolved.Count == 0 || resolved.Any(result => result.Kind != DataKind.Array))
        {
            items = null;

            return false;
        }

        items = [.. resolved.SelectMany(result => result.EnumerateArray())];

        return true;
    }

    IEnumerable<IDataSource> Project(IDataSource current, PropertyChainNode properties)
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

        var name = _glossary[properties[0]];
        current.TryGetProperty(name, out var next);
        if (properties.Count == 1 && properties.LastSegmentNegated) { next = next.Negate(); }

        foreach (var result in Project(next, properties.Tail()))
        {
            yield return result;
        }
    }
}