using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Tokenization;

namespace Guillemets.Parsing;

internal class Parser(TokenCursor _tokens, FilterRegistry _filters)
{
    readonly ParserRegistry _registry = new ParserRegistry()
        .Register<TextParser>(_ => new(_tokens))
        .Register<FilterParser>(_ => new(_tokens, _filters))
        .Register<PropertyChainParser>(_ => new(_tokens))
        .Register<VariableParser>(pr => new(_tokens, pr))
        .Register<BodyParser>(pr => new(_tokens, pr))
        .Register<BlockParser>(pr => new(_tokens, pr))
        .Build();

    public List<IRenderable> Parse() =>
        _registry.Get<BodyParser>().Parse(insideBlock: false, stopAtElse: false);
}