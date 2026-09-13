using Guillemets.Ast;
using Guillemets.Data;
using Guillemets.Data.Primitives;

namespace Guillemets.Rendering;

internal record Scope(IDataSource Data,
    Scope? Parent = null,
    bool? IsFirst = null,
    bool? IsLast = null,
    Glossary? Glossary = null
)
{
    const string THIS = "this";
    const string FIRST = "first";
    const string LAST = "last";

    static readonly HashSet<string> LOOP_FLAG_NAMES = [FIRST, LAST];

    readonly VariableStore _variables = new();

    public Glossary Glossary { get; } =
        Glossary ?? Parent?.Glossary
            ?? throw new InvalidOperationException("A root Scope (one with no Parent) must be given a Glossary.");

    public bool TryGetMagic(string property, bool negated, out IDataSource value)
    {
        var name = property.ToLowerInvariant();

        return TryGetCurrentValue(name, negated, out value) || TryGetLoopFlag(name, negated, out value);
    }

    bool TryGetCurrentValue(string name, bool negated, out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;
        if (name != THIS) { return false; }

        value = negated ? Data.Negate() : Data;

        return true;
    }

    bool TryGetLoopFlag(string name, bool negated, out IDataSource value)
    {
        value = UndefinedDataSource.INSTANCE;
        var flag = name switch
        {
            FIRST => IsFirst,
            LAST => IsLast,
            _ => null,
        };

        if (flag is not null)
        {
            value = (negated ? !flag.Value : flag.Value) ? BooleanDataSource.TRUE : BooleanDataSource.FALSE;

            return true;
        }

        return LOOP_FLAG_NAMES.Contains(name) && Parent is not null && Parent.TryGetMagic(name, negated, out value);
    }

    public Scope FindOwner(PropertyChainNode properties)
    {
        if (properties.Count == 0 || HasProperty(properties[0])) { return this; }

        return Parent is not null ? Parent.FindOwner(properties) : this;
    }

    public Scope? Climb(int levels) =>
        levels == 0 ? this : Parent?.Climb(levels - 1);

    public void DefineVariable(string name, string value) =>
        _variables.Define(name, value);

    public bool TryResolveVariable(string name, out IDataSource value)
    {
        if (_variables.TryResolve(name, out value)) { return true; }

        return Parent is not null && Parent.TryResolveVariable(name, out value);
    }

    bool HasProperty(string property) =>
        Data.Kind == DataKind.Object && Data.TryGetProperty(Glossary[property], out _);
}