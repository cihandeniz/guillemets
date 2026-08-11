using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class VariableParser(TokenCursor _tokens, ParserRegistry _registry)
    : IParser
{
    readonly PropertyChainParser _propertyChainParser = _registry.Get<PropertyChainParser>();

    public INode Parse(IToken token)
    {
        var open = (OpenToken)token;
        _tokens.Advance();
        var chain = _propertyChainParser.Parse(open.Position, stopAtNewline: false);

        return new VariableNode(chain);
    }
}