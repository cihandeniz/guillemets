namespace Guillemets.Rendering;

internal class PropertyChainBuilder
{
    readonly List<string> _properties = [];
    bool _negateNext;
    bool _lastSegmentNegated;

    public void Negate() =>
        _negateNext = true;

    public void Add(string text)
    {
        var name = NormalizeWhitespace(text);
        if (name.Length == 0) { return; }

        _properties.Add(name);
        _lastSegmentNegated = _negateNext;
        _negateNext = false;
    }

    public PropertyChain Build() =>
        new(_properties, _lastSegmentNegated);

    public string PopVariableName()
    {
        var name = _properties.Single();
        _properties.Clear();
        _lastSegmentNegated = false;

        return name;
    }

    static string NormalizeWhitespace(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}