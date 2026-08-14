using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Data.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Guillemets.Rendering;

internal class PropertyChainResolution(Scope _scope,
    PropertyChainNode _properties,
    Glossary _glossary
)
{
    Scope? ClimbedScope => _scope.Climb(_properties.ClimbLevels);
    Scope? Owner => _properties.ThisScopeOnly ? ClimbedScope : ClimbedScope?.FindOwner(_properties);

    public IEnumerable<IDataSource> Resolve()
    {
        if (!_properties.ThisScopeOnly)
        {
            if (TryMagicVariable(out var magic)) { return [magic]; }
            if (TryVariableDefinition(out var defined)) { return [defined]; }
        }

        if (TryFilteringItems(out var filtered)) { return filtered; }
        if (TryPlainProjection(out var projected)) { return projected; }

        return [];
    }

    bool TryMagicVariable(out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;
        var scope = ClimbedScope;

        return _properties.Count == 1 &&
            scope is not null &&
            scope.TryGetMagic(_properties[0], _properties.LastSegmentNegated, out value);
    }

    bool TryVariableDefinition(out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;
        var scope = ClimbedScope;

        return _properties.Count == 1 &&
            scope is not null &&
            scope.TryResolveVariable(_properties[0], out value);
    }

    public bool TryFilteringItems([NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        items = null;
        if (Owner is null) { return false; }

        var lists = Project(Owner.Data, _properties.WithoutLast()).ToList();
        if (lists.Count == 0 || lists.Any(list => list.Kind is not DataKind.Array)) { return false; }

        var candidates = lists.SelectMany(list => list.EnumerateArray());
        var filterPath = _properties.LastSegment();

        var result = new List<IDataSource>();
        foreach (var item in candidates)
        {
            var itemValues = Project(item, filterPath).ToList();
            if (itemValues.Count > 1) { return false; }

            var filterValue = itemValues.SingleOrDefault();
            if (filterValue is null) { continue; }
            if (filterValue.Kind is DataKind.Null or DataKind.Undefined) { continue; }
            if (filterValue.Kind is not DataKind.Boolean) { return false; }
            if (!filterValue.AsBoolean()) { continue; }

            result.Add(item);
        }

        items = result;

        return true;
    }

    bool TryPlainProjection([NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        items = null;
        if (Owner is null) { return false; }

        items = Project(Owner.Data, _properties).ToList();

        return true;
    }

    public bool TryArrayItems([NotNullWhen(true)] out IReadOnlyList<IDataSource>? items)
    {
        items = null;
        var resolved = Resolve().ToList();
        if (resolved.Count == 0 || resolved.Any(result => result.Kind is not DataKind.Array)) { return false; }

        items = [.. resolved.SelectMany(result => result.EnumerateArray())];

        return true;
    }

    IEnumerable<IDataSource> Project(IDataSource current, PropertyChainNode properties)
    {
        if (properties.Count == 0) { return [current]; }
        if (current.Kind is DataKind.Null) { return []; }

        if (current.Kind is DataKind.Array)
        {
            return current.EnumerateArray().SelectMany(item => Project(item, properties));
        }

        var propertyName = _glossary[properties[0]];
        current.TryGetProperty(propertyName, out var propertyValue);
        if (properties.Count == 1 && properties.LastSegmentNegated)
        {
            propertyValue = propertyValue.Negate();
        }

        return Project(propertyValue, properties.Tail());
    }
}