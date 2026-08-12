using System.Collections.ObjectModel;

namespace Guillemets.Ast;

internal class PropertyChainNode(IList<string> properties,
    bool lastSegmentNegated = false
) : ReadOnlyCollection<string>(properties)
{
    public bool LastSegmentNegated { get; } = lastSegmentNegated;

    public PropertyChainNode WithoutLast() =>
        new([.. this.Take(Count - 1)]);

    public PropertyChainNode Tail() =>
        new([.. this.Skip(1)], LastSegmentNegated);

    public class Builder
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

        public PropertyChainNode Build() =>
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
}