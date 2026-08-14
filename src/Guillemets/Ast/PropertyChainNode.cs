using System.Collections.ObjectModel;

namespace Guillemets.Ast;

internal class PropertyChainNode(IList<string> properties,
    bool lastSegmentNegated = false,
    bool thisScopeOnly = false,
    int climbLevels = 0
) : ReadOnlyCollection<string>(properties)
{
    public bool LastSegmentNegated { get; } = lastSegmentNegated;
    public bool ThisScopeOnly { get; } = thisScopeOnly;
    public int ClimbLevels { get; } = climbLevels;

    public PropertyChainNode WithoutLast() =>
        new([.. this.Take(Count - 1)]);

    public PropertyChainNode LastSegment() =>
        new([this[^1]], LastSegmentNegated);

    public PropertyChainNode Tail() =>
        new([.. this.Skip(1)], LastSegmentNegated);

    public class Builder
    {
        readonly List<string> _properties = [];
        bool _negateNext;
        bool _lastSegmentNegated;
        Position? _lastNegationPosition;
        bool _thisScopeOnly;
        int _climbLevels;

        public void Negate(Position position)
        {
            _negateNext = true;
            _lastNegationPosition = position;
        }

        public void PinToCurrentScope() =>
            _thisScopeOnly = true;

        public void Climb() =>
            _climbLevels++;

        public void Add(string text)
        {
            var name = NormalizeWhitespace(text);
            if (name.Length == 0) { return; }

            if (_lastSegmentNegated)
            {
                var position = _lastNegationPosition
                    ?? throw new InvalidOperationException("A negated segment must have a recorded position.");

                throw new TemplateParseException("A negated property must be the last in its chain", position);
            }

            _properties.Add(name);
            _lastSegmentNegated = _negateNext;
            _negateNext = false;
        }

        public PropertyChainNode Build(Position openPosition)
        {
            var result = new PropertyChainNode(_properties, _lastSegmentNegated, _thisScopeOnly, _climbLevels);
            if (result.Count == 0)
            {
                throw new TemplateParseException("Property chain must not be empty", openPosition);
            }

            return result;
        }

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