using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class ParserRegistry
{
    readonly Dictionary<Type, Func<ParserRegistry, IParser>> _factories = [];
    readonly Dictionary<Type, IParser> _parsers = [];

    public ParserRegistry Register<T>(Func<ParserRegistry, IParser> factory)
    {
        _factories[typeof(T)] = factory;

        return this;
    }

    public ParserRegistry Build()
    {
        foreach (var (type, factory) in _factories)
        {
            _parsers[type] = factory(this);
        }

        return this;
    }

    public T Get<T>() where T : IParser =>
        (T)_parsers[typeof(T)];

    public IParser Resolve(IToken token)
    {
        foreach (var (type, parser) in _parsers)
        {
            if (type.IsInstanceOfType(token)) { return parser; }
        }

        throw new TemplateParseException($"Unexpected token '{token.GetType().Name}'", token.Position);
    }
}