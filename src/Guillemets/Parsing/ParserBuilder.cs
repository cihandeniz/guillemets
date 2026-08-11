using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class ParserBuilder(TokenCursor _tokens)
{
    readonly Dictionary<Type, Func<NodesParser, IParser>> _factories = [];

    public ParserBuilder Register<TToken>(Func<NodesParser, IParser> factory) where TToken : IToken
    {
        _factories[typeof(TToken)] = factory;

        return this;
    }

    public IParser Build()
    {
        var result = new NodesParser(_tokens);
        foreach (var (tokenType, factory) in _factories)
        {
            result.Register(tokenType, factory(result));
        }

        return result;
    }
}