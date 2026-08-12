namespace Guillemets.Parsing;

internal class ParserRegistry
{
    readonly Dictionary<Type, Func<ParserRegistry, object>> _factories = [];
    readonly Dictionary<Type, object> _parsers = [];

    public ParserRegistry Register<T>(Func<ParserRegistry, T> factory) where T : notnull
    {
        _factories[typeof(T)] = pr => factory(pr);

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

    public T Get<T>() where T : notnull =>
        (T)_parsers[typeof(T)];

    public Lazy<T> GetLazy<T>() where T : notnull =>
        new(() => Get<T>());
}